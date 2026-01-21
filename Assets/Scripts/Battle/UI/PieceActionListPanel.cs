using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class PieceActionListPanel : SerializedMonoBehaviour
{
   [SerializeField] [ReadOnly]
    private PieceController pc;
    public List<Button> actionButtons;
    private Dictionary<ActionType, Button> actionButtonDic = new();
    public GameObject skillListPanel;
    public List<Button> skillButtons;
    //public Dictionary<ActionType, UnityAction> actionDic = new();

    public void Init(PieceController pc)
    {
        this.pc = pc;
        // 遍历所有ActionType枚举值
        int i = 0;
        foreach (ActionType actionType in Enum.GetValues(typeof(ActionType)))
        {
            // 查找对应名称的按钮
            Button button = actionButtons[i++];
            if (button != null)
            {
                // 为按钮添加点击事件监听器
                ActionType capturedActionType = actionType; // 捕获当前的actionType
                actionButtonDic[actionType] = button;
                button.onClick.AddListener(() => OnActionButtonClicked(capturedActionType));
                button.GetComponentInChildren<Text>().text = actionType.ToString();
            }
        }
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }
        for (int j = 0; j < pc.availableSkills.Count; j++)
        {
            // 更新所有技能按钮
            if (j<skillButtons.Count)
            {
                skillButtons[j].gameObject.SetActive(true);
                skillButtons[j].GetComponentInChildren<Text>().text = pc.availableSkills[j].skillName;
                int capturedIndex = j; // 捕获当前索引
                skillButtons[j].onClick.AddListener(() => {
                    gameObject.SetActive(false);
                    pc.StartSkillAttack(pc.availableSkills[capturedIndex]);
                });
            }
        }
    }

    private void OnEnable()
    {
        foreach (var button in actionButtonDic)
        {
            button.Value.gameObject.SetActive(
                pc.unitAttrCenter.CurMovePoint>=1 && pc.availableActions.Contains(button.Key));
        }
        foreach (var interactArea in pc.interactAreas)
        {
            if (actionButtonDic.ContainsKey(interactArea.actionType))
            {
                actionButtonDic[interactArea.actionType].gameObject.SetActive(true);
                actionButtonDic[interactArea.actionType].onClick.AddListener(interactArea.TriggerAction);
            }
        }
        skillListPanel.SetActive(false);
    }

    private void OnActionButtonClicked(ActionType actionType)
    {
        
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
                skillListPanel.SetActive(true);
                return;
                break;
            case ActionType.待机:
                pc.isIdle = true;
                // 清除剩余行动力
                pc.unitAttrCenter.CostMP(pc.unitAttrCenter.CurMovePoint);
                break;
            case ActionType.扫描:
                break;
            case ActionType.远程攻击:
                pc.StartNormalAttack(true);
                break;
            case ActionType.重新装填:
                pc.ReloadAmmo();
                break;
            case ActionType.道具:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null);
        }
        gameObject.SetActive(false);
    }
    
}