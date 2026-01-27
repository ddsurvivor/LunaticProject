using System;
using UnityEngine;


public class ClickManager : MonoBehaviour
{
    // 正在拖动的棋子
    private PieceController _selectedPiece;
    
    [SerializeField] private RangeUI _rangeUI; // 范围UI
    private float _dragRange = 10f; // 拖动范围限制
    private Vector3 _dragStartPos; // 拖动起始位置
    
    private bool _isDragging = false;
    
    

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

            if (!_isDragging)
            {
                ClickPiece();
            }
        }

        if (_selectedPiece != null && _isDragging)
        {
            DragPiece();
        }

        // if (Input.GetMouseButton(0))
        // {
        //     if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        //     {
        //         return;
        //     }
        //     DragPiece();
        // }

        if (Input.GetMouseButtonUp(0) && _isDragging)
        {
            // 判定是否点击到UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            StopDrag();
        }
        
        // 点击右键取消
        if (Input.GetMouseButtonDown(1))
        {
            _selectedPiece.transform.position = _dragStartPos;
            _selectedPiece = null;
            _isDragging = false;
            _rangeUI.CloseRange();
        }
    }

    private void ClickPiece()
    {
        
        BattleScene.Ins.BM.camera.SetFollow(null);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (var hit in hits)
        {
            PieceController piece = hit.collider.GetComponent<PieceController>();
            if (piece == null || !piece.isPlayerPiece) continue;
            //if(piece.unitAttrCenter.CurMovePoint<=0) continue;
            _selectedPiece?.CancelSelect();
            //BattleScene.Ins.BM.camera.SetFollow(piece.transform);
            _selectedPiece = piece;
            Debug.Log($"点击棋子{piece.name}");
            piece.ShowActionList();
            /*_selectedPiece.StartDrag();
            
            // 显示移动范围
            _dragStartPos = _selectedPiece.transform.position;
            _dragRange = _selectedPiece.unitAttrCenter.MoveRange;
            _rangeUI.ShowCircleRange(_dragStartPos, _dragRange);*/
            return;
        }
        
        //_selectedPiece?.CancelSelect();
        //_selectedPiece = null;
    }

    public void StartDarg(PieceController piece)
    {
        BattleScene.Ins.BM.camera.SetFollow(null);
        _selectedPiece = piece;
        _selectedPiece.StartDrag();
        _isDragging = true;
            
        // 显示移动范围
        _dragStartPos = _selectedPiece.transform.position;
        _dragRange = _selectedPiece.unitAttrCenter.MoveRange;
        _rangeUI.ShowCircleRange(_dragStartPos, _dragRange);
    }

    Vector3 point = Vector3.zero;
    public void DragPiece()
    {
        if (_selectedPiece == null) {return;}
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Mask")) return;
            if (hit.collider.CompareTag("Ground"))
            {
                //Debug.Log("点击地面，移动棋子");
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
            Debug.Log("停止拖动棋子");
            _isDragging = false;
            BattleScene.Ins.BM.camera.SetFollow(_selectedPiece.transform);
            _selectedPiece.unitAttrCenter.CostMP();
            _selectedPiece.StopDrag();
            _rangeUI.CloseRange();
            //_selectedPiece = null;
        }
    }
}