using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class 打字机 : MonoBehaviour
{
    public bool IsTesting;
    [FormerlySerializedAs("textComponent")] public Text _textComponent;
    public string 完整文本;
    [FormerlySerializedAs("字符延迟")] public float _typeSpeed = 0.03f;
    private bool inited;
    public bool IsTyping { get; private set; }
    private bool textCompleted;
    private int visibleCharactersShown;
    private 剧本System owner;
    private ChineseTextLayout chineseLayout;
    public float CharacterDelay => Mathf.Max(0f, _typeSpeed) / Mathf.Max(0.1f,
        GM.Ins != null && GM.Ins.DM != null && GM.Ins.DM.settingsData != null
            ? GM.Ins.DM.settingsData.textSpeed : 1f);

    private string currentText = "";

    [SerializeField]
    private GameObject outline;
    [SerializeField]
    private RectTransform fill;
    [SerializeField] private bool avgUI;
    private bool textInsetCaptured;
    private float defaultTextLeft;

    public float 初始化(string 文本, bool isOption = false)
    {
        // 每次取出对象都重新初始化，不能因曾播放过而跳过状态重置。
        return 播放文本(文本, 剧本System.instance, isOption);
    }

    /// <summary>同一个 Text 可反复播放不同正文，供 CGLOG 前景文字使用。</summary>
    public float 播放文本(string 文本, 剧本System reader, bool isOption = false)
    {
        StopAllCoroutines();
        SetOptionTextInset(isOption);
        if (owner != null) owner.当文本更新时 -= 下一句;
        owner = reader;
        if (owner != null) owner.当文本更新时 += 下一句;
        inited = true;
        textCompleted = false;
        visibleCharactersShown = 0;
        Canvas.ForceUpdateCanvases();
        if (chineseLayout != null) chineseLayout.LayoutChanged -= OnChineseLayoutChanged;
        chineseLayout = ChineseTextLayout.Ensure(_textComponent);
        chineseLayout.LayoutChanged += OnChineseLayoutChanged;
        完整文本 = chineseLayout.PrepareText(文本 ?? "", true);
        _textComponent.text = 完整文本;
        Canvas.ForceUpdateCanvases();
        float height = _textComponent.rectTransform.rect.height;
        if (fill != null) fill.gameObject.SetActive(false);
        if (outline != null) outline.SetActive(false);
        StartCoroutine(TypeText(完整文本));
        return height;
    }

    public void 清空文本()
    {
        StopAllCoroutines();
        IsTyping = false;
        textCompleted = true;
        visibleCharactersShown = 0;
        完整文本 = "";
        if (chineseLayout != null) chineseLayout.PrepareText("", true);
        _textComponent.text = "";
    }

    private void Update()
    {
        if (IsTesting)
        {
            Debug.Log($"位置{transform.position}");
        }
    }

    void 下一句()
    {
        StopAllCoroutines();
        IsTyping = false;
        textCompleted = true;
        _textComponent.text = 完整文本 + " ";
    }

    private void OnEnable()
    {
        if (!inited) return;
        if (owner != null) owner.当文本更新时 += 下一句;
        if (!textCompleted) StartCoroutine(TypeText(完整文本));
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        IsTyping = false;
        if (owner != null) owner.当文本更新时 -= 下一句;
    }

    private void OnChineseLayoutChanged(string text)
    {
        完整文本 = text;
        if (textCompleted) { _textComponent.text = 完整文本; return; }
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(TypeText(完整文本));
    }

    private void OnDestroy()
    {
        if (chineseLayout != null) chineseLayout.LayoutChanged -= OnChineseLayoutChanged;
    }

    IEnumerator ShowText()
    {
        _textComponent.color = DefaultTextColor;
        for (int i = 0; i < 完整文本.Length; i++)
        {
            currentText = 完整文本.Substring(0, i + 1);
            _textComponent.text = currentText;
            yield return new WaitForSeconds(CharacterDelay);
        }
    }
    
    
    /// <summary>
    /// 判断某个Text是否在屏幕渲染范围内
    /// </summary>
    /// <returns>true 表示在范围内，false 表示不在范围内</returns>
    public bool IsTextVisible()
    {
        if (_textComponent == null)
        {
            Debug.LogWarning("targetText 未设置！");
            return false;
        }
        
        RectTransform rectTransform = _textComponent.rectTransform;
        
        // 获取四个角在世界空间下的坐标
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // 依次将四个角转换到屏幕空间，检查是否在屏幕范围内
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(corners[i]);
            
            // 如果z小于0，说明在摄像机后方，也忽略
            if (screenPos.z < 0)
                continue;

            // 判断 x, y 是否在屏幕范围内
            if (screenPos.x >= 0 && screenPos.x <= Screen.width &&
                screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                return true;
            }
        }

        return false;
    }
    
    
    private IEnumerator TypeText(string fullContent)
    {
        IsTyping = true;
        _textComponent.text = "";
        _textComponent.color = DefaultTextColor;
        // 正则表达式：匹配 <tag> 或 </tag>
        // 旧版 Text 支持的标签有限：<b>, <i>, <size>, <color>
        string tagRegex = @"<[^>]+>";
        MatchCollection tags = Regex.Matches(fullContent, tagRegex);
        
        // 获取所有纯文本内容的索引
        List<int> visibleCharIndices = new List<int>();


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

            // 排版生成的换行不占用逐字播放时间。
            if (!isInTag && fullContent[i] != '\r' && fullContent[i] != '\n')
            {
                // 与排版使用相同的文本元素，避免拆开代理对或组合字符。
                int elementLength = StringInfo.GetNextTextElement(fullContent, i).Length;
                visibleCharIndices.Add(i + elementLength - 1);
                i += elementLength - 1;
            }
        }

        // 开始逐字显示
        for (int i = visibleCharactersShown; i < visibleCharIndices.Count; i++)
        {
            int displayLength = visibleCharIndices[i] + 1;
            string subString = fullContent.Substring(0, displayLength);
            
            // 核心步骤：补全未闭合的标签
            _textComponent.text = CloseTags(subString);
            visibleCharactersShown = i + 1;

            if (i + 1 < visibleCharIndices.Count)
                yield return new WaitForSeconds(CharacterDelay);
        }
        _textComponent.text = fullContent;
        textCompleted = true;
        IsTyping = false;
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

    /// <summary>
    /// 将文本显示为选项
    /// </summary>
    public void ShowSelect()
    {
        if (avgUI)
        {
            if (outline != null) outline.SetActive(false);
            if (fill != null) fill.gameObject.SetActive(true);
            return;
        }
        _textComponent.GetComponent<Text>().color = Color.white;
        fill.gameObject.SetActive(true);
    }
    private Color DefaultTextColor => avgUI
        ? new Color(0.937255f, 0.913725f, 0.819608f)
        : Color.white;

    public void ShowOption()
    {
        if (!avgUI) return;
        SetOptionTextInset(true);
        if (fill != null) fill.gameObject.SetActive(false);
        outline.SetActive(true);
        var background = outline.GetComponent<UnityEngine.UI.Image>();
        background.color = Color.white;
        var button = _textComponent.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            // 鼠标移入时只为 outline 着色；选中后由 ShowSelect 切换为 fill。
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.91f, 0.65f, 0.17f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }
    }

    private void SetOptionTextInset(bool isOption)
    {
        if (!avgUI || _textComponent == null) return;
        var rect = _textComponent.rectTransform;
        if (!textInsetCaptured)
        {
            defaultTextLeft = rect.offsetMin.x;
            textInsetCaptured = true;
        }
        var offset = rect.offsetMin;
        // 使用绝对值，重复选中不会累加；普通正文恢复预制体的初始 Left（20）。
        offset.x = isOption ? 60f : defaultTextLeft;
        rect.offsetMin = offset;
    }
    /// <summary>
    /// 设置为关闭的选项
    /// </summary>
    public void EndOption()
    {
        _textComponent.GetComponent<Button>().enabled = false;
        _textComponent.GetComponent<Text>().color = Color.gray;
    }
}
