using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class RichTextTypewriter : MonoBehaviour
{
    [SerializeField] private Text _textComponent;
    [SerializeField] private float _typeSpeed = 0.05f;

    private Coroutine _typeRoutine;

    /// <summary>
    /// 开始打字机效果
    /// </summary>
    /// <param name="fullContent">包含富文本的完整字符串</param>
    public void StartTyping(string fullContent)
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
        }
        _typeRoutine = StartCoroutine(TypeText(fullContent));
    }

    private IEnumerator TypeText(string fullContent)
    {
        _textComponent.text = "";
        
        // 正则表达式：匹配 <tag> 或 </tag>
        // 旧版 Text 支持的标签有限：<b>, <i>, <size>, <color>
        string tagRegex = @"<[^>]+>";
        MatchCollection tags = Regex.Matches(fullContent, tagRegex);
        
        // 获取所有纯文本内容的索引
        List<int> visibleCharIndices = new List<int>();
        int currentPos = 0;

        for (int i = 0; i < fullContent.Length; i++)
        {
            // 检查当前位置是否处于标签内
            bool isInTag = false;
            foreach (Match tag in tags)
            {
                if (i >= tag.Index && i < tag.Index + tag.Length)
                {
                    i = tag.Index + tag.Length - 1; // 跳过标签部分
                    isInTag = true;
                    break;
                }
            }

            if (!isInTag)
            {
                visibleCharIndices.Add(i);
            }
        }

        // 开始逐字显示
        for (int i = 0; i <= visibleCharIndices.Count; i++)
        {
            int displayLength = (i < visibleCharIndices.Count) ? visibleCharIndices[i] + 1 : fullContent.Length;
            string subString = fullContent.Substring(0, displayLength);
            
            // 核心步骤：补全未闭合的标签
            _textComponent.text = CloseTags(subString);

            yield return new WaitForSeconds(_typeSpeed);
        }

        _typeRoutine = null;
    }

    /// <summary>
    /// 使用栈逻辑自动补全缺失的闭合标签
    /// </summary>
    private string CloseTags(string input)
    {
        // 匹配所有开头标签，如 <color=#FF0000>
        MatchCollection openingTags = Regex.Matches(input, @"<[^/][^>]*>");
        // 匹配所有闭合标签，如 </color>
        MatchCollection closingTags = Regex.Matches(input, @"</[^>]+>");

        Stack<string> tagStack = new Stack<string>();

        foreach (Match tag in openingTags)
        {
            // 获取标签名称，例如从 <color=red> 中提取 color
            string tagName = tag.Value.Split(new char[] { '<', '>', '=', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)[0];
            tagStack.Push(tagName);
        }

        foreach (Match tag in closingTags)
        {
            if (tagStack.Count > 0)
            {
                tagStack.Pop();
            }
        }

        // 将栈中剩余的标签按相反顺序闭合
        while (tagStack.Count > 0)
        {
            input += "</" + tagStack.Pop() + ">";
        }

        return input;
    }
}