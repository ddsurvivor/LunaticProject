using System;
using UnityEngine;


public class ClickManager : MonoBehaviour
{
    // 正在拖动的棋子
    private PieceController _selectedPiece;
    
    [SerializeField] private RangeUI _rangeUI; // 范围UI
    private float _dragRange = 10f; // 拖动范围限制
    private Vector3 _dragStartPos; // 拖动起始位置

    private void Update()
    {
        if(! BattleScene.Ins.BM.PlayerController.isInTurn) return;
        
        // 鼠标左键点击时发射射线检测
        if (Input.GetMouseButtonDown(0))
        {
            // 判定是否点击到UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            StartDarg();
        }

        if (Input.GetMouseButton(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            DragPiece();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDrag();
        }
    }

    private void StartDarg()
    {
        Debug.Log("开始拖动");
        BattleScene.Ins.BM.camera.SetFollow(null);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (var hit in hits)
        {
            PieceController piece = hit.collider.GetComponent<PieceController>();
            if (piece == null || !piece.isPlayerPiece) continue;
            if(piece.unitAttrCenter.CurMovePoint<=0) continue;
            _selectedPiece?.CancelSelect();
            //BattleScene.Ins.BM.camera.SetFollow(piece.transform);
            _selectedPiece = piece;
            _selectedPiece.StartDrag();
            
            // 显示移动范围
            _dragStartPos = _selectedPiece.transform.position;
            _dragRange = _selectedPiece.unitAttrCenter.MoveRange;
            _rangeUI.ShowCircleRange(_dragStartPos, _dragRange);
            return;
        }
        
        _selectedPiece?.CancelSelect();
        _selectedPiece = null;
        
        
    }

    Vector3 point = Vector3.zero;
    private void DragPiece()
    {
        if (_selectedPiece == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Mask")) return;
            if (hit.collider.CompareTag("Ground"))
            {
                Debug.Log("点击地面，移动棋子");
                // 移动选中的棋子到地面点击位置
                point = hit.point;
            }
        }
        
        // 限制拖动范围
        Vector3 offset = point - _dragStartPos;
        if (offset.magnitude > _dragRange)
        {
            offset = offset.normalized * _dragRange;
            point = _dragStartPos + offset;
        }

        _selectedPiece.transform.position = (new Vector3(point.x
            , _selectedPiece.transform.position.y, point.z));
    }

    private void StopDrag()
    {
        if (_selectedPiece != null)
        {
            BattleScene.Ins.BM.camera.SetFollow(_selectedPiece.transform);
            _selectedPiece.StopDrag();
            _selectedPiece.unitAttrCenter.CostMP();
            _rangeUI.CloseRange();
            //_selectedPiece = null;
        }
    }
}