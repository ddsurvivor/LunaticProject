using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>装配当前角色的两级行动目录；费用与执行仍由正式战斗入口处理。</summary>
public partial class PieceActionListPanel : SerializedMonoBehaviour
{
    [SerializeField, ReadOnly] private PieceController pc;
    [HideInInspector] public List<UnityEngine.UI.Button> actionButtons;
    [HideInInspector] public List<UnityEngine.UI.Button> skillButtons;
    public GameObject skillListPanel;
    [SerializeField] private ActionBtn actionButtonPrefab;
    [SerializeField, Min(0)] private float detailsTransferDelay = 1.25f; // 从按钮移动到固定详情区的宽限秒数，不受时间缩放影响。
    private bool detailsPending;
    private float detailsCloseAt;
    private enum Category { None, Attack, Skills, Items, Orders }
    private sealed class Entry
    {
        public Func<string> title;
        public Category category;
        public Func<bool> available;
        public Func<ActionResourceCost> cost;
        public Action execute, showDetails;
    }
    private readonly List<Entry> primaryEntries = new(), secondaryEntries = new();
    private readonly List<ActionBtn> primaryRows = new(), secondaryRows = new();
    private readonly List<ActionCostPreviewUI> previewViews = new();
    private Entry expandedEntry, hoveredEntry;
    private ActionBtn expandedRow, hoveredRow;

    /// <summary>只装配一次布局与预制体池；资源缺失时不标记为完成。</summary>
    public void Init()
    {
        if (menuReady) return;
        if (actionButtonPrefab == null)
        {
            Debug.LogError("Action Panel 缺少 ActionButton 预制体引用。", this);
            return;
        }
        BuildMenuLayout();
    }

    /// <summary>以显式展示顺序构建可操作角色的动作，不依赖枚举或旧按钮数组顺序。</summary>
    public void ShowPanel(PieceController piece)
    {
        if (piece == null || !BattleScene.Ins.BM.CanControlPiece(piece)) return;
        BattleScene.Ins.UM.ClearActionFlow();
        Init();
        if (!menuReady) return;
        ClearHover();
        pc = piece;
        expandedEntry = null;
        expandedRow = null;
        ClearRows(secondaryRows);
        secondaryEntries.Clear();
        skillListPanel.SetActive(false);
        primaryEntries.Clear();
        AddPrimaryAction(ActionType.Move);
        if (HasAttack(ActionType.MeleeAttack) || HasAttack(ActionType.RangedAttack)) AddCategory(Category.Attack);
        if (HasAction(ActionType.Skill)) AddCategory(Category.Skills);
        if (HasAction(ActionType.Item)) AddCategory(Category.Items);
        if (HasAction(ActionType.GuardOrder)) AddCategory(Category.Orders);
        if (pc.unitAttrCenter.MaxAmmoCount > 0 && pc.unitAttrCenter.AmmoCount < pc.unitAttrCenter.MaxAmmoCount
            && (HasAction(ActionType.Reload) || HasAction(ActionType.RangedAttack))) AddPrimaryAction(ActionType.Reload, true);
        foreach (var action in new[] { ActionType.Scan, ActionType.ReleaseBind, ActionType.Climb,
            ActionType.Interact, ActionType.OrbitalStrike, ActionType.Talk }) AddPrimaryAction(action);
        AddPrimaryAction(ActionType.Wait, true);
        gameObject.SetActive(true);
        BindRows(primaryEntries, primaryRows, primaryContent);
        RefreshMenuLayout(true);
        RevealRows(primaryRows);
    }

    private bool HasAction(ActionType action) => pc.availableActions.Contains(action);
    private bool HasAttack(ActionType action) => HasAction(action) && pc.pieceData != null
        && (action == ActionType.MeleeAttack ? pc.pieceData.MeleeAttack != null : pc.pieceData.RangedAttack != null);
    private bool CanAct() => pc != null && pc.gameObject.activeInHierarchy && !pc.isDead
        && BattleScene.Ins != null && BattleScene.Ins.BM.CanControlPiece(pc);

    private void AddCategory(Category category)
    {
        primaryEntries.Add(new Entry { category = category, title = () => CategoryLabel(category), available = CanAct });
    }

    private static string CategoryLabel(Category category)
    {
        switch (category)
        {
            case Category.Attack: return ActionTypeLabels.English ? "Attack" : "攻击";
            case Category.Skills: return ActionType.Skill.ToDisplayName();
            case Category.Items: return ActionType.Item.ToDisplayName();
            case Category.Orders: return ActionType.GuardOrder.ToDisplayName();
            default: return string.Empty;
        }
    }

    private bool CanExecute(ActionType action, SkillPack skill = null)
    {
        if (!CanAct() || !ActionResourceCost.Query(pc, action, skill).CanPay(pc.unitAttrCenter)
            || pc.unitAttrCenter.GetBuffStacks(BuffType.Stun) > 0) return false;
        if ((action == ActionType.Move || action == ActionType.Climb)
            && pc.unitAttrCenter.GetBuffStacks(BuffType.Bind) > 0) return false;
        if (action == ActionType.Skill) return skill != null && pc.availableSkills.Contains(skill) && pc.SkillAvailable(skill);
        if (action == ActionType.MeleeAttack || action == ActionType.RangedAttack) return HasAttack(action);
        if (action == ActionType.Reload) return pc.unitAttrCenter.MaxAmmoCount > 0
            && pc.unitAttrCenter.AmmoCount < pc.unitAttrCenter.MaxAmmoCount;
        return action != ActionType.Wait || pc.unitAttrCenter.CurMovePoint > 0;
    }

    private Entry MakeAction(ActionType action)
    {
        return new Entry
        {
            title = () => action.ToDisplayName(), available = () => CanExecute(action)
                && (HasAction(action) || action == ActionType.Wait
                    || action == ActionType.Reload && HasAction(ActionType.RangedAttack)),
            cost = () => ActionResourceCost.Query(pc, action), execute = () => ExecuteAction(action)
        };
    }

    private void AddPrimaryAction(ActionType action, bool force = false)
    {
        InteractArea area = null;
        foreach (var candidate in pc.interactAreas)
            if (candidate != null && candidate.actionType == action && candidate.ableToTrigger) { area = candidate; break; }
        if (!force && !HasAction(action) && area == null) return;
        var entry = MakeAction(action);
        if (area != null)
        {
            var captured = area;
            var chargedAction = area is LadderArea ? ActionType.Climb : ActionType.Interact;
            entry.available = () => CanExecute(chargedAction) && captured != null && captured.ableToTrigger;
            entry.cost = () => ActionResourceCost.Query(pc, chargedAction);
            entry.execute = () =>
            {
                if (captured != null && captured.ableToTrigger && pc.unitAttrCenter.CostMP(chargedAction)) captured.TriggerAction(pc);
            };
        }
        else if (action != ActionType.Move && action != ActionType.Reload && action != ActionType.Scan
            && action != ActionType.Wait) entry.available = () => false;
        primaryEntries.Add(entry);
    }

    private void ExecuteAction(ActionType action)
    {
        switch (action)
        {
            case ActionType.Move: BattleScene.Ins.CM.StartDarg(pc); break;
            case ActionType.MeleeAttack: pc.StartNormalAttack(); break;
            case ActionType.RangedAttack: pc.StartNormalAttack(true); break;
            case ActionType.Reload:
                if (pc.unitAttrCenter.CostMP(action)) pc.ReloadAmmo();
                break;
            case ActionType.Scan:
                if (pc.unitAttrCenter.CostMP(action)) pc.PlayAudio(action);
                break;
            case ActionType.Wait:
                if (!pc.unitAttrCenter.CostMP(pc.unitAttrCenter.CurMovePoint)) return;
                pc.isIdle = true;
                pc.PlayAudio(action);
                break;
        }
    }

    private void OpenCategory(Entry entry, ActionBtn parent)
    {
        if (expandedEntry == entry) return;
        ClearHover();
        expandedRow?.SetExpanded(false);
        expandedEntry = entry;
        expandedRow = parent;
        parent.SetExpanded(true);
        secondaryEntries.Clear();
        switch (entry.category)
        {
            case Category.Attack:
                if (HasAttack(ActionType.MeleeAttack)) secondaryEntries.Add(MakeAction(ActionType.MeleeAttack));
                if (HasAttack(ActionType.RangedAttack)) secondaryEntries.Add(MakeAction(ActionType.RangedAttack));
                break;
            case Category.Skills:
                foreach (var skill in pc.availableSkills)
                {
                    if (skill == null) continue;
                    var current = skill;
                    secondaryEntries.Add(new Entry
                    {
                        title = () => current.skillName, available = () => CanExecute(ActionType.Skill, current),
                        cost = () => ActionResourceCost.Query(pc, ActionType.Skill, current),
                        execute = () => pc.StartSkillAttack(current),
                        showDetails = () => BattleScene.Ins.UM.skillTooltipUI.ShowTooltip(current, pc)
                    });
                }
                break;
            case Category.Items:
                foreach (var pack in GM.Ins.PLAYERPROFILE.itemPacks)
                {
                    var item = GM.Ins.marketSystem.marketItemListSO.GetData(pack.itemName);
                    if (item == null || item.equipType != EquipType.Consumable || pack.itemNum <= 0) continue;
                    var currentPack = pack;
                    secondaryEntries.Add(new Entry
                    {
                        title = () => item.itemName + " x" + currentPack.itemNum,
                        available = () => currentPack.itemNum > 0 && GM.Ins.PLAYERPROFILE.itemPacks.Contains(currentPack)
                            && CanExecute(ActionType.Item) && pc.ItemAvailable(item),
                        cost = () => ActionResourceCost.Query(pc, ActionType.Item), execute = () => pc.UseItem(item),
                        showDetails = () => BattleScene.Ins.UM.skillTooltipUI.ShowItemTip(item)
                    });
                }
                break;
            case Category.Orders:
                foreach (var order in pc.pieceData.orderProfiles)
                {
                    var current = order;
                    secondaryEntries.Add(new Entry
                    {
                        title = () => current.orderName, available = () => CanExecute(ActionType.GuardOrder),
                        cost = () => ActionResourceCost.Query(pc, ActionType.GuardOrder), execute = () => pc.StartOrderGraud(current)
                    });
                }
                break;
        }
        skillListPanel.SetActive(true);
        BindRows(secondaryEntries, secondaryRows, secondaryContent);
        emptyLabel.gameObject.SetActive(secondaryEntries.Count == 0);
        RefreshMenuLayout(true);
        RevealRows(secondaryRows);
    }

    private void BindRows(List<Entry> entries, List<ActionBtn> rows, RectTransform content)
    {
        ClearRows(rows);
        while (rows.Count < entries.Count) rows.Add(Instantiate(actionButtonPrefab, content, false));
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var row = rows[i];
            row.gameObject.SetActive(true);
            row.Bind(entry.title, entry.category != Category.None, entry.available, entry.cost,
                () => ClickEntry(entry, row), () => HoverEntry(entry, row), () => { if (hoveredRow == row) ClearHover(true); });
        }
    }

    private void ClickEntry(Entry entry, ActionBtn row)
    {
        if (!CanAct() || !entry.available()) return;
        if (entry.category != Category.None) { OpenCategory(entry, row); return; }
        // 先退出菜单及预览，再移交到原目标选择或执行流程。
        HidePanel();
        BattleScene.Ins.UM.BeginActionSelection(pc, entry.cost);
        entry.execute?.Invoke();
    }

    private void HoverEntry(Entry entry, ActionBtn row)
    {
        ClearHover();
        if (entry.category != Category.None || !CanAct()) return;
        hoveredEntry = entry;
        hoveredRow = row;
        previewViews.AddRange(pc.GetComponentsInChildren<ActionCostPreviewUI>(false));
        var state = BattleScene.Ins.UM.pieceStatePanel.GetComponent<ActionCostPreviewUI>();
        if (state != null) previewViews.Add(state);
        var info = BattleScene.Ins.UM.pieceInfoPanel.GetComponent<ActionCostPreviewUI>();
        if (info != null) previewViews.Add(info);
        RefreshHover();
        entry.showDetails?.Invoke();
    }

    private void RefreshHover()
    {
        if (hoveredEntry == null) return;
        if (!CanAct()) { ClearHover(); return; }
        if (hoveredRow == null || !hoveredRow.IsHovered) { ClearHover(true); return; }
        if (!hoveredEntry.available())
        {
            // 资源不足仅取消费用呼吸，不中断技能/道具说明的显示生命周期。
            foreach (var view in previewViews) if (view != null) view.Clear();
            return;
        }
        var cost = hoveredEntry.cost();
        foreach (var view in previewViews) if (view != null) view.Show(pc.unitAttrCenter, cost);
    }

    private void ClearHover(bool retainDetails = false)
    {
        bool hasDetails = hoveredEntry != null && hoveredEntry.showDetails != null;
        foreach (var view in previewViews) if (view != null) view.Clear();
        previewViews.Clear();
        hoveredEntry = null;
        hoveredRow = null;
        // 费用预览立即退出；详情允许跨过棋盘空隙，进入后可独立阅读和滚动。
        if (retainDetails && hasDetails)
        {
            detailsPending = true;
            detailsCloseAt = Time.unscaledTime + detailsTransferDelay;
            return;
        }
        detailsPending = false;
        if (BattleScene.Ins != null && BattleScene.Ins.UM != null) BattleScene.Ins.UM.skillTooltipUI?.HideTooltip();
    }

    private void RefreshDetails()
    {
        if (!detailsPending) return;
        var tooltip = BattleScene.Ins.UM.skillTooltipUI;
        if (tooltip == null || !tooltip.gameObject.activeInHierarchy) { detailsPending = false; return; }
        if (tooltip.ContainsPointer)
            detailsCloseAt = Time.unscaledTime + detailsTransferDelay;
        else if (Time.unscaledTime >= detailsCloseAt)
        {
            detailsPending = false;
            tooltip.HideTooltip();
        }
    }

    private static void ClearRows(List<ActionBtn> rows)
    {
        foreach (var row in rows)
        {
            row.ResetBinding();
            row.gameObject.SetActive(false);
        }
    }

    /// <summary>关闭两级菜单，清理悬停预览、展开锁与尚未结束的动画。</summary>
    public void HidePanel() => gameObject.SetActive(false);
    private void OnDisable()
    {
        ClearHover();
        ClearRows(primaryRows);
        ClearRows(secondaryRows);
        expandedRow = null;
        expandedEntry = null;
        if (menuReady) skillListPanel.SetActive(false);
    }
}
