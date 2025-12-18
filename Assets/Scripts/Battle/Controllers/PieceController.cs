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
    
    public bool isPlayerPiece;// 是否是玩家棋子
    
    private bool _isDragging = false;// 是否正在拖拽
    
    private Vector3 _originalPosition;// 原始位置

    private ActionSlot _curActionSlot; // 当前绑定的点位

    private void OnMouseEnter()
    {
        //_rangeUI.ShowCircleRange(3);
    }

    private void OnMouseExit()
    {
        /*if(_isDragging) return;
        _rangeUI.CloseRange();*/
    }

    private void OnMouseDown()
    {
        _originalPosition = transform.position;
    }

    private void OnMouseDrag()
    {
        if (!isPlayerPiece) return;
        
        _isDragging = true;
        if (_curActionSlot != null)
        {
            _curActionSlot.LeaveSlot(transform);
            _curActionSlot = null;
        }
        // 鼠标拖拽时，节点跟随鼠标位置移动
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // 假设地面Layer为"Ground"
        int groundLayer = LayerMask.GetMask("Ground");
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 point = hit.point;
            transform.position = new Vector3(point.x, transform.position.y, point.z);
        }

        //_rangeUI.ShowMoveIcon(true);
    }

    private void OnMouseUp()
    {
        _isDragging = false;
        if(CheckActionPos()) return;
        //transform.DOMove(_originalPosition, 0.5f);
        // _rangeUI.ShowMoveIcon(false);
        // _rangeUI.CloseRange();
        // MoveTo(new Vector3(_rangeUI.moveIcon.transform.position.x, 
        //     transform.position.y, _rangeUI.moveIcon.transform.position.z) );
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
}