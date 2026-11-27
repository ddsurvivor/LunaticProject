using System.Collections.Generic;
using UnityEngine;

public partial class UIManager
{
    private bool openingDialogueHudHidden; // 仅开场剧情持有，不影响战中剧情或其他暂停来源。
    private readonly Dictionary<CanvasGroup, (float alpha, bool interactable, bool blocks, bool enabled)> openingHudGroups = new(); // 首次遮罩前的状态，重复刷新不覆盖。

    /// <summary>只遮住战斗HUD并保留原显示状态；不关闭阅读器、骰子、设置或宿主Canvas。</summary>
    public void SetOpeningDialogueHudHidden(bool hidden)
    {
        openingDialogueHudHidden = hidden;
        if (hidden) RefreshOpeningDialogueHud();
        else
        {
            foreach (var item in openingHudGroups)
            {
                if (item.Key == null) continue;
                item.Key.alpha = item.Value.alpha;
                item.Key.interactable = item.Value.interactable;
                item.Key.blocksRaycasts = item.Value.blocks;
                item.Key.enabled = item.Value.enabled;
            }
            openingHudGroups.Clear();
        }
    }

    private void RefreshOpeningDialogueHud()
    {
        if (!openingDialogueHudHidden) return;
        foreach (var component in new Component[] { pieceInfoPanel, pieceActionListPanel, pieceStatePanel,
            turnPanel, endTurnButton, burstButton, turnNumberText, skillTooltipUI, teamPanel, infoBox,
            OverheadStatuses, speedTimeline, timeControls, logPanel, skillNameDisplay, NotificationRect,
            undoMoveBtn, skipButton, burstStart, burstEnd })
            HideOpeningHudRoot(component != null ? component.transform : null);
        HideOpeningHudRoot(restartButton != null ? restartButton.transform : null);
        if (turnImage != null)
            foreach (var image in turnImage) HideOpeningHudRoot(image != null ? image.transform : null);
    }

    private void HideOpeningHudRoot(Transform root)
    {
        if (root == null || logSystem != null && logSystem.transform.IsChildOf(root)) return;
        if (root.GetComponent<CanvasGroup>() == null) root.gameObject.AddComponent<CanvasGroup>();
        // 子级可能忽略父CanvasGroup，逐一遮蔽，且保留其原有透明度与射线状态。
        foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (!openingHudGroups.ContainsKey(group))
                openingHudGroups.Add(group, (group.alpha, group.interactable, group.blocksRaycasts, group.enabled));
            group.enabled = true;
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        var scene = BattleScene.Ins;
        if (scene != null && scene.UM == this && scene.BM != null)
            SetOpeningDialogueHudHidden(scene.BM.IsPlayingOpeningDialogue);
    }
}
