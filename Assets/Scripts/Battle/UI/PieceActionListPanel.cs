using System;
using System.Collections.Generic;
using DG.Tweening;
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
    private Sequence showSequence;

    public void Init()
    {
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
    }
    /*public void Init(PieceController pc)
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
    }*/

    public void Update()
    {
        if (pc!= null)
        {
            // 计算棋子在屏幕中的位置，更新行动面板的位置
            Vector3 screenPos = Camera.main.WorldToScreenPoint(pc.transform.position);
            float offsetX = 150;
            if (screenPos.x + 400f > 1920f) {
                offsetX = -220f;
            }
            transform.position = screenPos + new Vector3(offsetX, 100, 0);
        }
    }

    // private void OnEnable()
    // {
    //     foreach (var button in actionButtonDic)
    //     {
    //         button.Value.gameObject.SetActive(
    //             pc.unitAttrCenter.CurMovePoint>=1 && pc.availableActions.Contains(button.Key));
    //     }
    //     foreach (var interactArea in pc.interactAreas)
    //     {
    //         if (actionButtonDic.ContainsKey(interactArea.actionType))
    //         {
    //             actionButtonDic[interactArea.actionType].gameObject.SetActive(true);
    //             actionButtonDic[interactArea.actionType].onClick.AddListener(()=>interactArea.TriggerAction(pc));
    //         }
    //     }
    //     skillListPanel.SetActive(false);
    // }
    
    /// <summary>
    /// 显示特定棋子的行动面板
    /// </summary>
    public void ShowPanel(PieceController pc)
    {
        // 1. 基础行动力检查
        if (pc.unitAttrCenter.CurMovePoint < 1)
        {
            return;
        }
        this.pc = pc;

        // 2. 状态更新：装填动作逻辑
        if (pc.unitAttrCenter.AmmoCount < pc.unitAttrCenter.MaxAmmoCount)
        {
            if (!pc.availableActions.Contains(ActionType.重新装填))
                pc.availableActions.Add(ActionType.重新装填);
        }
        else
        {
            pc.availableActions.Remove(ActionType.重新装填);
        }

        // 3. 收集当前真正需要显示的按钮和它们对应的交互数据
        // 使用一个字典或列表来记录本次需要展示的 Action
        List<ActionType> activeActions = new List<ActionType>();

        // 检查基础可用动作
        foreach (var pair in actionButtonDic)
        {
            if (pc.availableActions.Contains(pair.Key))
            {
                activeActions.Add(pair.Key);
            }
        }

        // 检查环境/区域交互动作，并动态绑定最新的触发事件
        foreach (var interactArea in pc.interactAreas)
        {
            if (actionButtonDic.ContainsKey(interactArea.actionType))
            {
                // 如果不能触发，从显示列表移除
                if (!interactArea.ableToTrigger)
                {
                    activeActions.Remove(interactArea.actionType);
                    continue;
                }

                if (!activeActions.Contains(interactArea.actionType))
                {
                    activeActions.Add(interactArea.actionType);
                }

                // 重新绑定事件（针对特定区域交互）
                Button btn = actionButtonDic[interactArea.actionType];
                btn.onClick.RemoveAllListeners();
                
                // 这里的闭包捕获了当前的 interactArea 和 pc
                InteractArea currentArea = interactArea; 
                btn.onClick.AddListener(() =>
                {
                    if (!pc.unitAttrCenter.CostMP()) return;
                    currentArea.TriggerAction(pc);
                    // 动作触发后，通常需要关闭面板或刷新面板
                    HidePanel(); 
                });
            }
        }

        // 4. 彻底解耦：开始执行“灵动”的入场动画
        skillListPanel.SetActive(false);
        gameObject.SetActive(true); // 激活面板自身

        PlayShowAnimation(activeActions);
    }

    /// <summary>
    /// 播放按钮依次显示的精美动画
    /// </summary>
    private void PlayShowAnimation(List<ActionType> activeActions)
    {
        // 如果上一次的动画还在播放，先杀掉
        if (showSequence != null && showSequence.IsPlaying())
        {
            showSequence.Kill(true);
        }

        showSequence = DOTween.Sequence();

        // 收集所有需要播放动画的按钮组件
        List<CanvasGroup> btnsToAnimate = new List<CanvasGroup>();

        foreach (var pair in actionButtonDic)
        {
            Button btn = pair.Value;
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg == null) cg = btn.gameObject.AddComponent<CanvasGroup>();

            if (activeActions.Contains(pair.Key))
            {
                btn.gameObject.SetActive(true);
                // 动画前置状态：透明度为0，稍微缩小，或者往下偏移一点
                cg.alpha = 0;
                btn.transform.localScale = Vector3.one * 0.7f; 
                
                btnsToAnimate.Add(cg);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // 计算动画节奏
        int count = btnsToAnimate.Count;
        if (count == 0) return;

        // 总时间 0.5秒，平分给每个按钮的间隔时间和自身动画时间
        float totalDuration = 0.5f;
        float perBtnDuration = 0.25f; // 每个按钮自身淡入动画的时间
        // 计算错开的时间间隔 (Stagger)
        float interval = count > 1 ? (totalDuration - perBtnDuration) / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            CanvasGroup cg = btnsToAnimate[i];
            Transform trans = cg.transform;
            float delayTime = i * interval;

            // 无论是透明度还是缩放，都加入到 Sequence 中，并通过 SetDelay 实现扇形展开效果
            showSequence.Insert(delayTime, cg.DOFade(1f, perBtnDuration).SetEase(Ease.OutCubic));
            showSequence.Insert(delayTime, trans.DOScale(1f, perBtnDuration).SetEase(Ease.OutBack)); // OutBack 带有一点点回弹，更灵动
        }
    }

    public void HidePanel()
    {
        if (showSequence != null) showSequence.Kill();
        gameObject.SetActive(false);
    }

    /*
    /// <summary>
    /// 显示特定棋子的行动面板
    /// </summary>
    /// <param name="pc"></param>
    public void ShowPanel(PieceController pc)
    {
        if (pc.unitAttrCenter.CurMovePoint<1)
        {
            return;
        }
        this.pc = pc;
        if (pc.unitAttrCenter.AmmoCount < pc.unitAttrCenter.MaxAmmoCount)
        {
            pc.availableActions.Add(ActionType.重新装填);
        }
        else
        {
            pc.availableActions.Remove(ActionType.重新装填);
        }

        foreach (var button in actionButtonDic)
        {
            button.Value.gameObject.SetActive(
                pc.unitAttrCenter.CurMovePoint>=1 && pc.availableActions.Contains(button.Key));
        }
        foreach (var interactArea in pc.interactAreas)
        {
            if (actionButtonDic.ContainsKey(interactArea.actionType))
            {
                if (!interactArea.ableToTrigger)
                {
                    actionButtonDic[interactArea.actionType].gameObject.SetActive(false);
                    continue;
                }
                actionButtonDic[interactArea.actionType].gameObject.SetActive(pc.unitAttrCenter.CurMovePoint>=1);
                actionButtonDic[interactArea.actionType].onClick.RemoveAllListeners();
                actionButtonDic[interactArea.actionType].onClick.AddListener(()=>
                {
                    if(!pc.unitAttrCenter.CostMP()) return;
                    interactArea.TriggerAction(pc);
                });
            }
        }
        skillListPanel.SetActive(false);
        
        gameObject.SetActive(true);
    }
    */

    private void OnActionButtonClicked(ActionType actionType)
    {
        if(!pc.unitAttrCenter.HasMP())return;
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
                break;
            case ActionType.待机:
                pc.isIdle = true;
                // 清除剩余行动力
                pc.unitAttrCenter.CostMP(pc.unitAttrCenter.CurMovePoint);
                pc.PlayAudio(actionType);
                break;
            case ActionType.扫描:
                pc.PlayAudio(actionType);
                break;
            case ActionType.攀爬:
                break;
            case ActionType.远程攻击:
                pc.StartNormalAttack(true);
                break;
            case ActionType.重新装填:
                pc.unitAttrCenter.CostMP();
                pc.ReloadAmmo();
                break;
            case ActionType.道具:
                //pc.PlayAudio(actionType);
                // 打开道具二级菜单
                OpenItemPanel();
                 return;
                break;
            case ActionType.交互:
                pc.PlayAudio(actionType);
                break;
            case ActionType.指令:
                
            default:
                Debug.LogWarning($"{actionType} 未实现");
                break;
        }
        gameObject.SetActive(false);
    }
    
    private void OpenSkillListPanel()
    {
        skillListPanel.SetActive(true);
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
                int capturedIndex = j; // 捕获当前索引
                skillButtons[j].gameObject.SetActive(true);
                skillButtons[j].enabled = pc.SkillAvailable(pc.availableSkills[capturedIndex]);
                skillButtons[j].GetComponentInChildren<Text>().text = pc.availableSkills[capturedIndex].skillName;
                skillButtons[j].onClick.RemoveAllListeners();
                skillButtons[j].onClick.AddListener(() => {
                    gameObject.SetActive(false);
                    pc.StartSkillAttack(pc.availableSkills[capturedIndex]);
                    BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
                });
                
                HoverScale hoverScale = skillButtons[j].GetComponent<HoverScale>();
                hoverScale.onHoverEnter.RemoveAllListeners();
                hoverScale.onHoverExit.RemoveAllListeners();
                hoverScale.onHoverEnter.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.ShowTooltip(pc.availableSkills[capturedIndex]);
                });
                hoverScale.onHoverExit.AddListener(() =>
                {
                    BattleScene.Ins.UM.skillTooltipUI.HideTooltip();
                });
            }
        }
    }

    /// <summary>
    /// 显示道具
    /// </summary>
    private void OpenItemPanel()
    {
        // 复用技能按钮
        skillListPanel.SetActive(true);
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }
        for (int j = 0; j < GM.Ins.PLAYERPROFILE.itemPacks.Count; j++)
        {
            // 更新所有技能按钮
            if (j >= skillButtons.Count) return;
            ItemData itemData =
                GM.Ins.marketSystem.marketItemListSO.GetData(GM.Ins.PLAYERPROFILE.itemPacks[j]
                    .itemName);
            if (itemData!=null && itemData.equipType == EquipType.Consumable)
            {
                int capturedIndex = j; // 捕获当前索引
                int num = GM.Ins.PLAYERPROFILE.itemPacks[j].itemNum;
                skillButtons[j].gameObject.SetActive(true);
                skillButtons[j].enabled = pc.ItemAvailable(itemData);
                skillButtons[j].GetComponentInChildren<Text>().text =
                    itemData.itemName.ToString() + $" x{num}";
                skillButtons[j].onClick.RemoveAllListeners();
                skillButtons[j].onClick.AddListener(() => {
                    gameObject.SetActive(false);
                    pc.UseItem(itemData);
                });
            }
        }
    }

    private void OpenCommandPanel()
    {
        // 打开指令二级菜单
        skillButtons[0].gameObject.SetActive(true);
        skillButtons[0].GetComponentInChildren<Text>().text = "近战指令";
        skillButtons[0].onClick.RemoveAllListeners();
        skillButtons[0].onClick.AddListener(() => {
            // 进行警戒功能
        });
        
        skillButtons[1].gameObject.SetActive(true);
        skillButtons[1].GetComponentInChildren<Text>().text = "远程指令";
        skillButtons[1].onClick.RemoveAllListeners();
        skillButtons[1].onClick.AddListener(() => {
            // 进行警戒功能
        });
    }
    
}