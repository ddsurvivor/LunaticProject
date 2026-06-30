using System;
using System.Collections;
using System.Collections.Generic;
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

    private string currentText = "";

    [SerializeField]
    private GameObject outline;
    [SerializeField]
    private RectTransform fill;

    public float 初始化(string 文本)
    {
        float 框大小 = 0;
        剧本System.instance.当文本更新时 += 下一句;
 
        if (!inited)
        {
            inited = true;
            完整文本 = 文本;
            _textComponent.text = 完整文本;
            Canvas.ForceUpdateCanvases();
            // 计算高度
            框大小 = _textComponent.GetComponent<RectTransform>().rect.height;

            // 启动打字机效果
            StartCoroutine(TypeText(文本));
            //StartCoroutine(ShowText());
            fill.gameObject.SetActive(false);
            outline.SetActive(false);
        }
        // 返回计算出的高度
        return 框大小;
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
        _textComponent.text = 完整文本 + " ";
    }

    private void OnDisable()
    {
        剧本System.instance.当文本更新时 -= 下一句;
    }

    IEnumerator ShowText()
    {
        _textComponent.color = new Color(1, 1, 1, 1); // 确保文本在显示时是可见的
        for (int i = 0; i < 完整文本.Length; i++)
        {
            currentText = 完整文本.Substring(0, i + 1);
            _textComponent.text = currentText;
            yield return new WaitForSeconds(_typeSpeed);
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
        _textComponent.text = "";
        _textComponent.color = new Color(1, 1, 1, 1); // 确保文本在显示时是可见的
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

        //_typeRoutine = null;
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
        _textComponent.GetComponent<Text>().color = Color.white;
        fill.gameObject.SetActive(true);
        fill.sizeDelta = _textComponent.GetComponent<RectTransform>().sizeDelta + new Vector2(20, 20); // 根据文本大小调整背景框
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
