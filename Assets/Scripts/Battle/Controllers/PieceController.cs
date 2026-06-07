using System;
using System.Collections.Generic;
using DG.Tweening;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
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

    public GameObject hightlightEffect; // 高亮特效

    [SerializeField] private PieceActionListPanel _actionListPanel; // 棋子动作列表面板

    [SerializeField] public PieceDisplay pieceDisplay;
    //public GameObject uiCanvas; // UI画布
    //public HitInfoPanel hitInfoPanel;

    [Header("配置")] [SerializeField] [ReadOnly]
    private PieceData _pieceData;

    public PieceData pieceData => _pieceData;

    public Player playerData;

    public int pieceID; // 棋子ID

    [HideInInspector] public PlayerController player;


    [Header("状态")] public bool isPlayerPiece; // 是否是玩家棋子

    public bool isDead => unitAttrCenter.CurHealth <= 0; // 是否死亡

    //private bool _isDragging = false; // 是否正在拖拽

    //private Vector3 _originalPosition; // 原始位置

    private CaverSlot _curCaverSlot; // 当前绑定的点位
    public CaverSlot CurCaverSlot => _curCaverSlot;
    private LadderArea _curLadderArea; // 当前绑定的梯子区域

    // 当前攻击数据
    private bool _isAttacking = false; // 是否正在攻击
    private bool _isUsingSkill = false; // 是否正在使用技能
    public bool IsUsingSkill => _isUsingSkill || _isAttacking;
    [SerializeField] [ReadOnly] private AttackPack _attackPack; // 当前正在使用的攻击
    protected SkillPack _curAttackPack;
    protected ActionType _curAtkType;

    [SerializeField] [ReadOnly] private SkillPack _skillPack; // 当前正在使用的技能
    //[SerializeField] [ReadOnly] private int _damage;
    //[SerializeField] [ReadOnly] private DamageType _damageType;

    public List<InteractArea> interactAreas = new(); // 可交互区域列表

    public List<ActionType> availableActions = new(); // 可用动作列表

    public List<SkillPack> availableSkills = new(); // 可用技能列表

    public List<PassiveType> availablePassives = new(); // 可用被动技能列表

    //public bool isActived = false; // 是否被激活

    public bool isIdle; // 是否处于待机状态

    public bool cantControl; // 无法被控制（眩晕等状态）
    public bool ableMove = true; // 是否能移动
    public bool deadNotDelete = false; // 死亡后不删除，用于剧情需要
    [FoldoutGroup("事件")] public UnityEvent OnInit;
    [FoldoutGroup("事件")] public UnityEvent OnTurnStart;
    [FoldoutGroup("事件")] public UnityEvent OnTurnEnd;
    [FoldoutGroup("事件")] public UnityEvent OnHurt;
    [FoldoutGroup("事件")] public UnityEvent OnDead;


    public void Init(PlayerController player, PieceData pieceData = null)
    {
        this.player = player;
        this.playerData = GM.Ins.PLAYERPROFILE.GetPlayer(pieceID - 1);
        //unitAttrCenter.Init();
        if (isPlayerPiece)
        {
            availableActions.Add(ActionType.移动);
            availableActions.Add(ActionType.近战攻击);
            availableActions.Add(ActionType.远程攻击);
            availableActions.Add(ActionType.待机); // 待机
            //availableActions.Add(ActionType.重新装填); // 装填
            availableActions.Add(ActionType.技能); // 技能
            availableActions.Add(ActionType.道具);
        }

        //Debug.Log(_pieceDisplay.name);
        pieceDisplay?.ChangeDisplayState(PieceDisplayState.Idle);
        if (pieceData != null)
        {
            _pieceData = pieceData;
            availableSkills = pieceData?.skillPacks;
            unitAttrCenter.SetData(_pieceData, playerData);
            if (isPlayerPiece && GM.Ins.pieceHPInherit)
            {
                Player playerData = GM.Ins.PLAYERPROFILE.GetPlayer(pieceID - 1);
                if (playerData.curHealth > 0)
                {
                    // 只有当玩家当前血量大于0时才继承血量，否则按照默认值初始化，避免玩家死亡后再次进入战斗时棋子带着异常血量
                    unitAttrCenter.SetValues(playerData.curHealth, playerData.curMana
                        , playerData.curAmmo);
                }
            }
        }
        else
        {
            unitAttrCenter.Init();
        }

        InitComp();
        //if (_actionListPanel != null) _actionListPanel.Init(this);
        isIdle = true;
        OnInit?.Invoke();
    }

    private void Update()
    {
        if (cantControl) return;
        if (!isPlayerPiece) return;
        if (_isAttacking)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CastAttack();
                //CastSkill();
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

    public virtual void TurnStart()
    {
        OnTurnStart?.Invoke();
        if (isDead) return;
        unitAttrCenter.FullMovePoint();
        isIdle = false;
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
        // 恢复idle动画
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
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

    // public void ShowActionList()
    // {
    //     if (!isPlayerPiece) return;
    //     //_actionListPanel.gameObject.SetActive(true);
    //     BattleScene.Ins.UM.pieceActionListPanel.ShowPanel(this);
    //     BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(this);
    //     //BattleScene.Ins.UM.infoBox.ShowInfo(this);
    // }

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

        // if (_curLadderArea != null)
        // {
        //     _curLadderArea.LeaveSlot(this);
        //     _curLadderArea = null;
        // }
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

    public void StartMove()
    {
        if (!isPlayerPiece) return;
        PlayAudio(ActionType.移动);
        //pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
    }

    public void StopMove()
    {
        CheckActionPos();
        CheckArea();
        _actionListPanel.gameObject.SetActive(true);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
    }

    public void OnSelect()
    {
        rangeUI?.ShowSelect(true);
    }

    public void CancelSelect()
    {
        Debug.Log("取消选择棋子");
        _isAttacking = false;
        rangeUI?.CloseRange();
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
            if (caverSlot != null && !caverSlot.isFull) // 掩体判定规则
            {
                // 取消吸附机制
                //caverSlot.AddToSlot(transform);
                _curCaverSlot = caverSlot;
                // 播放掩体特效
                SpriteEffectPlayer shieldEffect
                    = ObjectPool.Ins.GenerateObject(ItemType.SHIELD, transform.position
                            , Quaternion.identity)
                        .GetComponent<SpriteEffectPlayer>();
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

            HealArea healArea = collider.transform.GetComponent<HealArea>();
            if (healArea != null)
            {
                healArea.AddBuffToUnit(this);
            }
        }

        return result;
    }

    public void CheckArea()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f);
        foreach (var collider in hitColliders)
        {
        }
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
        if(!_isAttacking) return;
        // 根据范围获取所有棋子
        List<PieceController> targets = rangeUI.GetCurTargets;
        if (targets.Count < 1)
        {
            Debug.Log("未选中任何目标，无法发动技能");
            return;
        }

        Debug.Log("棋子攻击");
        CheckFace(targets[0].transform.position - transform.position);
        if (_curAtkType == ActionType.近战攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Attack, false, 1f);
            PlayAudio(ActionType.近战攻击);
            PassiveTrigger(PassiveTriggerType.OnMeleeAttack);
        }
        else if (_curAtkType == ActionType.远程攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
            PlayAudio(ActionType.远程攻击);
            PassiveTrigger(PassiveTriggerType.OnRangedAttack);
        }


        // // 聚能充能
        // if (isPlayerPiece)
        // {
        //     if (!BattleScene.Ins.BM.PlayerController.isBursting)
        //     {
        //         // 攻击充能
        //         BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.attackBurstCharge);
        //     }
        // }


        // 结束攻击状态
        _isAttacking = false;
        if (targets.Count > 0)
        {
            ShootBolt(targets[0].transform.position, _curAttackPack.bulletVFXType);
        }
        Transform atkPos = rangeUI.GetSkillTransform();
        if (atkPos != null && _curAttackPack.skillVFXType != 0)
        {
                    
            GameObject fx  = ObjectPool.Ins.GenerateObject(
                _curAttackPack.skillVFXType,
                atkPos.position + Vector3.up * 0.1f,
                atkPos.localRotation);
            if (_curAttackPack.isRotate)
            {
                // fx沿 z轴 旋转，方向为从transfrom指向atkPos
                Vector3 dir = (atkPos.position - transform.position).normalized;
                dir.y = 0;
                float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
                fx.transform.rotation = Quaternion.Euler(45,  -45, angle + 90f);
                Debug.Log($"生成技能特效{_curAttackPack.skillVFXType},{dir}旋转角度{angle}");
            }
        }
        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f
            , () =>
            {
                if (!unitAttrCenter.CostMP()) return;
                BattleScene.Ins.BM.PieceSkill(this, targets, _curAttackPack,
                    atkPos !=null ? atkPos.position : Vector3.zero
                    , _curAtkType);
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
        BattleScene.Ins.TM.RequestHitStop();
        OnHurt?.Invoke();
        // 聚能充能
        if (isPlayerPiece)
        {
            // 受伤充能
            BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.hurtBurstCharge);
        }

        Debug.Log($"{this.name} 受伤");
        // 判定地方聚能状态
        if (!isPlayerPiece && BattleScene.Ins.BM.PlayerController.isBursting)
        {
            // 如果是敌人棋子且玩家处于聚能状态，受伤动画持续到回合结束
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Hit, false, -1);
        }
        else
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Hit, false, 0.5f);
        }

        // TODO: 根据受伤的数值改变振动的强度
        pieceDisplay.pieceSpriteRenderer.transform.DOShakePosition(0.5f, 0.8f);
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
        //if (uiCanvas != null) uiCanvas.SetActive(true);
        ShowHighlight(false);
    }

    public virtual void Dead()
    {
        Debug.Log($"{this.name} 死亡");
        OnDead?.Invoke();
        //if (uiCanvas != null) uiCanvas.SetActive(false);
        if(deadNotDelete) return;
        if (pieceDisplay == null)
        {
            gameObject.SetActive(false);
            return;
        }

        pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, false, -1, () =>
        {
            BattleScene.Ins.BM.PlayerCheckWin();
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
        BattleScene.Ins.BM.tipTextManager.ShowReloadAmmo(this.transform);
    }

    public void StartSkillAttack(SkillPack skillPack)
    {
        if (!unitAttrCenter.HasMana(skillPack.mpCost))
        {
            Debug.Log("能量不足");
            return;
        }

        _isUsingSkill = true;
        rangeUI?.ShowSkillRange(skillPack);
        _skillPack = skillPack;
    }

    public bool SkillAvailable(SkillPack skillPack)
    {
        if (!unitAttrCenter.HasMana(skillPack.mpCost)) return false;
        if (!unitAttrCenter.HasItem(skillPack.consumeItems)) return false;
        return true;
    }

    /// <summary>
    /// 发动技能
    /// </summary>
    public virtual void CastSkill()
    {
        if (_skillPack == null) return;
        if(!_isUsingSkill) return;
        Sequence sequence = DOTween.Sequence();
        Transform atkPos = rangeUI.GetSkillTransform();
        if (_skillPack.isDelaySkill) // 延时类技能跳过结算
        {
            BattleScene.Ins.BM.RestoreDelaySkill(this, _skillPack, atkPos.position);
            _isUsingSkill = false;
            rangeUI.CloseRange();
            Debug.Log("延迟类技能");
            return;
        }

        // 根据范围获取所有棋子
        List<PieceController> targets = rangeUI.GetCurTargets;
        if (targets.Count < 1 && (_skillPack.target != SkillTarget.Area &&
                                  _skillPack.target != SkillTarget.Self))
        {
            Debug.Log("未选中任何目标，无法发动技能");
            return;
        }

        _isUsingSkill = false;
        
        CheckResult checkResult = CheckResult.None;
        if (_skillPack.isRecognitionCheck && targets.Count > 0)
        {
            checkResult =
                BattleScene.Ins.BM.diceCheckManager.ModeRecognitionCheck(this, targets[0]);
            sequence.AppendInterval(2.5f);
        }

        Vector3 atkPosValue = atkPos != null ? atkPos.position : Vector3.zero;
        sequence.AppendCallback(() =>
        {
            if (atkPos != null)
            {
                CheckFace(atkPosValue - transform.position);
            }
            else if (targets.Count > 0)
            {
                CheckFace(targets[0].transform.position - transform.position);
            }

            Debug.Log($"{this.name}发动技能攻击{_skillPack.skillName}，targets数量：{targets.Count}");


            // 播放技能动画
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Skill, false, 1f,
                null, _skillPack.animationIndex);


            PlayAudio(_skillPack);
            
            // 生成特效
            //Transform atkPos = rangeUI.GetSkillTransform();
            if (atkPos != null && _skillPack.skillVFXType != 0)
            {
                GameObject fx  = ObjectPool.Ins.GenerateObject(
                    _skillPack.skillVFXType,
                    atkPos.position + Vector3.up * 0.1f,
                    atkPos.localRotation);
                if (_skillPack.isRotate)
                {
                    // fx沿 z轴 旋转，方向为从transfrom指向atkPos
                    Vector3 dir = (atkPos.position - transform.position).normalized;
                    dir.y = 0;
                    float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
                    fx.transform.rotation = Quaternion.Euler(45,  -45, angle + 90f);
                    Debug.Log($"生成技能特效{_skillPack.skillVFXType},{dir}旋转角度{angle}");
                }

            }
            else if (_skillPack.skillVFXType != 0)
            {
                Vector3 pos = targets.Count > 0
                    ? targets[0].transform.position
                    : transform.position;
                ObjectPool.Ins.GenerateObject(
                    _skillPack.skillVFXType,
                    pos + Vector3.up * 0.1f,
                    Quaternion.identity);
            }
        });
        sequence.AppendInterval(0.3f);
        sequence.AppendCallback(() =>
        {
            if (targets.Count > 0)
            {
                ShootBolt(targets[0].transform.position, _skillPack.bulletVFXType);
            }
        });
        // 延迟0.3f
        sequence.AppendCallback(
            () =>
            {
                if (!unitAttrCenter.CostMP()) return;
                if (!unitAttrCenter.CostMana(_skillPack.mpCost))
                {
                    Debug.LogError("能量值不足");
                    return;
                }

                if (!unitAttrCenter.CostItem(_skillPack.consumeItems)) return;
                PassiveTrigger(PassiveTriggerType.OnSkillUse, _skillPack);
                Vector3 skillPos = atkPos != null ? atkPos.position : transform.position;
                BattleScene.Ins.BM.PieceSkill(this, targets, _skillPack, skillPos, ActionType.技能
                    , checkResult);

                //Debug.Log("关闭显示范围");
                rangeUI.CloseRange();
                
            });

        // 技能聚能充能
    }


    // 更新朝向
    public void CheckFace(Vector3 direction)
    {
        // 如果targetPos在当前棋子左侧，则朝向左侧，否则朝向右侧，更新piece display
        // 由于棋子式斜45站立的，所以应该同时计算x轴和z轴
        if (direction.x < -direction.z)
        {
            pieceDisplay.FaceRight(!isPlayerPiece);
        }
        else //if (direction.x > 0)
        {
            pieceDisplay.FaceRight(isPlayerPiece);
        }
    }

    public float GetRange(bool isNormalAtk)
    {
        return isNormalAtk ? _pieceData.meleeAtk.rangeValue : _pieceData.rangedAtk.rangeValue;
    }

    public virtual void ShowHighlight(bool option)
    {
        rangeUI?.ShowHighlight(option);
        if (hightlightEffect != null) hightlightEffect.SetActive(option);
        //if(!option) hitInfoPanel?.gameObject.SetActive(false);
    }

    //[Button("测试发射火箭")]
    public void TestShoot(Transform targetTransform)
    {
        Vector3 targetPos = targetTransform.position;
        ShootBolt(targetPos, ItemType.ROCKET);
    }
    
    protected void ShootBolt(Vector3 tagetPos, ItemType itemType)
    {
        if (itemType == ItemType.NONE)
        {
            Debug.Log("没有子弹特效");
            return;
        }
        Debug.Log("生成子弹");
        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPosFixed = new Vector3(tagetPos.x, startPos.y, tagetPos.z);
        Transform bolt = ObjectPool.Ins
            .GenerateObject(itemType, startPos, Quaternion.identity)
            .transform;
        //bolt.LookAt(tagetPos);
        // 计算方向并设置bolt的rotation，使其x轴指向目标点
        Vector3 direction = (targetPosFixed - startPos).normalized;
        if (direction != Vector3.zero)
        {
            // 让bolt的forward（z轴）指向目标点，然后旋转90度使x轴指向目标
            bolt.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        }
        bolt.DOMove(targetPosFixed, 0.5f).SetEase(Ease.Flash)
            .OnComplete(() => { ObjectPool.Ins.HideObject(bolt.gameObject); });
    }

    // ======= 道具 ====== //
    public bool ItemAvailable(ItemData itemData)
    {
        if (itemData == null) return false;
        if (!unitAttrCenter.HasMP()) return false;
        switch (itemData.useType)
        {
            case UseType.InBattle:
                return true;
            case UseType.WhenEnergyNotFull:
                if (unitAttrCenter.ManaPoint >= unitAttrCenter.MaxManaPoint)
                    return false;
                break;
            case UseType.OutOfBattle:
                return false;
                break;
            case UseType.WhenHpNotFull:
                if (unitAttrCenter.CurHealth >= unitAttrCenter.MaxHealth)
                    return false;
                break;
            case UseType.WhenStaminaNotFull:
                if (unitAttrCenter.CurMovePoint >= unitAttrCenter.MaxMovePoint)
                    return false;
                break;
            default:
                return true;
        }

        return true;
    }

    public void UseItem(ItemData itemData)
    {
        var items = new List<ItemPack>() { new ItemPack(itemData.itemName, 1) };
        if (!unitAttrCenter.HasItem(items)) return;
        if (!unitAttrCenter.CostMP()) return;
        switch (itemData.itemName)
        {
            case ItemName.通用作战平台_CW179:
                break;
            case ItemName.魔女兵器:
                break;
            case ItemName.UX210_枪骑兵:
                break;
            case ItemName.能量包: // 回复能量
                unitAttrCenter.AddMana(100);
                break;
            case ItemName.医疗单元I型:
                unitAttrCenter.Heal(100);
                break;
            case ItemName.专速达:
                unitAttrCenter.AddMP(3);
                break;
            case ItemName.礼盒:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        unitAttrCenter.CostItem(items);
    }

    // ======= 插件 ====== //

    /// <summary>
    /// 初始化插件系统
    /// </summary>
    private void InitComp()
    {
        if (playerData == null) return;
        foreach (var i in playerData.normalSlots)
        {
            var data = GM.Ins.DM.componentConfig.GetData(i);
            if (data == null) continue;
            availablePassives.Add(data.passiveType);
        }

        foreach (var i in playerData.weaponSlots)
        {
            // 添加武器效果
        }
    }

    private void PassiveTrigger(PassiveTriggerType passiveTriggerType, SkillPack skillPack = null)
    {
        switch (passiveTriggerType)
        {
            case PassiveTriggerType.OnMeleeAttack:
                if (availablePassives.Contains(PassiveType.Lash))
                {
                    unitAttrCenter.AddMana(3);
                }

                break;
            case PassiveTriggerType.OnRangedAttack:
                break;
            case PassiveTriggerType.OnDamaged:
                break;
            case PassiveTriggerType.OnSkillUse:
                if (availablePassives.Contains(PassiveType.LongTermInterests))
                {
                    // 35%概率不消耗能量
                    int randomValue = UnityEngine.Random.Range(0, 100);
                    if (randomValue < 35)
                    {
                        unitAttrCenter.AddMana(skillPack.mpCost);
                        Debug.Log("长远利益被动触发，技能能量返还");
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(passiveTriggerType), passiveTriggerType
                    , null);
        }
    }

    /// <summary>
    /// 被选中时
    /// </summary>
    public virtual void OnBeTarget(PieceController attacker, SkillPack skillPack)
    {
    }

    public virtual void OnCloseHitInfo()
    {
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


    // ===== 描边效果 ======//

    public GameObject outlineEffect;

    // 鼠标进入时显示
    /*private void OnMouseEnter()
    {
        if (outlineEffect != null) outlineEffect.SetActive(true);
    }
    // 鼠标离开时隐藏
    private void OnMouseExit()
    {
        if (outlineEffect != null) outlineEffect.SetActive(false);
    }*/
    public virtual void ShowOutline(bool option)
    {
        if (outlineEffect != null) outlineEffect.SetActive(option);
        //ShowHighlight(option);
    }
}