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
    //public string pieceName;
    public UnitAttrCenter unitAttrCenter;// 单位属性中心

    [SerializeField]
    private RangeUI _rangeUI;// 范围UI
    
    [SerializeField]
    private PieceActionListPanel _actionListPanel;// 棋子动作列表面板
    
    public bool isPlayerPiece;// 是否是玩家棋子
    
    private bool _isDragging = false;// 是否正在拖拽
    
    private Vector3 _originalPosition;// 原始位置

    private CaverSlot _curCaverSlot; // 当前绑定的点位
    
    // 当前攻击数据
    private bool _isAttacking = false;// 是否正在攻击
    [SerializeField][ReadOnly]private int _damage;
    [SerializeField][ReadOnly]private DamageType _damageType;
    
    public List<InteractArea> interactAreas = new();// 可交互区域列表
    
    public List<ActionType> availableActions = new();// 可用动作列表
    
    public bool isActived = false;// 是否被激活

    public void Init()
    {
        unitAttrCenter.Init();
        availableActions.Add(ActionType.Move);
        availableActions.Add(ActionType.Attack);
        availableActions.Add(ActionType.Range_ATK);
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
    }
    public void TurnEnd()
    {
        if(_actionListPanel!=null) {_actionListPanel.gameObject.SetActive(false);}    
    }

    private void OnMouseEnter()
    {
        //_rangeUI.ShowCircleRange(3);
    }

    private void OnMouseExit()
    {
        /*if(_isDragging) return;
        _rangeUI.CloseRange();*/
    }


    private void OnMouseUp()
    {
        BattleScene.Ins.UM.infoBox.ShowInfo(this);
        if(!isPlayerPiece) return;
        _isDragging = false;
        CheckActionPos();
        _actionListPanel.gameObject.SetActive(true);              
    }

    public void StartDrag()
    {
        if(!isPlayerPiece) return;
        _actionListPanel.gameObject.SetActive(false);
        if (_curCaverSlot != null)
        {
            _curCaverSlot.LeaveSlot(transform);
            _curCaverSlot = null;
        }
    }
    public void StopDrag()
    {
        if(!isPlayerPiece) return;
        CheckActionPos();
        _actionListPanel.gameObject.SetActive(true);
    }
    

    private bool CheckActionPos()
    {
        interactAreas.Clear();
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1f);
        foreach (var collider in hitColliders)
        {
            CaverSlot caverSlot = collider.transform.GetComponent<CaverSlot>();
            if (caverSlot!=null && !caverSlot.isFull)
            {
                caverSlot.AddToSlot(transform);
                _curCaverSlot = caverSlot;
                return true;
            }
            
            InteractArea interactArea = collider.transform.GetComponent<InteractArea>();
            if (interactArea != null)
            {
                interactAreas.Add(interactArea);
                return true;
            }
        }
        return false;
    }

    public void StartNormalAttack(bool range = false)
    {
        _isAttacking = true;
        if (!range)
        {
            _rangeUI?.ShowAttackRange(unitAttrCenter.attr.GetRange(true));
            _damageType = DamageType.Melee;
            _damage = unitAttrCenter.attr.GetAtk(DamageType.Melee);
        }
        else
        {
            _rangeUI?.ShowAttackRange(unitAttrCenter.attr.GetRange(false));
            _damageType = DamageType.Ranged;
            _damage = unitAttrCenter.attr.GetAtk(DamageType.Ranged);
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
                if(!unitAttrCenter.CostMP()) return;
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
        enemy.unitAttrCenter.TakeDamage(_damage,_damageType,0);
    }

    
    
}