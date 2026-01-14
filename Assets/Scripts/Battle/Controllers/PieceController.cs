using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 棋子控制器
/// </summary>
public class PieceController : MonoBehaviour
{
    [Header("引用")]
    //public string pieceName;
    public UnitAttrCenter unitAttrCenter; // 单位属性中心

    [SerializeField] private RangeUI _rangeUI; // 范围UI

    [SerializeField] private PieceActionListPanel _actionListPanel; // 棋子动作列表面板

    [SerializeField] 
    public PieceDisplay pieceDisplay;
    
    [Header("配置")]
    public int pieceID; // 棋子ID


    [Header("状态")] public bool isPlayerPiece; // 是否是玩家棋子

    public bool isDead => unitAttrCenter.CurHealth <= 0; // 是否死亡

    //private bool _isDragging = false; // 是否正在拖拽

    //private Vector3 _originalPosition; // 原始位置

    private CaverSlot _curCaverSlot; // 当前绑定的点位

    // 当前攻击数据
    private bool _isAttacking = false; // 是否正在攻击
    [SerializeField] [ReadOnly]  private AttackPack _attackPack;
    //[SerializeField] [ReadOnly] private int _damage;
    //[SerializeField] [ReadOnly] private DamageType _damageType;

    public List<InteractArea> interactAreas = new(); // 可交互区域列表

    public List<ActionType> availableActions = new(); // 可用动作列表

    //public bool isActived = false; // 是否被激活

    public bool isIdle;// 是否处于待机状态
    

    public void Init()
    {
        unitAttrCenter.Init();
        availableActions.Add(ActionType.Move);
        availableActions.Add(ActionType.Attack);
        availableActions.Add(ActionType.Range_ATK);
        availableActions.Add(ActionType.Idle);// 待机
        availableActions.Add(ActionType.Reload);// 装填
        //Debug.Log(_pieceDisplay.name);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        if(_actionListPanel!=null)_actionListPanel.Init(this);
        isIdle = true;
    }

    private void Update()
    {
        if (_isAttacking && isPlayerPiece)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CheckEnemy();
            }

            // 点击右键取消
            if (Input.GetMouseButtonDown(1))
            {
                _isAttacking = false;
                _rangeUI.CloseRange();
            }
        }
    }

    public void TurnStart()
    {
        unitAttrCenter.FullMovePoint();
        isIdle = false;
    }

    public void TurnEnd()
    {
        if (_actionListPanel != null)
        {
            _actionListPanel.gameObject.SetActive(false);
        }
    }

    public void ShowActionList()
    {
        if (!isPlayerPiece) return;
        _actionListPanel.gameObject.SetActive(true);
        BattleScene.Ins.UM.infoBox.ShowInfo(this);
    }

    public void StartDrag()
    {
        if (!isPlayerPiece) return;
        _actionListPanel.gameObject.SetActive(false);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Move);
        if (_curCaverSlot != null)
        {
            _curCaverSlot.LeaveSlot(transform);
            _curCaverSlot = null;
        }
        BattleScene.Ins.UM.teamPanel.OnSelectPiece(pieceID);
    }

    public void StopDrag()
    {
        BattleScene.Ins.UM.infoBox.ShowInfo(this);
        if (!isPlayerPiece) return;
        CheckActionPos();
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
        _actionListPanel.gameObject.SetActive(true);
    }

    public void CancelSelect()
    {
        Debug.Log("取消选择棋子");
        _isAttacking = false;
        _rangeUI.CloseRange();
        _actionListPanel.gameObject.SetActive(false);
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
        }

        return result;
    }

    public void StartNormalAttack(bool range = false)
    {
        _isAttacking = true;
        if (!range) // 近战攻击
        {
            _rangeUI?.ShowAttackRange(unitAttrCenter.attr.GetRange(true));
            _attackPack = new AttackPack(unitAttrCenter.attr.GetAtk(DamageType.Melee),DamageType.Melee);
        }
        else // 远程攻击
        {
            if(unitAttrCenter.AmmoCount<=0)
            {
                Debug.Log("弹药不足，无法进行远程攻击");
                _isAttacking = false;
                return;
            }
            _rangeUI?.ShowAttackRange(unitAttrCenter.attr.GetRange(false));
            _attackPack = new AttackPack(unitAttrCenter.attr.GetAtk(DamageType.Ranged),DamageType.Ranged);
        }
    }

    private void CheckEnemy()
    {
        // 获取攻击目标点
        Vector3 atkPos = _rangeUI.GetAtkPos();
        float attackRadius = 1f; // 可根据需要调整攻击半径
        int enemyLayer = LayerMask.GetMask("Enemy");

        // 检测球体范围内的所有敌人
        Collider[] hitColliders = Physics.OverlapSphere(atkPos, attackRadius, enemyLayer);

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
                _rangeUI.CloseRange();
                return;
            }
        }
    }

    public void Attack(PieceController enemy)
    {
        Debug.Log("棋子攻击");
        if (_attackPack.damageType == DamageType.Melee)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Attack, false, 1f);
        }
        else if (_attackPack.damageType == DamageType.Ranged)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
        }

        
        // 聚能充能
        if (isPlayerPiece)
        {
            if (!BattleScene.Ins.BM.PlayerController.isBursting)
            {
                // 攻击充能
                BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.attackBurstCharge);
            }
            else
            {
                // 爆发状态下攻击同一目标增加额外伤害
                if (BattleScene.Ins.BM.PlayerController.burstTarget == enemy)
                {
                    _attackPack.damage = (int)(GameConst.burstDamageRate * _attackPack.damage );
                    _attackPack.damage += (int)(BattleScene.Ins.BM.PlayerController.totalDamage *
                                          GameConst.burstAddDamageRate);
                    BattleScene.Ins.BM.PlayerController.totalDamage += _attackPack.damage;
                }
                else
                {
                    BattleScene.Ins.BM.PlayerController.burstTarget = enemy;
                    BattleScene.Ins.BM.PlayerController.totalDamage = _attackPack.damage;
                }
            }
        }
        
        // 执行攻击
        BattleScene.Ins.BM.PieceAttack(this, enemy, _attackPack);
        
    }

    public void Hurt()
    {
        // 聚能充能
        if (isPlayerPiece)
        {
            // 受伤充能
            BattleScene.Ins.BM.PlayerController.ChargeBurst(GameConst.hurtBurstCharge);
        }
        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            Debug.Log($"{this.name} 受伤");
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Hit, false, 0.5f);
            transform.DOShakePosition(0.3f, 0.8f);
        });
    }

    public void Dead()
    {
        Debug.Log($"{this.name} 死亡");
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
        if (!unitAttrCenter.CostMP()) return;
        unitAttrCenter.FullAmmo();
    }
}