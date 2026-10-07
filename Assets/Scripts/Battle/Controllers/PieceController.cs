using System;
using System.Collections;
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
    public bool IsUsingSkill
    {
        get { return _isUsingSkill || _isAttacking || IsPerformingAction; }
    }

    public bool isUsingOrder;
    public bool isGrauding;// 正在警戒

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

    public bool ableStrick;// 能否触发夹击
    
    [FoldoutGroup("事件")] public UnityEvent OnInit;
    [FoldoutGroup("事件")] public UnityEvent OnTurnStart;
    [FoldoutGroup("事件")] public UnityEvent OnTurnEnd;
    [FoldoutGroup("事件")] public UnityEvent OnHurt;
    [FoldoutGroup("事件")] public UnityEvent OnDead;


    private SkillSystem.CharacterSkillManager passiveManager;

    public bool IsPerformingAction { get; private set; }
    private Coroutine combatRoutine;
    private int combatVersion;
    private PieceDisplay.Playback actionPlayback;
    private Tween projectileTween;
    private Tween hitShake;
    private Vector3 hitShakeOrigin;

    protected void CancelCombatAction()
    {
        combatVersion++;
        if (actionPlayback != null && !actionPlayback.IsDone) pieceDisplay?.StopAnimation();
        actionPlayback = null;
        if (combatRoutine != null) StopCoroutine(combatRoutine);
        combatRoutine = null;
        projectileTween?.Kill();
        projectileTween = null;
        IsPerformingAction = false;
    }

    protected virtual void OnDisable()
    {
        CancelCombatAction();
        StopHitShake();
        _isAttacking = _isUsingSkill = false;
        pieceDisplay?.StopAnimation();
    }

    protected void StopHitShake()
    {
        if (hitShake == null) return;
        hitShake.Kill();
        hitShake = null;
        if (pieceDisplay != null && pieceDisplay.pieceSpriteRenderer != null)
            pieceDisplay.pieceSpriteRenderer.transform.localPosition = hitShakeOrigin;
    }

    private void OnDestroy() => passiveManager?.UnregisterPiece(gameObject);

    public void Init(PlayerController player, PieceData pieceData = null)
    {
        CancelCombatAction();
        StopHitShake();
        pieceDisplay?.StopAnimation();
        passiveManager = BattleScene.Ins?.BM?.characterSkillManager;
        passiveManager?.UnregisterPiece(gameObject);
        this.player = player;
        this.playerData = isPlayerPiece ? GM.Ins.PLAYERPROFILE.GetPlayer(pieceID - 1) : null;
        //unitAttrCenter.Init();
        /*if (isPlayerPiece)
        {
            availableActions.Add(ActionType.移动);
            availableActions.Add(ActionType.近战攻击);
            availableActions.Add(ActionType.远程攻击);
            availableActions.Add(ActionType.待机); // 待机
            //availableActions.Add(ActionType.重新装填); // 装填
            availableActions.Add(ActionType.技能); // 技能
            availableActions.Add(ActionType.道具);
            availableActions.Add(ActionType.警戒指令);
        }*/

        //Debug.Log(_pieceDisplay.name);
        if(gameObject.activeInHierarchy) pieceDisplay?.ChangeDisplayState(PieceDisplayState.Idle);
        if (pieceData != null)
        {
            _pieceData = pieceData;
            availableSkills = pieceData.skillPacks != null ? new List<SkillPack>(pieceData.skillPacks) : new();
            // 敌人按场景等级加载成长，玩家保持原属性初始化流程。
            if (this is EnemyController enemy)
                unitAttrCenter.SetEnemyData(_pieceData, enemy.Level);
            else
                unitAttrCenter.SetData(_pieceData, playerData);
            if (isPlayerPiece)//&& GM.Ins.pieceHPInherit
            {
                Player playerData = GM.Ins.PLAYERPROFILE.GetPlayer(pieceID - 1);
                if (playerData.curHealth < 0)
                /*{
                    // 只有当玩家当前血量大于0时才继承血量，否则按照默认值初始化，避免玩家死亡后再次进入战斗时棋子带着异常血量
                    unitAttrCenter.SetValues(playerData.curHealth, playerData.curMana
                        , playerData.curAmmo);
                }
                else if (playerData.curHealth == 0)
                {
                    // 默认初始化
                }
                else*/
                {
                    playerData.deadCount++;
                    // 触发角色死亡惩罚初始化：上一场战斗中死亡，这一场战斗中变成负伤状态
                    unitAttrCenter.SetHurtState();
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
        if (passiveManager != null && passiveManager.IsInitialized && _pieceData != null)
            passiveManager.RegisterPiece(gameObject, _pieceData.passiveSkillTypes);
    }

    private void OnEnable()
    {
        pieceDisplay?.ChangeDisplayState(PieceDisplayState.Idle);
    }

    private void Update()
    {
        if (BattleScene.Ins != null && BattleScene.Ins.BM.HasCombatPresentation) return;
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

        if (isUsingOrder)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CastOrder();
            }

            // 点击右键取消
            if (Input.GetMouseButtonDown(1))
            {
                isUsingOrder = false;
                rangeUI.CloseRange();
            }            
        }
    }

    public virtual void TurnStart()
    {
        //Debug.Log($"TurnStart: {gameObject.name}");
        OnTurnStart?.Invoke();
        if (isDead) return;
        unitAttrCenter.FullMovePoint();
        isIdle = false;
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
        // 恢复idle动画
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        ableStrick = true;// 重置夹击状态
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
        if (!isDead && gameObject.activeInHierarchy)
        {
            BattleScene.Ins.BM.characterSkillManager.NotifyTurnEnd(gameObject);
            BattleScene.Ins.BM.buffManager.ProcessBuffDurations(unitAttrCenter);
        }
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
        //pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
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
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
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
        rangeUI.ShowSelect(false);
        if(!isGrauding) rangeUI?.CloseRange();
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
        if (IsPerformingAction) return;
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

    /// <summary>
    /// 夹击和警戒专用，直接发动普通攻击
    /// </summary>
    /// <param name="target"></param>
    /// <param name="range"></param>
    public void CastNormalAttack(PieceController target, bool range = false, bool isOrder = false)
    {
        if (IsPerformingAction || isDead || !BuffManager.CanTarget(this, target)) return;
        var skill = range ? _pieceData.rangedAtk : _pieceData.meleeAtk;
        var action = range ? ActionType.远程攻击 : ActionType.近战攻击;
        if (skill == null || (range && unitAttrCenter.AmmoCount <= 0)) return;
        if (range) unitAttrCenter.CostAmmo();
        _isAttacking = false;
        BeginCombatAction(skill, new List<PieceController> { target }, target.transform.position,
            action, range ? PieceDisplayState.Shoot : PieceDisplayState.Attack, !isOrder);
    }

    private void CastAttack()
    {
        if (!_isAttacking || IsPerformingAction || isDead) return;
        var skill = _curAttackPack;
        var action = _curAtkType;
        var targets = new List<PieceController>(rangeUI.GetCurTargets);
        if (skill == null || (targets.Count == 0 && skill.target != SkillTarget.Area)) return;
        bool ranged = action == ActionType.远程攻击;
        if (ranged && unitAttrCenter.AmmoCount <= 0) return;
        Vector3 position = rangeUI.GetSkillTransform() != null ? rangeUI.GetSkillTransform().position
            : targets.Count > 0 ? targets[0].transform.position : transform.position;
        // 在动作开始时一次性支付，避免动画播完后才发现行动力不足。
        if (!unitAttrCenter.CostMP(action)) return;
        if (ranged) unitAttrCenter.CostAmmo();
        _isAttacking = false;
        rangeUI.CloseRange();
        BeginCombatAction(skill, targets, position, action,
            ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack);
    }

    /// <summary>
    /// 统一玩家、AI、夹击：动画出手帧 -> 弹道到达 -> 伤害/受击 -> 动画结束。
    /// 参数与目标列表在开始时快照，不能被下一次选技能或关闭范围 UI 改写。
    /// </summary>
    protected void BeginCombatAction(SkillPack skill, List<PieceController> targets, Vector3 position,
        ActionType action, PieceDisplayState displayState, bool isFlank = false,
        CheckResult check = CheckResult.None, bool waitForRecognition = false)
    {
        if (IsPerformingAction || isDead || !isActiveAndEnabled) return;
        IsPerformingAction = true;
        combatVersion++;
        BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
        combatRoutine = StartCoroutine(CombatActionRoutine(skill, new List<PieceController>(targets),
            position, action, displayState, isFlank, check, waitForRecognition));
    }

    private IEnumerator CombatActionRoutine(SkillPack skill, List<PieceController> targets, Vector3 position,
        ActionType action, PieceDisplayState displayState, bool isFlank, CheckResult check, bool waitForRecognition)
    {
        int version = combatVersion;
        try
        {
            // 让协程句柄先归属本动作；等待检定真正显示结果，不猜测面板时长。
            yield return null;
            var dicePanel = BattleScene.Ins.BM.diceCheckManager.checkDicePanel;
            while (waitForRecognition && dicePanel != null && dicePanel.IsRolling) yield return null;
            if (isDead || !isActiveAndEnabled) yield break;
            CheckFace(position - transform.position);
            if (displayState == PieceDisplayState.Skill || !isPlayerPiece) PlayAudio(skill);
            else PlayAudio(action);
            bool impactDone = false;
            bool launched = false;
            Action impact = () =>
            {
                if (impactDone || version != combatVersion) return;
                impactDone = true;
                if (this == null || isDead || !isActiveAndEnabled || BattleScene.Ins == null) return;
                ShowSkillImpact(skill, position);
                BattleScene.Ins.BM.PieceSkill(this, targets, skill, position, action, check, isFlank);
                if (this is EnemyController enemy)
                    enemy.enemyCanvas?.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
            };
            UnityAction release = () =>
            {
                if (launched || version != combatVersion || isDead || !isActiveAndEnabled) return;
                launched = true;
                projectileTween = ShootBolt(position, skill.bulletVFXType, impact);
            };
            PieceDisplay.Playback animation = null;
            if (pieceDisplay != null && pieceDisplay.isActiveAndEnabled)
                animation = pieceDisplay.PlayAction(displayState, release, skill.animationIndex);
            else release();
            actionPlayback = animation;

            while (!impactDone || (animation != null && !animation.IsDone))
            {
                if (isDead || (animation != null && animation.IsCancelled)) yield break;
                // 外部回收/销毁弹道只取消本次动作，不补发已取消的伤害。
                if (launched && !impactDone && (projectileTween == null || !projectileTween.IsActive())) yield break;
                yield return null;
            }
            // 不靠固定秒数猜测受击何时结束；聚能保持最后一帧不算仍在播放。
            while (HasPendingReaction(targets) || (pieceDisplay != null && pieceDisplay.IsPlayingOneShot))
            {
                if (isDead) yield break;
                yield return null;
            }
        }
        finally
        {
            if (actionPlayback != null && !actionPlayback.IsDone) pieceDisplay?.StopAnimation();
            actionPlayback = null;
            projectileTween?.Kill();
            projectileTween = null;
            IsPerformingAction = false;
            combatRoutine = null;
        }
    }

    private static bool HasPendingReaction(List<PieceController> targets)
    {
        foreach (var target in targets)
            if (target != null && target.gameObject.activeInHierarchy && target.pieceDisplay != null &&
                target.pieceDisplay.IsPlayingOneShot) return true;
        return false;
    }

    private void ShowSkillImpact(SkillPack skill, Vector3 position)
    {
        if (skill.skillVFXType == ItemType.NONE) return;
        var fx = ObjectPool.Ins.GenerateObject(skill.skillVFXType,
            position + Vector3.up * 0.1f, Quaternion.identity);
        if (fx == null || !skill.isRotate) return;
        Vector3 dir = (position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        fx.transform.rotation = Quaternion.Euler(45, -45, angle + 90f);
    }

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
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Hit, true);
        }

        // TODO: 根据受伤的数值改变振动的强度
        StopHitShake();
        hitShakeOrigin = pieceDisplay.pieceSpriteRenderer.transform.localPosition;
        hitShake = pieceDisplay.pieceSpriteRenderer.transform.DOShakePosition(0.5f, 0.8f)
            .OnComplete(StopHitShake);
        BattleScene.Ins.UM.pieceInfoPanel.UpdateDisplay();
        //if (uiCanvas != null) uiCanvas.SetActive(true);
        ShowHighlight(false);
    }

    public virtual void Dead()
    {
        CancelCombatAction();
        StopHitShake();
        BattleScene.Ins.TM.RequestHitStop();
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
        if (IsPerformingAction) return;
        if (!unitAttrCenter.HasMana(unitAttrCenter.GetSkillManaCost(skillPack)))
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
        if (!unitAttrCenter.HasMana(unitAttrCenter.GetSkillManaCost(skillPack))) return false;
        if (!unitAttrCenter.HasItem(skillPack.consumeItems)) return false;
        return true;
    }

    /// <summary>
    /// 发动技能
    /// </summary>
    public virtual void CastSkill()
    {
        var skill = _skillPack;
        if (skill == null || !_isUsingSkill || IsPerformingAction || isDead) return;
        Transform atkPos = rangeUI.GetSkillTransform();
        if (skill.isDelaySkill)
        {
            if (atkPos == null || !unitAttrCenter.TryCostSkill(skill)) return;
            NotifySkillUsed(skill);
            BattleScene.Ins.BM.RestoreDelaySkill(this, skill, atkPos.position);
            _isUsingSkill = false;
            rangeUI.CloseRange();
            return;
        }
        var targets = new List<PieceController>(rangeUI.GetCurTargets);
        if (targets.Count == 0 && skill.target != SkillTarget.Area && skill.target != SkillTarget.Self) return;
        Vector3 position = atkPos != null ? atkPos.position
            : targets.Count > 0 ? targets[0].transform.position : transform.position;
        if (!unitAttrCenter.TryCostSkill(skill)) return;
        _isUsingSkill = false;
        rangeUI.CloseRange();
        CheckResult check = CheckResult.None;
        bool recognition = skill.isRecognitionCheck && targets.Count > 0;
        if (recognition) check = BattleScene.Ins.BM.diceCheckManager.ModeRecognitionCheck(this, targets[0]);
        BeginCombatAction(skill, targets, position, ActionType.技能, PieceDisplayState.Skill,
            check: check, waitForRecognition: recognition);
        NotifySkillUsed(skill);
    }


    // 更新朝向
    protected void NotifySkillUsed(SkillPack skill)
    {
        ComponentEquipment.OnSkillUsed(this, skill);
        BattleScene.Ins.BM.characterSkillManager.NotifyCastActiveSkill(gameObject);
    }
    public void CheckFace(Vector3 direction)
    {
        if (pieceDisplay == null || pieceDisplay.pieceSpriteRenderer == null) return;
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
    
    protected Tween ShootBolt(Vector3 tagetPos, ItemType itemType, Action arrived = null)
    {
        if (itemType == ItemType.NONE)
        {
            arrived?.Invoke();
            return null;
        }
        Debug.Log("生成子弹");
        Vector3 startPos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPosFixed = new Vector3(tagetPos.x, startPos.y, tagetPos.z);
        var projectile = ObjectPool.Ins.GenerateObject(itemType, startPos, Quaternion.identity);
        if (projectile == null)
        {
            arrived?.Invoke();
            return null;
        }
        // 本次飞行负责回收，取消预制体通用寿命，防止减速期间提前回收。
        projectile.GetComponent<LifeTime>()?.CancelExpiry();
        Transform bolt = projectile.transform;
        //bolt.LookAt(tagetPos);
        // 计算方向并设置bolt的rotation，使其x轴指向目标点
        Vector3 direction = (targetPosFixed - startPos).normalized;
        if (direction != Vector3.zero)
        {
            // 让bolt的forward（z轴）指向目标点，然后旋转90度使x轴指向目标
            bolt.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        }
        bool recycled = false;
        Action recycle = () =>
        {
            if (recycled) return;
            recycled = true;
            if (projectile != null) ObjectPool.Ins.HideObject(projectile);
        };
        return bolt.DOMove(targetPosFixed, 0.5f).SetEase(Ease.Linear)
            .SetUpdate(UpdateType.Normal, false)
            .SetLink(projectile, LinkBehaviour.KillOnDisable)
            .OnComplete(() => { arrived?.Invoke(); recycle(); })
            .OnKill(() => recycle());
    }

    // ======= 道具 ====== //
    public bool ItemAvailable(ItemData itemData)
    {
        if (itemData == null) return false;
        if (!unitAttrCenter.HasMP(ActionType.道具)) return false;
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
        if (!unitAttrCenter.HasMP(ActionType.道具)) return;
        //BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
        unitAttrCenter.CostItem(items);
        if (itemData.useType == UseType.ActiveInBattle)
        {
            // 主动释放技能直接进入技能释放流程
            SkillPack skillPack = GM.Ins.DM.skillPackListSO.GetSkillPack(itemData.skillPack);
            if (skillPack != null)
            {
                StartItemAttack(skillPack);
                return;
            }
        }
        switch (itemData.itemName)
        {
            case ItemName.通用作战平台_CW179:
                break;
            case ItemName.魔女兵器:
                break;
            case ItemName.UX210_枪骑兵:
                break;
            case ItemName.能量包: // 回复能量
                unitAttrCenter.AddMana(3);
                break;
            case ItemName.医疗单元I型:
                unitAttrCenter.Heal(100);
                break;
            case ItemName.专速达:
                unitAttrCenter.AddMP(3);
                break;
            case ItemName.礼盒:
                break;
            case ItemName.修复集群:
                unitAttrCenter.FullHealth();// 回复满血
                break;
            case ItemName.能量包S:
                unitAttrCenter.AddMana(9);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
        
        if (!unitAttrCenter.CostMP(ActionType.道具)) return;
        //BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 开始使用道具进行攻击
    /// </summary>
    /// <param name="skillPack"></param>
    private void StartItemAttack(SkillPack skillPack)
    {
        _isAttacking = true;
        _curAttackPack = skillPack;
        _curAtkType = ActionType.使用道具;
        //_isUsingSkill = true;
        //_skillPack = skillPack;
        rangeUI?.ShowSkillRange(_curAttackPack);
    }

    // ======= 插件 ====== //

    /// <summary>
    /// 初始化插件系统
    /// </summary>
    private void InitComp()
    {
        componentBonuses.Clear();
        componentPlayerAttributes = ReadPlayerAttributes();
        RefreshComponents();
    }

    private readonly Dictionary<BuffAttrType, float> componentBonuses = new();
    private readonly HashSet<PassiveType> componentPassives = new();
    private int[] componentPlayerAttributes;

    private int[] ReadPlayerAttributes()
    {
        var result = new int[5];
        if (playerData != null)
            for (int i = 0; i < result.Length; i++) result[i] = playerData.AccessAttribute(i, AttrOp.Get);
        return result;
    }

    public void RefreshComponents()
    {
        foreach (var bonus in componentBonuses) unitAttrCenter.AddBuffAttr(bonus.Key, -bonus.Value);
        componentBonuses.Clear();
        foreach (var passive in componentPassives) availablePassives.Remove(passive);
        componentPassives.Clear();
        availableSkills = _pieceData?.skillPacks != null ? new List<SkillPack>(_pieceData.skillPacks) : new();
        var attributes = ReadPlayerAttributes();
        if (componentPlayerAttributes != null && playerData != null)
            unitAttrCenter.RefreshPlayerAttributes(componentPlayerAttributes, attributes);
        componentPlayerAttributes = attributes;
        foreach (var data in ComponentEquipment.Equipped(playerData))
        {
            if (data.effectType == ComponentEffect.PassiveEffect && data.passiveType != PassiveType.None &&
                !availablePassives.Contains(data.passiveType))
            {
                availablePassives.Add(data.passiveType);
                componentPassives.Add(data.passiveType);
            }
            if (data.effectType == ComponentEffect.ActiveSkill && data.skillPack != null &&
                !string.IsNullOrWhiteSpace(data.skillPack.skillName) && !availableSkills.Contains(data.skillPack))
                availableSkills.Add(data.skillPack);
            if (data.battleBonuses == null) continue;
            foreach (var bonus in data.battleBonuses)
            {
                if (bonus == null || bonus.attribute == BuffAttrType.None) continue;
                componentBonuses.TryGetValue(bonus.attribute, out float old);
                componentBonuses[bonus.attribute] = old + bonus.value;
                unitAttrCenter.AddBuffAttr(bonus.attribute, bonus.value);
            }
        }
        _isUsingSkill = false;
        if (BattleScene.Ins?.UM != null) BattleScene.Ins.UM.OnPieceStateChance(this);
    }

    /// <summary>
    /// 被选中时
    /// </summary>
    public virtual void OnBeTarget(PieceController attacker, SkillPack skillPack)
    {
    }

    private OrderProfile _orderProfile;
    public void StartOrderGraud(OrderProfile orderProfile)
    {
        _orderProfile = orderProfile;
        rangeUI.ShowOrderRange(orderProfile);
        isUsingOrder = true;
    }
    public void CastOrder()
    {
        if (!BattleScene.Ins.BM.buffManager.OnAction(unitAttrCenter))
        {
            isUsingOrder = false;
            rangeUI.CloseRange();
            return;
        }
        Debug.Log($"确认指令:{_orderProfile.orderName}");
        BattleScene.Ins.BM.orderManager.ConfirmOrder(this, _orderProfile,rangeUI.fanRoot.transform.localRotation);
        isUsingOrder = false;
        isGrauding = true;
        _orderProfile = null;
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
            GM.Ins.AM.PlayClip(clip);
        }
    }

    public void PlayAudio(SkillPack skillPack)
    {
        if (skillPack.skillSound != null)
        {
            GM.Ins.AM.PlayClip(skillPack.skillSound);
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
