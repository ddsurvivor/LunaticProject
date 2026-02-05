using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 棋子控制器
/// </summary>
public class PieceController : MonoBehaviour
{
    [Header("引用")]
    //public string pieceName;
    public UnitAttrCenter unitAttrCenter; // 单位属性中心

    [FormerlySerializedAs("_rangeUI")] [SerializeField]
    public RangeUI rangeUI; // 范围UI

    [SerializeField] private PieceActionListPanel _actionListPanel; // 棋子动作列表面板

    [SerializeField] public PieceDisplay pieceDisplay;
    public GameObject uiCanvas;// UI画布

    [Header("配置")] [SerializeField] [ReadOnly]
    private PieceData _pieceData;
    public PieceData pieceData => _pieceData;

    public int pieceID; // 棋子ID

    [HideInInspector] public PlayerController player;


    [Header("状态")] public bool isPlayerPiece; // 是否是玩家棋子

    public bool isDead => unitAttrCenter.CurHealth <= 0; // 是否死亡

    //private bool _isDragging = false; // 是否正在拖拽

    //private Vector3 _originalPosition; // 原始位置

    private CaverSlot _curCaverSlot; // 当前绑定的点位
    private LadderArea _curLadderArea; // 当前绑定的梯子区域

    // 当前攻击数据
    private bool _isAttacking = false; // 是否正在攻击
    private bool _isUsingSkill = false; // 是否正在使用技能
    [SerializeField] [ReadOnly] private AttackPack _attackPack; // 当前正在使用的攻击
    protected SkillPack _curAttackPack;
    protected ActionType _curAtkType;

    [SerializeField] [ReadOnly] private SkillPack _skillPack; // 当前正在使用的技能
    //[SerializeField] [ReadOnly] private int _damage;
    //[SerializeField] [ReadOnly] private DamageType _damageType;

    public List<InteractArea> interactAreas = new(); // 可交互区域列表

    public List<ActionType> availableActions = new(); // 可用动作列表

    public List<SkillPack> availableSkills = new(); // 可用技能列表

    //public bool isActived = false; // 是否被激活

    public bool isIdle; // 是否处于待机状态

    [Header("事件")] public UnityEvent OnInit;
    public UnityEvent OnTurnStart;
    public UnityEvent OnTurnEnd;
    


    public void Init(PlayerController player, PieceData pieceData = null)
    {
        this.player = player;
        //unitAttrCenter.Init();
        if (isPlayerPiece)
        {
            availableActions.Add(ActionType.移动);
            availableActions.Add(ActionType.近战攻击);
            availableActions.Add(ActionType.远程攻击);
            availableActions.Add(ActionType.待机); // 待机
            availableActions.Add(ActionType.重新装填); // 装填
            availableActions.Add(ActionType.技能); // 技能
        }
        //Debug.Log(_pieceDisplay.name);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        if (pieceData != null)
        {
            _pieceData = pieceData;
            availableSkills = pieceData?.skillPacks;
            unitAttrCenter.SetData(_pieceData);
        }
        else
        {
            unitAttrCenter.Init();
        }

        //if (_actionListPanel != null) _actionListPanel.Init(this);
        isIdle = true;
        OnInit?.Invoke();
    }

    private void Update()
    {
        if (!isPlayerPiece) return;
        if (_isAttacking)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CastAttack();
            }

            // 点击右键取消
            if (Input.GetMouseButtonDown(1))
            {
                _isAttacking = false;
                rangeUI.CloseRange();
            }
        }

        if (_isUsingSkill)
        {
            if (Input.GetMouseButtonDown(0))
            {
                //CheckEnemy();
                CastSkill();
            }

            // 点击右键取消
            if (Input.GetMouseButtonDown(1))
            {
                _isUsingSkill = false;
                rangeUI.CloseRange();
            }
        }
    }

    public void TurnStart()
    {
        OnTurnStart?.Invoke();
        if(isDead) return;
        unitAttrCenter.FullMovePoint();
        isIdle = false;
    }

    public void TurnEnd()
    {
        // if (_actionListPanel != null)
        // {
        //     _actionListPanel.gameObject.SetActive(false);
        // }
        BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);

        isIdle = true;
        OnTurnEnd?.Invoke();
    }

    public void ShowActionList()
    {
        if (!isPlayerPiece) return;
        //_actionListPanel.gameObject.SetActive(true);
        BattleScene.Ins.UM.pieceActionListPanel.ShowPanel(this);
        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(this);
        //BattleScene.Ins.UM.infoBox.ShowInfo(this);
    }

    public void StartDrag()
    {
        if (!isPlayerPiece) return;
        //_actionListPanel.gameObject.SetActive(false);
        BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
        if (_curCaverSlot != null)
        {
            _curCaverSlot.LeaveSlot(transform);
            _curCaverSlot = null;
        }

        if (_curLadderArea != null)
        {
            _curLadderArea.LeaveSlot(this);
            _curLadderArea = null;
        }
        //BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(this);
        //BattleScene.Ins.UM.teamPanel.OnSelectPiece(pieceID);
    }

    public void StopDrag()
    {
        //BattleScene.Ins.UM.infoBox.ShowInfo(this);
        if (!isPlayerPiece) return;
        PlayAudio(ActionType.移动);
        CheckActionPos();
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        _actionListPanel.gameObject.SetActive(true);
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
    }

    public void CancelSelect()
    {
        Debug.Log("取消选择棋子");
        _isAttacking = false;
        rangeUI.CloseRange();
        // _actionListPanel.gameObject.SetActive(false);
        BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
    }


    private bool CheckActionPos()
    {
        bool result = false;
        interactAreas.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f);
        foreach (var collider in hitColliders)
        {
            CaverSlot caverSlot = collider.transform.GetComponent<CaverSlot>();
            if (caverSlot != null && !caverSlot.isFull)
            {
                caverSlot.AddToSlot(transform);
                _curCaverSlot = caverSlot;
                result = true;
            }

            InteractArea interactArea = collider.transform.GetComponent<InteractArea>();
            if (interactArea != null)
            {
                interactAreas.Add(interactArea);
                result = true;
            }

            LadderArea ladderSlot = collider.transform.GetComponent<LadderArea>();
            if (ladderSlot != null)
            {
                interactAreas.Add(ladderSlot);
                result = true;
            }
        }

        return result;
    }

    public void StartNormalAttack(bool range = false)
    {
        _isAttacking = true;
        if (!range) // 近战攻击
        {
            _curAttackPack = _pieceData.meleeAtk;
            rangeUI?.ShowSkillRange(_curAttackPack);
            _curAtkType = ActionType.近战攻击;
            // _attackPack = new AttackPack(unitAttrCenter.attr.GetAtk(DamageType.Melee)
            //     , DamageType.Melee);
        }
        else // 远程攻击
        {
            if (unitAttrCenter.AmmoCount <= 0)
            {
                Debug.Log("弹药不足，无法进行远程攻击");
                _isAttacking = false;
                return;
            }

            //rangeUI?.ShowAttackRange(_pieceData.rangedAtk.rangeValue);
            _curAttackPack = _pieceData.rangedAtk;
            rangeUI?.ShowSkillRange(_curAttackPack);
            _curAtkType = ActionType.远程攻击;
            // _attackPack = new AttackPack(unitAttrCenter.attr.GetAtk(DamageType.Ranged)
            //     , DamageType.Ranged);
        }
    }

    /*private void CheckEnemy()
    {
        // 获取攻击目标点
        Vector3 atkPos = rangeUI.GetAtkPos();
        float attackRadius = 3f; // 可根据需要调整攻击半径
        int enemyLayer = LayerMask.GetMask("Enemy");

        // 检测球体范围内的所有敌人
        Collider[] hitColliders = Physics.OverlapSphere(atkPos, attackRadius);

        if (hitColliders.Length <= 0) return;
        foreach (var collider in hitColliders)
        {
            // 在这里处理攻击逻辑，比如对collider.transform进行伤害计算
            Debug.Log($"Attacked target: {collider.transform.name}");
            PieceController enemy = collider.transform.GetComponent<PieceController>();
            if (enemy != null && !enemy.isPlayerPiece)
            {
                if (!unitAttrCenter.CostMP()) return;
                Attack(enemy);
                // 结束攻击状态
                _isAttacking = false;
                rangeUI.CloseRange();
                return;
            }
        }
    }*/

    private void CastAttack()
    {
        // 根据范围获取所有棋子
        List<PieceController> targets = rangeUI.GetCurTargets;
        if(targets.Count <1)
        {
            Debug.Log("未选中任何目标，无法发动技能");
            return;
        }
        Transform atkPos = rangeUI.GetSkillTransform();
        if (atkPos != null && _curAttackPack.skillVFXType != 0)
        {
            ObjectPool.Ins.GenerateObject(
                _curAttackPack.skillVFXType,
                atkPos.position + Vector3.up * 3f,
                atkPos.localRotation);
        }

        Debug.Log("棋子攻击");
        CheckFace(targets[0].transform.position - transform.position);
        if (_curAtkType == ActionType.近战攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Attack, false, 1f);
            PlayAudio(ActionType.近战攻击);
        }
        else if (_curAtkType == ActionType.远程攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
            PlayAudio(ActionType.远程攻击);
        }


        // 聚能充能
        if (isPlayerPiece)
        {
            if (!BattleScene.Ins.BM.PlayerController.isBursting)
            {
                // 攻击充能
                BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.attackBurstCharge);
            }
        }

        BattleScene.Ins.BM.camera.FocusShake(targets[0].transform);
        
        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f
            , () =>
            {
                if (!unitAttrCenter.CostMP()) return;
                BattleScene.Ins.BM.PieceSkill(this, targets, _curAttackPack);
                
                // 结束攻击状态
                _isAttacking = false;
                rangeUI.CloseRange();
            }, false);
        
        // 技能聚能充能
    }


    /*public void Attack(PieceController enemy)
    {
        Debug.Log("棋子攻击");
        if (_curAtkType == ActionType.近战攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Attack, false, 1f);
            PlayAudio(ActionType.近战攻击);
        }
        else if (_curAtkType == ActionType.远程攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
            PlayAudio(ActionType.远程攻击);
        }


        // 聚能充能
        if (isPlayerPiece)
        {
            if (!BattleScene.Ins.BM.PlayerController.isBursting)
            {
                // 攻击充能
                BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.attackBurstCharge);
            }
        }

        BattleScene.Ins.BM.camera.FocusShake(enemy.transform);
        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            // 执行攻击
            BattleScene.Ins.BM.PieceSkill(this, new List<PieceController>(){enemy}, _curAttackPack);
        }, false);
    }*/

    public void Hurt()
    {
        // 聚能充能
        if (isPlayerPiece)
        {
            // 受伤充能
            BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.hurtBurstCharge);
        }

        Debug.Log($"{this.name} 受伤");
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Hit, false, 0.5f);
        transform.DOShakePosition(0.5f, 0.8f);
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
        if(uiCanvas!= null) uiCanvas.SetActive(true);
    }

    public virtual void Dead()
    {
        Debug.Log($"{this.name} 死亡");
        if(uiCanvas!= null) uiCanvas.SetActive(false);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, false, -1, () =>
        {
            if (!isPlayerPiece)
            {
                pieceDisplay.pieceSpriteRenderer.DOFade(0f, 0.8f).OnComplete(() =>
                {
                    this.gameObject.SetActive(false);
                    BattleScene.Ins.BM.PlayerCheckWin();
                });
            }
        });
    }

    /// <summary>
    /// 重新装填弹药
    /// </summary>
    public void ReloadAmmo()
    {
        Debug.Log("重新装填弹药");
        unitAttrCenter.FullAmmo();
        PlayAudio(ActionType.重新装填);
    }

    public void StartSkillAttack(SkillPack skillPack)
    {
        _isUsingSkill = true;
        rangeUI?.ShowSkillRange(skillPack);
        _skillPack = skillPack;
    }
    public bool SkillAvailable(SkillPack skillPack)
    {
        if (!unitAttrCenter.HasMana()) return false;
        if(!unitAttrCenter.HasItem(skillPack.consumeItems)) return false;
        return true;
    }

    /// <summary>
    /// 发动技能
    /// </summary>
    public virtual void CastSkill()
    {
        if(_skillPack == null) return;
        
        // 根据范围获取所有棋子
        List<PieceController> targets = rangeUI.GetCurTargets;
        if(targets.Count <1 && (_skillPack.target != SkillTarget.Area && _skillPack.target != SkillTarget.Self))
        {
            Debug.Log("未选中任何目标，无法发动技能");
            return;
        }
        Transform atkPos = rangeUI.GetSkillTransform();
        if (atkPos != null && _skillPack.skillVFXType != 0)
        {
            ObjectPool.Ins.GenerateObject(
                _skillPack.skillVFXType,
                atkPos.position + Vector3.up * 3f,
                atkPos.localRotation);
        }
        
        CheckFace(atkPos.transform.position - transform.position);
        Debug.Log($"{this.name}发动技能攻击{_skillPack.skillName}，targets数量：{targets.Count}");
        // 播放技能动画
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Skill, false, 1f);
        PlayAudio(_skillPack);
        
        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f
            , () =>
            {
                if (!unitAttrCenter.CostMP()) return;
                if(!unitAttrCenter.CostMana(_skillPack.mpCost)) return;
                if(!unitAttrCenter.CostItem(_skillPack.consumeItems)) return;
                BattleScene.Ins.BM.PieceSkill(this, targets, _skillPack, atkPos.position);
                // 结束攻击状态
                _isUsingSkill = false;
                rangeUI.CloseRange();
            }, false);
        
        // 技能聚能充能
    }


    // 更新朝向
    public void CheckFace(Vector3 direction)
    {
        // 如果targetPos在当前棋子左侧，则朝向左侧，否则朝向右侧，更新piece display
        // 由于棋子式斜45站立的，所以应该同时计算x轴和z轴
        if (direction.x < 0 )
        {
            pieceDisplay.FaceRight(!isPlayerPiece);
        }
        else if (direction.x > 0)
        {
            pieceDisplay.FaceRight(isPlayerPiece);
        }
    }
    
    public float GetRange(bool isNormalAtk)
    {
        return isNormalAtk ? _pieceData.meleeAtk.rangeValue : _pieceData.rangedAtk.rangeValue;
    }

    // ======= 音效 ======= //

    public void PlayAudio(ActionType actionType)
    {
        if (_pieceData == null)
        {
            return;
        }

        if (_pieceData.actionSounds.ContainsKey(actionType))
        {
            AudioClip clip = _pieceData.actionSounds[actionType];
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
        }
    }

    public void PlayAudio(SkillPack skillPack)
    {
        if (skillPack.skillSound != null)
        {
            AudioSource.PlayClipAtPoint(skillPack.skillSound, Camera.main.transform.position);
        }
    }
}