using System.Collections.Generic;
using UnityEngine;

/// <summary>控制 uGUI Text 的中文标点换行。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Text))]
public sealed class ChineseTextLayout : MonoBehaviour
{
    private UnityEngine.UI.Text target;
    private readonly TextGenerator generator = new TextGenerator();
    private readonly Dictionary<string, float> widths = new Dictionary<string, float>();
    private string source;
    private string formatted;
    private int settingsHash;
    private bool controlledByTypewriter;
    private HorizontalWrapMode originalOverflow;
    /// <summary>通知打字机使用重新排版后的文本。</summary>
    public event System.Action<string> LayoutChanged;

    private int LayoutHash => target.rectTransform.rect.width.GetHashCode() ^ target.fontSize ^
        (int)target.fontStyle * 397 ^ (target.font != null ? target.font.GetInstanceID() : 0) ^
        target.supportRichText.GetHashCode() ^ target.pixelsPerUnit.GetHashCode();

    /// <summary>获取排版组件；缺少时自动添加。</summary>
    public static ChineseTextLayout Ensure(UnityEngine.UI.Text text)
    {
        var layout = text.GetComponent<ChineseTextLayout>();
        return layout != null ? layout : text.gameObject.AddComponent<ChineseTextLayout>();
    }

    /// <summary>按文本框宽度排版。打字机模式保留整段排版结果。</summary>
    public string PrepareText(string text, bool typewriter = false)
    {
        ResolveTarget();
        controlledByTypewriter |= typewriter;
        float width = target.rectTransform.rect.width;
        int hash = LayoutHash;
        if (source == text && formatted != null && settingsHash == hash) return formatted;
        source = text ?? "";
        settingsHash = hash;
        if (target.font == null || width <= 0f) return formatted = source;
        var settings = target.GetGenerationSettings(Vector2.zero);
        settings.resizeTextForBestFit = false;
        settings.horizontalOverflow = HorizontalWrapMode.Overflow;
        settings.verticalOverflow = VerticalWrapMode.Overflow;
        widths.Clear();
        formatted = ChineseLineBreakUtility.Wrap(source, width, sample =>
        {
            if (!widths.TryGetValue(sample, out float measured))
            {
                measured = generator.GetPreferredWidth(sample, settings) / target.pixelsPerUnit;
                widths.Add(sample, measured);
            }
            return measured;
        }, target.supportRichText);
        // 由脚本插入换行，避免 Text 再按普通规则把标点独立折到下一行。
        target.horizontalOverflow = formatted == source ? originalOverflow : HorizontalWrapMode.Overflow;
        return formatted;
    }

    private void LateUpdate()
    {
        ResolveTarget();
        if (controlledByTypewriter)
        {
            if (settingsHash != LayoutHash) LayoutChanged?.Invoke(PrepareText(source, true));
            return;
        }
        string original = target.text == formatted ? source : target.text;
        string result = PrepareText(original);
        if (target.text != result) target.text = result;
    }

    private void ResolveTarget()
    {
        if (target != null) return;
        target = GetComponent<UnityEngine.UI.Text>();
        originalOverflow = target.horizontalOverflow;
    }

    private void OnDestroy() => (generator as object as System.IDisposable)?.Dispose();
}
