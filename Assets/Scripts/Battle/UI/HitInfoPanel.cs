using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 严格使用旧版 UI 系统

/// <summary>
/// 命中信息面板：负责在 UI 上显示攻击者与目标之间的预期战斗结果（图文混排版）
/// </summary>
public class HitInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public class DamageDisplaySlot
    {
        public DamageType damageType;    // 伤害类型枚举
        public GameObject slotRoot;      // 槽位的根节点（用于控制整组Icon+Text的显示/隐藏）
        public UnityEngine.UI.Image damageIcon;         // 伤害类型图标组件
        public UnityEngine.UI.Text damageText;          // 伤害数值/百分比文本组件
    }

    [Header("UI Components - Hit Rate")]
    [SerializeField] private UnityEngine.UI.Image hitRateIcon;   // 命中率图标（可在编辑器中固定图片）
    [SerializeField] private UnityEngine.UI.Text hitRateText;    // 命中率文本（仅显示百分比数字）

    [Header("UI Components - Damage Slots (Configure 3 types in Editor)")]
    [SerializeField] private List<DamageDisplaySlot> damageSlots; // 在编辑器中配置的3种伤害UI槽位

    [Header("UI Components - Crit Rate & HP")]
    [SerializeField] private UnityEngine.UI.Text critRateText;   // 暴击率文本
    public HpBarUI hpBarUI;                       // 生命值显示组件
    private bool hasDisplay; // 只有本次生命周期实际收到预览后才允许共享头顶显示。

    /// <summary>
    /// 更新面板信息的公共接口
    /// </summary>
    public void UpdateDisplay(PieceController attacker, SkillPack skillPack, PieceController target,
        CheckResult? checkResult = null, bool isFlank = false)
    {
        if (attacker == null || target == null || skillPack == null)
        {
            gameObject.SetActive(false);
            return;
        }
        gameObject.SetActive(true);

        // 1. 只向伤害管理器请求预览，UI 不再维护另一套战斗公式。
        var preview = BattleScene.Ins.BM.damageManager.PreviewSkill(attacker, target, skillPack, checkResult, isFlank, attacker.rangeUI?.PreviewTargets);
        hitRateText.text = $"{preview.HitRate}%";

        // 2. 同类型多段伤害已合并；固定伤害显示单值，随机伤害显示区间。
        foreach (var slot in damageSlots)
        {
            bool hasDamage = preview.ByType.TryGetValue(slot.damageType, out var range);
            if (slot.slotRoot != null) slot.slotRoot.SetActive(hasDamage);
            if (hasDamage && slot.damageText != null)
                slot.damageText.text = range.MinDamage == range.MaxDamage
                    ? $"{range.MinDamage}" : $"{range.MinDamage}–{range.MaxDamage}";
        }

        // 3. 血条使用同一份合计区间，避免文本与血条预测不一致。
        if (hpBarUI != null) hpBarUI.ShowPreDamage(preview.Total, target.unitAttrCenter.MaxHealth);
        hasDisplay = true;
    }

    internal bool HasDisplay => hasDisplay && gameObject.activeInHierarchy;

    /// <summary>为共享头顶创建只读图文副本；副本不执行预览计算，也不持有旧血条的清理权限。</summary>
    internal HitInfoPanel CreateOverheadCopy(Transform parent)
    {
        var copy = Instantiate(this, parent, false);
        copy.name = "TargetPreview";
        copy.hpBarUI = null;
        var rect = (RectTransform)copy.transform;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition3D = new Vector3(0, 6, 0);
        var group = copy.GetComponent<CanvasGroup>();
        if (group == null) group = copy.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1;
        group.blocksRaycasts = group.interactable = false;
        foreach (var graphic in copy.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
        if (copy.critRateText != null) copy.critRateText.gameObject.SetActive(false);
        return copy;
    }

    /// <summary>同步正式预览已算出的数值及图标，最多两列换行，保持屏幕像素字号。</summary>
    internal void RefreshOverheadCopy(HitInfoPanel source)
    {
        int count = 0;
        CopySlot(source.hitRateIcon, source.hitRateText, hitRateIcon, hitRateText,
            (RectTransform)hitRateText.transform.parent, count++);
        for (int i = 0; i < damageSlots.Count; i++)
        {
            var slot = damageSlots[i];
            var original = source.damageSlots[i];
            bool visible = original.slotRoot != null && original.slotRoot.activeSelf;
            slot.slotRoot.SetActive(visible);
            if (visible) CopySlot(original.damageIcon, original.damageText, slot.damageIcon, slot.damageText,
                (RectTransform)slot.slotRoot.transform, count++);
        }
        ((RectTransform)transform).sizeDelta = new Vector2(count > 1 ? 160 : 78, Mathf.CeilToInt(count / 2f) * 20);
    }

    private static void CopySlot(UnityEngine.UI.Image originalIcon, UnityEngine.UI.Text originalText,
        UnityEngine.UI.Image icon, UnityEngine.UI.Text text, RectTransform root, int index)
    {
        root.gameObject.SetActive(true);
        Place(root, index % 2 * 82, index / 2 * 20, 78, 20);
        if (icon != null)
        {
            icon.sprite = originalIcon != null ? originalIcon.sprite : null;
            icon.color = originalIcon != null ? originalIcon.color : Color.white;
            icon.preserveAspect = true;
            Place(icon.rectTransform, 0, 3, 14, 14);
        }
        text.text = originalText.text;
        text.font = originalText.font;
        float density = text.canvas != null ? Mathf.Max(.01f, text.canvas.scaleFactor) : 1;
        // 头顶根节点抵消 Canvas 缩放；同步提高字形采样密度，避免小分辨率下把低分辨率字形放大。
        text.fontSize = Mathf.RoundToInt(13 / density);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.RoundToInt(10 / density);
        text.resizeTextMaxSize = text.fontSize;
        text.color = GameConst.UIColors.Text;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Place(text.rectTransform, 18, 0, 60 / density, 20 / density);
        text.rectTransform.localScale = Vector3.one * density;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition3D = new Vector3(x, -y, 0);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void OnDisable()
    {
        hasDisplay = false;
        if (hpBarUI != null) hpBarUI.ClosePreDamage();
    }
}
