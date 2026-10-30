using UnityEngine;
using UnityEngine.UI; // 使用旧版 UGUI Text
using System.Text;
using System.Collections.Generic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;

public class PlayerLogPanel : MonoBehaviour
{
    [Header("UI Components")] [SerializeField]
    private Text logDisplayText;

    [SerializeField] private int maxLogLines = 15;

    [Header("Attack & Skill Settings")] [SerializeField]
    private Color unitColor = Color.yellow;

    [SerializeField] private Color targetColor = Color.cyan;
    [SerializeField] private Color skillColor = Color.magenta; // 技能颜色

    [SerializeField] private Color damageColor = Color.red;

    //[SerializeField, TextArea] 
    private string attackTemplate = "{0} 施展 [{1}] 对 {2}，造成 {3} 点 <color=white>[{4}]</color> 伤害";

    [Header("Item Settings")] [SerializeField]
    private Color itemColor = Color.green; // 道具颜色

    //[SerializeField, TextArea] 
    private string itemTemplate = "{0} 使用了道具 [{1}]";

    [Header("Move Settings")]
    //[SerializeField, TextArea] 
    private string moveTemplate = "{0} 移动到了坐标 {1}";

    private StringBuilder sb = new StringBuilder();
    private List<string> logEntries = new List<string>();


    // --- 扩展接口部分 ---

    /// <summary>
    /// 记录带技能名称的攻击行为
    /// </summary>
    public void PlayerLogAttack(string attacker, string skillName, string target, int damage
        , string damageType)
    {
        string cAttacker = GetColoredText(attacker, unitColor);
        string cSkill = GetColoredText(skillName, skillColor);
        string cTarget = GetColoredText(target, targetColor);
        string cDamage = GetColoredText(damage.ToString("F0"), damageColor);

        // 填充模板: {0}攻击者, {1}技能, {2}目标, {3}伤害数值, {4}伤害类型
        string finalLog =
            string.Format(attackTemplate, cAttacker, cSkill, cTarget, cDamage, damageType);
        AddEntry(finalLog);
    }
    // ... 之前的变量定义 (logDisplayText, unitColor, targetColor 等) ...

    [Header("Complex AOE Settings")] [SerializeField]
    private string targetLineSeparator = "；"; // 不同敌人之间的分隔符

    [SerializeField] private string damageDetailSeparator = "、"; // 同一敌人多种伤害的分隔符

    //[SerializeField, TextArea] 
    private string complexAoeTemplate = "{0}发动{1}：{2}";

    /// <summary>
    /// 重载：对多个敌人分别造成多重伤害
    /// </summary>
    /// <param name="attacker">攻击者名称</param>
    /// <param name="targets">目标名称列表</param>
    /// <param name="allDamages">外层 List 对应每个目标，内层 List 对应该目标的多种伤害</param>
    public void PlayerLogAttack(string attacker, string skillName, List<string> targets
        , List<List<DamageInfo>> allDamages)
    {
        if (targets == null || allDamages == null || targets.Count != allDamages.Count)
        {
            Debug.LogError("LogManager: 目标数量与伤害数据数量不匹配！");
            return;
        }

        StringBuilder mainContentBuilder = new StringBuilder();

        for (int i = 0; i < targets.Count; i++)
        {
            // 1. 格式化当前目标名称
            string cTarget = GetColoredText(targets[i], targetColor);
            mainContentBuilder.Append($"对 {cTarget} 造成了 ");


            // 2. 格式化该目标受到的所有伤害
            List<DamageInfo> currentTargetDamages = allDamages[i];
            for (int j = 0; j < currentTargetDamages.Count; j++)
            {
                DamageInfo info = currentTargetDamages[j];
                string cVal = GetColoredText(info.damageValue.ToString("F0"), damageColor);

                // 暴击
                if (info.isCritical)
                {
                    mainContentBuilder.Append($"<color=orange>暴击！</color>");
                }

                // 格式：xx点伤害（xx类型）
                mainContentBuilder.Append($"{cVal}点伤害({info.damageType})");

                // 如果不是最后一种伤害，加分隔符
                if (j < currentTargetDamages.Count - 1)
                {
                    mainContentBuilder.Append(damageDetailSeparator);
                }
            }

            // 3. 如果不是最后一个敌人，加换行或分隔符
            if (i < targets.Count - 1)
            {
                mainContentBuilder.Append(targetLineSeparator);
            }
        }

        // 4. 组装最终文本
        string cAttacker = GetColoredText(attacker, unitColor);
        string finalLog = string.Format(complexAoeTemplate, cAttacker, skillName
            , mainContentBuilder.ToString());

        AddEntry(finalLog);
        Debug.Log(finalLog);
    }

    /// <summary>
    /// 记录棋子使用道具
    /// </summary>
    private void PlayerLogUseItem(string unitName, string itemName)
    {
        string cUnit = GetColoredText(unitName, unitColor);
        string cItem = GetColoredText(itemName, itemColor);

        // 填充模板: {0}棋子名, {1}道具名
        string finalLog = string.Format(itemTemplate, cUnit, cItem);
        AddEntry(finalLog);
    }

    /// <summary>
    /// 记录移动行为
    /// </summary>
    public void PlayerLogMove(string unitName, Vector3 destination)
    {
        string cUnit = GetColoredText(unitName, unitColor);
        string posStr = $"({destination.x:F1}, {destination.z:F1})";

        string finalLog = string.Format(moveTemplate, cUnit, posStr);
        AddEntry(finalLog);
    }
    /// <summary>
    /// 记录普通文本日志（不带特殊格式）
    /// </summary>
    /// <param name="message"></param>
    public void PlayerLog(string message)
    {
        AddEntry(message);
    }

    private UnityEngine.UI.ScrollRect logScroll;
    /// <summary>在原日志文本位置接入滚动视口，长日志可查看，不遮挡其他消息区。</summary>
    public void EnsureScrolling()
    {
        if (logScroll != null || logDisplayText == null) return;
        var old = logDisplayText.rectTransform;
        logScroll = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        if (logScroll == null)
        {
            var root = new GameObject("LogScroll", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            root.transform.SetParent(transform, false);
            logScroll = root.GetComponent<UnityEngine.UI.ScrollRect>();
        }
        var rect = (RectTransform)logScroll.transform;
        if (rect != transform)
        {
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12, 24); rect.offsetMax = new Vector2(-12, -12);
            rect.localScale = Vector3.one;
        }
        var viewport = logScroll.viewport;
        if (viewport == null)
        {
            var node = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Mask));
            node.transform.SetParent(rect, false); viewport = (RectTransform)node.transform;
            node.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .01f);
            node.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        }
        viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        var content = logScroll.content;
        if (content == null)
        {
            var node = new GameObject("Content", typeof(RectTransform));
            node.transform.SetParent(viewport, false); content = (RectTransform)node.transform;
        }
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        content.localScale = Vector3.one;
        var layout = content.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (layout == null) layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
        var fitter = content.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (fitter == null) fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var oldFitter = old.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (oldFitter != null) oldFitter.enabled = false;
        old.SetParent(content, false); old.gameObject.SetActive(true); old.localScale = Vector3.one;
        logDisplayText.enabled = true; logDisplayText.fontSize = 18; logDisplayText.lineSpacing = 2f;
        logDisplayText.resizeTextForBestFit = false; logDisplayText.alignment = TextAnchor.UpperLeft;
        logDisplayText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logDisplayText.verticalOverflow = VerticalWrapMode.Overflow; logDisplayText.raycastTarget = false;
        logScroll.content = content; logScroll.viewport = viewport;
        logScroll.horizontal = false; logScroll.vertical = true;
        logScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        logScroll.scrollSensitivity = 24;
    }

    /// <summary>新场或离场时清空本场日志，不修改任何战斗与奖励状态。</summary>
    public void ClearLog() { logEntries.Clear(); RefreshUI(); }

    public int EntryCount => logEntries.Count;

    // --- 内部逻辑 ---

    private void AddEntry(string entry)
    {
        while (logEntries.Count >= Mathf.Max(1, maxLogLines))
        {
            logEntries.RemoveAt(0);
        }

        logEntries.Add(entry);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (logDisplayText == null) return;

        sb.Clear();
        foreach (var log in logEntries)
        {
            sb.AppendLine(log);
        }

        bool followLatest = logScroll == null || logScroll.verticalNormalizedPosition < .05f;
        logDisplayText.text = sb.ToString();
        if (logScroll != null && followLatest)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(logScroll.content);
            logScroll.verticalNormalizedPosition = 0;
        }
    }

    private string GetColoredText(string text, Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        return $"<color=#{hex}>{text}</color>";
    }
}

[System.Serializable]
public class DamageInfo
{
    public float damageValue;
    public string damageType;

    public bool isCritical; // 可选：是否暴击

    public DamageInfo(float value, string type, bool isCrit = false)
    {
        damageValue = value;
        damageType = type;
        isCritical = isCrit;
    }
}