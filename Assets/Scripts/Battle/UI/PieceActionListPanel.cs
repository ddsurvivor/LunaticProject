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
        if (pc.unitAttrCenter.CurMovePoint < 1) return;
        this.pc = pc;

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

        // 2. 收集本次需要显示的“目标 Transform 列表”
        List<Transform> targetsToAnimate = new List<Transform>();
        HashSet<ActionType> visibleActions = new HashSet<ActionType>();

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
                    if (!pc.unitAttrCenter.CostMP()) return;
                    currentArea.TriggerAction(pc);
                    HidePanel(); 
                });
            }
        }

        // 3. 根据筛选结果，控制物体的显隐，并填充动画队列
        foreach (var pair in actionButtonDic)
        {
            Button btn = pair.Value;
            if (visibleActions.Contains(pair.Key))
            {
                btn.gameObject.SetActive(true);
                targetsToAnimate.Add(btn.transform); // 只塞入需要表现的组件
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // 4. 打开面板并播放纯粹的动画
        skillListPanel.SetActive(false);
        gameObject.SetActive(true);

        PlayShowAnimation(targetsToAnimate);
    }

    /// <summary>
    /// 纯粹的表现层动画：依次展现传入的 Transform 列表
    /// </summary>
    /// <param name="targets">需要播放动画的物体列表</param>
    private void PlayShowAnimation(List<Transform> targets)
    {
        if (showSequence != null && showSequence.IsPlaying())
        {
            showSequence.Kill(true);
        }

        int count = targets.Count;
        if (count == 0) return;

        showSequence = DOTween.Sequence();

        // 基础时间配置
        float totalDuration = 0.5f;
        float perBtnDuration = 0.25f; 
        float interval = count > 1 ? (totalDuration - perBtnDuration) / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            Transform t = targets[i];
            
            // 获取或添加 CanvasGroup 用于控制透明度
            CanvasGroup cg = t.GetComponent<CanvasGroup>();
            if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();

            // 动画前置状态初始化
            cg.alpha = 0f;
            t.localScale = Vector3.one * 0.7f;

            float delayTime = i * interval;

            // 纯粹基于 Transform 和 CanvasGroup 的动画组织
            showSequence.Insert(delayTime, cg.DOFade(1f, perBtnDuration).SetEase(Ease.OutCubic));
            showSequence.Insert(delayTime, t.DOScale(1f, perBtnDuration).SetEase(Ease.OutBack));
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
                OpenCommandPanel();
                return;
                break;
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
        List<Transform> targetsToAnimate = new List<Transform>();
        for (int j = 0; j < pc.availableSkills.Count; j++)
        {
            // 更新所有技能按钮
            if (j<skillButtons.Count)
            {
                int capturedIndex = j; // 捕获当前索引
                skillButtons[j].gameObject.SetActive(true);
                targetsToAnimate.Add(skillButtons[j].transform); // 只塞入需要表现的组件
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
        
        PlayShowAnimation(targetsToAnimate);
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
        List<Transform> targetsToAnimate = new List<Transform>();
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
                targetsToAnimate.Add(skillButtons[j].transform); // 只塞入需要表现的组件
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
        
        PlayShowAnimation(targetsToAnimate);
    }

    private void OpenCommandPanel()
    {
        // 复用技能按钮
        skillListPanel.SetActive(true);
        foreach (var skillButton in skillButtons)
        {
            skillButton.gameObject.SetActive(false);
            skillButton.onClick.RemoveAllListeners();
        }
        List<Transform> targetsToAnimate = new List<Transform>();
        for (int i = 0; i < pc.pieceData.orderProfiles.Count; i++)
        {
            int index = i;
            skillButtons[index].gameObject.SetActive(true);
            skillButtons[index].GetComponentInChildren<Text>().text = 
                pc.pieceData.orderProfiles[index].orderName;
            skillButtons[index].onClick.RemoveAllListeners();
            skillButtons[index].onClick.AddListener(() => {
                gameObject.SetActive(false);
                // 进行警戒功能
                pc.StartOrderGraud(pc.pieceData.orderProfiles[index]);
            });
        }
        PlayShowAnimation(targetsToAnimate);
    }
    
}