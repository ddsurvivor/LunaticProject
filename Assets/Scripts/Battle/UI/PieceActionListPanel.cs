using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;


public partial class PieceActionListPanel : SerializedMonoBehaviour
{
    [SerializeField] [ReadOnly] private PieceController pc;
    public List<Button> actionButtons;
    private Dictionary<ActionType, Button> actionButtonDic = new();
    public GameObject skillListPanel;

    public List<Button> skillButtons;

    //public Dictionary<ActionType, UnityAction> actionDic = new();


    /// <summary>初始化现有按钮与响应布局；重复调用不会累积监听器。</summary>
    public void Init()
    {
        if (menuReady) return;
        BuildMenuLayout();
        int i = 0;
        foreach (ActionType actionType in Enum.GetValues(typeof(ActionType)))
        {
            // 查找对应名称的按钮
            if (i >= actionButtons.Count) break;
            Button button = actionButtons[i++];
            if (button != null)
            {
                // 为按钮添加点击事件监听器
                ActionType capturedActionType = actionType; // 捕获当前的actionType
                actionButtonDic[actionType] = button;
                button.onClick.AddListener(() => OnActionButtonClicked(capturedActionType));
                ActionBtn actionBtnComponent = button.GetComponent<ActionBtn>();
                if (actionBtnComponent != null)
                {
                    button.GetComponent<ActionBtn>().actionNameText.text = actionType.ToString();
                    button.GetComponent<ActionBtn>().costText.text =
                        actionType == ActionType.待机
                            ? "全部AP"
                            : GM.Ins.DM.gameConstSO.GetActionPointCost(actionType) + "AP";
                }
                else
                {
                    button.GetComponentInChildren<Text>().text = actionType.ToString();
                }
            }
        }
    }
    /// <summary>
    /// 显示特定棋子的行动面板
    /// </summary>
    public void ShowPanel(PieceController pc)
    {
        if (!BattleScene.Ins.BM.CanControlPiece(pc)) return;
        Init();
        this.pc = pc;
        HideTooltip();
        foreach (var pair in actionButtonDic)
        {
            ActionType type = pair.Key;
            pair.Value.onClick.RemoveAllListeners();
            pair.Value.onClick.AddListener(() => OnActionButtonClicked(type));
            pair.Value.interactable = type == ActionType.待机
                ? pc.unitAttrCenter.CurMovePoint > 0 : pc.unitAttrCenter.HasMP(type);
            pair.Value.GetComponent<ActionBtn>()?.SetSelected(false);
            SetRow(pair.Value, type.ToString(), type == ActionType.待机
                ? pc.unitAttrCenter.CurMovePoint + "AP"
                : GM.Ins.DM.gameConstSO.GetActionPointCost(type) + "AP",
                type == ActionType.技能 || type == ActionType.道具 || type == ActionType.警戒指令);
        }

        // 1. 状态更新：装填动作逻辑
        if (pc.unitAttrCenter.AmmoCount < pc.unitAttrCenter.MaxAmmoCount)
        {
            if (!pc.availableActions.Contains(ActionType.重新装填))
                pc.availableActions.Add(ActionType.重新装填);
        }
        else
        {
            pc.availableActions.Remove(ActionType.重新装填);
        }

        // 只展示当前角色与环境允许的动作，待机始终保留入口。
        HashSet<ActionType> visibleActions = new HashSet<ActionType>();
        // 无全局结束按钮时，任何可操作棋子都必须能主动放弃剩余行动力。
        visibleActions.Add(ActionType.待机);

        // 筛选基础动作
        foreach (var pair in actionButtonDic)
        {
            if (pc.availableActions.Contains(pair.Key))
            {
                visibleActions.Add(pair.Key);
            }
        }

        // 筛选并重新绑定环境交互动作
        foreach (var interactArea in pc.interactAreas)
        {
            if (actionButtonDic.ContainsKey(interactArea.actionType))
            {
                if (!interactArea.ableToTrigger)
                {
                    visibleActions.Remove(interactArea.actionType);
                    continue;
                }

                visibleActions.Add(interactArea.actionType);

                // 动态绑定事件
                Button btn = actionButtonDic[interactArea.actionType];
                btn.onClick.RemoveAllListeners();

                InteractArea currentArea = interactArea;
                btn.onClick.AddListener(() =>
                {
                    if (!BattleScene.Ins.BM.CanControlPiece(pc) || BattleScene.Ins.BM.HasCombatPresentation
                        || !currentArea.ableToTrigger) return;
                    if (!pc.unitAttrCenter.CostMP(currentArea is LadderArea
                        ? ActionType.攀爬 : ActionType.交互)) return;
                    currentArea.TriggerAction(pc);
                    HidePanel();
                });
            }
        }


        foreach (var pair in actionButtonDic)
        {
            Button btn = pair.Value;
            if (visibleActions.Contains(pair.Key))
            {
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }


        skillListPanel.SetActive(false);
        gameObject.SetActive(true);

        RefreshMenuLayout(true);
    }

    /// <summary>关闭一级、二级菜单并清除悬停提示。</summary>
    public void HidePanel()
    {
        HideTooltip();
        skillListPanel.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnActionButtonClicked(ActionType actionType)
    {
        if (!BattleScene.Ins.BM.CanControlPiece(pc) || BattleScene.Ins.BM.HasCombatPresentation) return;
        if (actionType == ActionType.待机)
        {
            if (pc.unitAttrCenter.CurMovePoint <= 0) return;
        }
        else if (!pc.unitAttrCenter.HasMP(actionType)) return;
        BattleScene.Ins.UM.pieceInfoPanel.StartMpIconsBlink();
        Debug.Log($"Action Button Clicked: {actionType}");
        // 在这里处理按钮点击事件
        switch (actionType)
        {
            case ActionType.移动:
                BattleScene.Ins.CM.StartDarg(pc);
                break;
            case ActionType.近战攻击:
                pc.StartNormalAttack();
                break;
            case ActionType.技能:
                OpenSkillListPanel();
                return;
            case ActionType.待机:
                // 待机没有固定行动力消耗，直接消耗当前全部剩余行动力。
                if (!pc.unitAttrCenter.CostMP(pc.unitAttrCenter.CurMovePoint)) return;
                pc.isIdle = true;
                pc.PlayAudio(actionType);
                break;
            case ActionType.扫描:
                if (!pc.unitAttrCenter.CostMP(ActionType.扫描)) return;
                pc.PlayAudio(actionType);
                break;
            case ActionType.攀爬:
                break;
            case ActionType.远程攻击:
                pc.StartNormalAttack(true);
                break;
            case ActionType.重新装填:
                if (!pc.unitAttrCenter.CostMP(ActionType.重新装填)) return;
                pc.ReloadAmmo();
                break;
            case ActionType.道具:
                //pc.PlayAudio(actionType);
                // 打开道具二级菜单
                OpenItemPanel();
                return;
            case ActionType.交互:
                pc.PlayAudio(actionType);
                break;
            case ActionType.警戒指令:
                OpenCommandPanel();
                return;
            default:
                Debug.LogWarning($"{actionType} 未实现");
                break;
        }

        HidePanel();
        //gameObject.SetActive(false);
    }

    private void OpenSkillListPanel()
    {
        PrepareCategory();
        skillListPanel.SetActive(true);
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }

        EnsureSkillButtons(pc.availableSkills.Count);
        SelectCategory(ActionType.技能);
        for (int j = 0; j < pc.availableSkills.Count; j++)
        {
            // 更新所有技能按钮
            if (j < skillButtons.Count)
            {
                int capturedIndex = j; // 捕获当前索引
                skillButtons[j].gameObject.SetActive(true);
                skillButtons[j].interactable = pc.SkillAvailable(pc.availableSkills[capturedIndex]);
                ActionBtn actionBtnComponent =  skillButtons[j].GetComponent<ActionBtn>();
                if (actionBtnComponent != null)
                {
                    skillButtons[j].GetComponentInChildren<Text>().text =
                        pc.availableSkills[capturedIndex].skillName;
                    skillButtons[j].GetComponent<ActionBtn>().costText.text =
                        GM.Ins.DM.gameConstSO.GetActionPointCost(ActionType.技能) + "AP";
                }
                else
                {
                    skillButtons[j].GetComponentInChildren<Text>().text =
                        pc.availableSkills[capturedIndex].skillName;
                }
                skillButtons[j].onClick.RemoveAllListeners();
                skillButtons[j].onClick.AddListener(() =>
                {
                    if (!BattleScene.Ins.BM.CanControlPiece(pc) || BattleScene.Ins.BM.HasCombatPresentation
                        || !pc.SkillAvailable(pc.availableSkills[capturedIndex])) return;
                    HidePanel();
                    pc.StartSkillAttack(pc.availableSkills[capturedIndex]);
                    BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
                });

                HoverScale hoverScale = skillButtons[j].GetComponent<HoverScale>();
                hoverScale.onHoverEnter.RemoveAllListeners();
                hoverScale.onHoverExit.RemoveAllListeners();
                hoverScale.onHoverEnter.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.ShowTooltip(
                        pc.availableSkills[capturedIndex], pc);
                });
                hoverScale.onHoverExit.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
                });
            }
        }

        RefreshMenuLayout(true);
    }

    /// <summary>
    /// 显示道具
    /// </summary>
    private void OpenItemPanel()
    {
        // 复用技能按钮
        PrepareCategory();
        skillListPanel.SetActive(true);
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }

        EnsureSkillButtons(GM.Ins.PLAYERPROFILE.itemPacks.Count);
        SelectCategory(ActionType.道具);
        for (int j = 0; j < GM.Ins.PLAYERPROFILE.itemPacks.Count; j++)
        {
            // 更新所有技能按钮
            if (j >= skillButtons.Count) return;
            ItemData itemData =
                GM.Ins.marketSystem.marketItemListSO.GetData(GM.Ins.PLAYERPROFILE.itemPacks[j]
                    .itemName);
            if (itemData != null && itemData.equipType == EquipType.Consumable && GM.Ins.PLAYERPROFILE.itemPacks[j].itemNum > 0)
            {
                int capturedIndex = j; // 捕获当前索引
                int num = GM.Ins.PLAYERPROFILE.itemPacks[j].itemNum;
                skillButtons[j].gameObject.SetActive(true);
                skillButtons[j].interactable = pc.ItemAvailable(itemData);
                ActionBtn actionBtnComponent =  skillButtons[j].GetComponent<ActionBtn>();
                if (actionBtnComponent != null)
                {
                    skillButtons[j].GetComponentInChildren<Text>().text =
                        itemData.itemName.ToString() + $" x{num}";
                    skillButtons[j].GetComponent<ActionBtn>().costText.text =
                        GM.Ins.DM.gameConstSO.GetActionPointCost(ActionType.道具) + "AP";
                }
                else
                {
                    skillButtons[j].GetComponentInChildren<Text>().text =
                        itemData.itemName.ToString() + $" x{num}";
                }
                skillButtons[j].onClick.RemoveAllListeners();
                skillButtons[j].onClick.AddListener(() =>
                {
                    if (!BattleScene.Ins.BM.CanControlPiece(pc) || BattleScene.Ins.BM.HasCombatPresentation
                        || !pc.ItemAvailable(itemData) || GM.Ins.PLAYERPROFILE.itemPacks[capturedIndex].itemNum <= 0) return;
                    HidePanel();
                    pc.UseItem(itemData);
                });

                HoverScale hoverScale = skillButtons[j].GetComponent<HoverScale>();
                hoverScale.onHoverEnter.RemoveAllListeners();
                hoverScale.onHoverExit.RemoveAllListeners();
                hoverScale.onHoverEnter.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.ShowItemTip(itemData);
                });
                hoverScale.onHoverExit.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
                });
            }
        }

        RefreshMenuLayout(true);
    }

    private void OpenCommandPanel()
    {
        // 复用技能按钮
        PrepareCategory();
        skillListPanel.SetActive(true);
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }

        EnsureSkillButtons(pc.pieceData.orderProfiles.Count);
        SelectCategory(ActionType.警戒指令);
        for (int i = 0; i < pc.pieceData.orderProfiles.Count; i++)
        {
            int index = i;
            skillButtons[index].gameObject.SetActive(true);
            ActionBtn actionBtnComponent =  skillButtons[index].GetComponent<ActionBtn>();
            if (actionBtnComponent != null)
            {
                skillButtons[index].GetComponentInChildren<Text>().text =
                    pc.pieceData.orderProfiles[index].orderName;
                skillButtons[index].GetComponent<ActionBtn>().costText.text =
                    GM.Ins.DM.gameConstSO.GetActionPointCost(ActionType.警戒指令) + "AP";
            }
            else
            {
                skillButtons[index].GetComponentInChildren<Text>().text =
                    pc.pieceData.orderProfiles[index].orderName;
            }
            
            skillButtons[index].interactable = pc.unitAttrCenter.HasMP(ActionType.警戒指令);
            skillButtons[index].onClick.RemoveAllListeners();
            skillButtons[index].onClick.AddListener(() =>
            {
                if (!BattleScene.Ins.BM.CanControlPiece(pc) || BattleScene.Ins.BM.HasCombatPresentation
                    || !pc.unitAttrCenter.HasMP(ActionType.警戒指令)) return;
                HidePanel();
                // 进行警戒功能
                pc.StartOrderGraud(pc.pieceData.orderProfiles[index]);
            });

            HoverScale hoverScale = skillButtons[index].GetComponent<HoverScale>();
            hoverScale.onHoverEnter.RemoveAllListeners();
            hoverScale.onHoverExit.RemoveAllListeners();
        }

        RefreshMenuLayout(true);
    }

    private void OnDisable()
    {
        HideTooltip();
    }
}
