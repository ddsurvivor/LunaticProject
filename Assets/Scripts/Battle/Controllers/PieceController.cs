using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 棋子控制器
/// </summary>
public class PieceController : MonoBehaviour
{
    [SerializeField]
    private UnitAttrCenter _attrCenter;// 单位属性中心

    [SerializeField]
    private RangeUI _rangeUI;// 范围UI
    
    [SerializeField]
    private PieceActionListPanel _actionListPanel;// 棋子动作列表面板
    
    public bool isPlayerPiece;// 是否是玩家棋子
    
    private bool _isDragging = false;// 是否正在拖拽
    
    private Vector3 _originalPosition;// 原始位置

    private ActionSlot _curActionSlot; // 当前绑定的点位
    
    private bool _isAttacking = false;// 是否正在攻击
    private int _damage;
    private DamageType _damageType;

    public void Start()
    {
        _attrCenter.Init();
    }

    private void Update()
    {
        if (_isAttacking)
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

    private void OnMouseEnter()
    {
        //_rangeUI.ShowCircleRange(3);
    }

    private void OnMouseExit()
    {
        /*if(_isDragging) return;
        _rangeUI.CloseRange();*/
    }

    // private void OnMouseDown()
    // {
    //     _originalPosition = transform.position;
    // }

    // private void OnMouseDrag()
    // {
    //     if (!isPlayerPiece) return;
    //     
    //     _isDragging = true;
    //     _actionListPanel.gameObject.SetActive(false);
    //     if (_curActionSlot != null)
    //     {
    //         _curActionSlot.LeaveSlot(transform);
    //         _curActionSlot = null;
    //     }
    //     // 鼠标拖拽时，节点跟随鼠标位置移动
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     RaycastHit hit;
    //     // 假设地面Layer为"Ground"
    //     int groundLayer = LayerMask.GetMask("Ground");
    //     if (Physics.Raycast(ray, out hit, 100f, groundLayer))
    //     {
    //         Vector3 point = hit.point;
    //         transform.position = new Vector3(point.x, transform.position.y, point.z);
    //     }
    //
    //     //_rangeUI.ShowMoveIcon(true);
    // }

    private void OnMouseUp()
    {
        _isDragging = false;
        _actionListPanel.gameObject.SetActive(true);
        if(CheckActionPos()) return;
        //transform.DOMove(_originalPosition, 0.5f);
        // _rangeUI.ShowMoveIcon(false);
        // _rangeUI.CloseRange();
        // MoveTo(new Vector3(_rangeUI.moveIcon.transform.position.x, 
        //     transform.position.y, _rangeUI.moveIcon.transform.position.z) );
    }

    public void StartDrag()
    {
        _actionListPanel.gameObject.SetActive(false);
        if (_curActionSlot != null)
        {
            _curActionSlot.LeaveSlot(transform);
            _curActionSlot = null;
        }
    }
    public void StopDrag()
    {
        _actionListPanel.gameObject.SetActive(true);
        if(CheckActionPos()) return;
    }

    // private void MoveTo(Vector3 targetPosition)
    // {
    //     // 实现棋子移动的逻辑
    //     transform.DOMove(targetPosition, 0.5f);
    // }

    private bool CheckActionPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // 假设地面Layer为"Ground"
        if (Physics.Raycast(ray, out hit, 100f))
        {
            ActionSlot actionSlot = hit.transform.GetComponent<ActionSlot>();
            if (actionSlot!=null && !actionSlot.isFull)
            {
                actionSlot.AddToSlot(transform);
                _curActionSlot = actionSlot;
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
            _rangeUI.ShowAttackRange(_attrCenter.attr.GetRange(true));
            _damageType = DamageType.Melee;
            _damage = _attrCenter.attr.GetAtk(DamageType.Melee);
        }
        else
        {
            _rangeUI.ShowAttackRange(_attrCenter.attr.GetRange(false));
            _damageType = DamageType.Ranged;
            _damage = _attrCenter.attr.GetAtk(DamageType.Ranged);
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

        if (hitColliders.Length > 0)
        {
            foreach (var collider in hitColliders)
            {
                // 在这里处理攻击逻辑，比如对collider.transform进行伤害计算
                Debug.Log($"Attacked target: {collider.transform.name}");
                UnitAttrCenter enemyAttrCenter = collider.transform.GetComponent<UnitAttrCenter>();
                if (enemyAttrCenter != null)
                {
                        
                    Attack(enemyAttrCenter);
                }
            }
            // 结束攻击状态
            _isAttacking = false;
            _rangeUI.CloseRange();
        }
    }

    private void Attack(UnitAttrCenter enemy)
    {
        enemy.TakeDamage(_damage,_damageType,0);
    }
    
}