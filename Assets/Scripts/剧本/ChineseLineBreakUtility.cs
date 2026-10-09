using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>中文禁则换行。只插入软换行，不删除原文、富文本标签或显式换行。</summary>
public static class ChineseLineBreakUtility
{
    private const string NoLineStart = "，。、；：？！…—）〕］｝〉》」』】〗〙〛’”﹚﹜﹞％‰℃°,.!?;:%)]}";
    private const string NoLineEnd = "（〔［｛〈《「『【〖〘〚‘“﹙﹛﹝([{￥＄";
    private static readonly Regex Tag = new Regex(@"\G</?(?:b|i|size|color)(?:=[^>]+)?>", RegexOptions.IgnoreCase);

    private sealed class Element
    {
        public int Offset;
        public string Value;
        public float Width;
    }

    public static string Wrap(string source, float width, Func<string, float> measure, bool richText = true)
    {
        if (string.IsNullOrEmpty(source) || width <= 0f) return source ?? "";
        var elements = new List<Element>();
        var tags = new List<string>();
        for (int i = 0; i < source.Length;)
        {
            Match tag = richText ? Tag.Match(source, i) : null;
            if (tag != null && tag.Success)
            {
                if (tag.Value.StartsWith("</"))
                {
                    if (tags.Count > 0) tags.RemoveAt(tags.Count - 1);
                }
                else tags.Add(tag.Value);
                i += tag.Length;
                continue;
            }
            string value = StringInfo.GetNextTextElement(source, i);
            var styled = new StringBuilder();
            foreach (string opening in tags) styled.Append(opening);
            styled.Append(value);
            for (int t = tags.Count - 1; t >= 0; t--)
            {
                string name = tags[t].Substring(1).Split('=', '>')[0];
                styled.Append("</").Append(name).Append('>');
            }
            elements.Add(new Element { Offset = i, Value = value,
                Width = value == "\r" || value == "\n" ? 0f : Math.Max(0f, measure(styled.ToString())) });
            i += value.Length;
        }

        var breaks = new HashSet<int>();
        int start = 0;
        while (start < elements.Count)
        {
            if (IsNewline(elements[start].Value)) { start++; continue; }
            int end = start;
            float used = 0f;
            while (end < elements.Count && !IsNewline(elements[end].Value) &&
                   (end == start || used + elements[end].Width <= width + 0.01f))
                used += elements[end++].Width;
            if (end == elements.Count || IsNewline(elements[end].Value)) { start = end; continue; }

            int split = end;
            while (split > start && !CanBreakAt(elements, split)) split--;
            if (split == start)
            {
                // 极窄容器或长英文单词：允许拆英文，但不能违反标点禁则。
                split = end;
                while (split > start && !CanBreakAt(elements, split, false)) split--;
                if (split == start)
                {
                    split = end;
                    while (split < elements.Count && !IsNewline(elements[split].Value) &&
                           !CanBreakAt(elements, split, false)) split++;
                }
            }
            if (split < elements.Count && !IsNewline(elements[split].Value)) breaks.Add(elements[split].Offset);
            start = split;
        }
        var result = new StringBuilder(source.Length + breaks.Count);
        for (int i = 0; i < source.Length; i++)
        {
            if (breaks.Contains(i)) result.Append('\n');
            result.Append(source[i]);
        }
        return result.ToString();
    }

    private static bool IsNewline(string text) => text == "\r" || text == "\n" || text == "\r\n";

    private static bool CanBreakAt(List<Element> elements, int split, bool keepLatinWord = true)
    {
        int left = split - 1, right = split;
        // 标点前后的空格不能绕过行首、行尾禁则。
        while (left >= 0 && !IsNewline(elements[left].Value) && string.IsNullOrWhiteSpace(elements[left].Value)) left--;
        while (right < elements.Count && !IsNewline(elements[right].Value) && string.IsNullOrWhiteSpace(elements[right].Value)) right++;
        if (left < 0 || right == elements.Count || IsNewline(elements[left].Value) || IsNewline(elements[right].Value)) return true;
        return CanBreak(elements[left].Value, elements[right].Value,
            keepLatinWord && left == split - 1 && right == split);
    }

    public static bool CanBreak(string left, string right, bool keepLatinWord = true)
    {
        char a = left[left.Length - 1], b = right[0];
        if (NoLineEnd.IndexOf(a) >= 0 || NoLineStart.IndexOf(b) >= 0) return false;
        if ((a == '…' && b == '…') || (a == '—' && b == '—')) return false;
        return !keepLatinWord || !(IsLatinWord(a) && IsLatinWord(b));
    }

    private static bool IsLatinWord(char c) => c < 128 && (char.IsLetterOrDigit(c) || c == '_');
}
