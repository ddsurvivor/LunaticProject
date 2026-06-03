using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;


public class ClickManager : MonoBehaviour
{
    // 正在拖动的棋子
    private PieceController _selectedPiece;

    [SerializeField] private RangeUI _rangeUI; // 范围UI
    private float _dragRange = 10f; // 拖动范围限制
    private Vector3 _dragStartPos; // 拖动起始位置

    private bool _isDragging = false;

    Vector3 point = Vector3.zero;

    [LabelText("拖动方式移动")]
    public bool dragMove;

    private void Update()
    {
        if (!BattleScene.Ins.BM.PlayerController.isInTurn) return;

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
            // 判定是否点击到UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (dragMove)
            {
                DragPiece();
            }
            else
            {
                DragMoveIcon();
            }
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

            if (dragMove)
            {
                StopDrag();
            }
            else
            {
                ClickMovePoint();
            }
        }

        // 点击右键取消
        if (Input.GetMouseButtonDown(1))
        {
            if (_isDragging)
            {
                _selectedPiece.transform.position = _dragStartPos;
                _selectedPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
                _selectedPiece = null;
                _isDragging = false;
                _rangeUI.CloseRange();
                BattleScene.Ins.BM.moveManager.ClearPathLine();
            }
            BattleScene.Ins.UM.pieceActionListPanel.gameObject.SetActive(false);
            BattleScene.Ins.UM.pieceInfoPanel.StopMpIconsBlink();
        }
        
        // 按下123的时候，按顺序切换选中的玩家棋子
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ClickPieceByIndex(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ClickPieceByIndex(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ClickPieceByIndex(2);
        }
    }

    private PieceController lastHoveredPiece = null;

    // LateUpdate 保持不变，只需修改射线检测部分
    public void LateUpdate()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        PieceController currentHoveredPiece = null;
        foreach (var hit in hits)
        {
            var piece = hit.collider.GetComponent<PieceController>();
            if (piece != null)
            {
                currentHoveredPiece = piece;
                break; // 只取第一个被射线穿透命中的棋子
            }
        }

        if (lastHoveredPiece != currentHoveredPiece)
        {
            if (lastHoveredPiece != null)
            {
                lastHoveredPiece.ShowOutline(false);
            }
            if (currentHoveredPiece != null)
            {
                currentHoveredPiece.ShowOutline(true);
            }
            lastHoveredPiece = currentHoveredPiece;
        }
    }

    private void ClickPieceByIndex(int index)
    {
        var playerPieces = BattleScene.Ins.BM.PlayerController.pieces;
        if (index < playerPieces.Count)
        {
            var piece = playerPieces[index];
            if (piece.cantControl || piece.isDead) return;
            _selectedPiece?.CancelSelect();
            _selectedPiece = piece;
            piece.OnSelect();
            Debug.Log($"通过快捷键点击棋子{piece.name}");
            BattleScene.Ins.UM.ShowPieceActionPanel(piece);
            BattleScene.Ins.UM.ShowPieceState(piece);
            BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
            BattleScene.Ins.BM.camera.SetFollow(piece.transform);
        }
    }

    private void ClickPiece()
    {
        //BattleScene.Ins.BM.camera.SetFollow(null);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (var hit in hits)
        {
            PieceController piece = hit.collider.GetComponent<PieceController>();
            if (piece == null || !piece.isPlayerPiece) continue;
            if (piece.cantControl)return;
            if(piece.isDead) continue;
            //if(piece.unitAttrCenter.CurMovePoint<=0) continue;
            _selectedPiece?.CancelSelect();
            //BattleScene.Ins.BM.camera.SetFollow(piece.transform);
            _selectedPiece = piece;
            piece.OnSelect();
            Debug.Log($"点击棋子{piece.name}");
            //piece.ShowActionList();
            BattleScene.Ins.UM.ShowPieceActionPanel(piece);
            BattleScene.Ins.UM.ShowPieceState(piece);
            BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
            BattleScene.Ins.BM.camera.SetFollow(piece.transform);
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
        point = _dragStartPos;
        _dragRange = _selectedPiece.unitAttrCenter.MoveRange;
        _rangeUI.ShowCircleRange(_dragStartPos, _dragRange);

        if (dragMove)
        {
            
        }
        else
        {
            _rangeUI.ShowMoveIcon(true);
        }
    }


    public void DragPiece()
    {
        if (_selectedPiece == null)
        {
            return;
        }

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

        _selectedPiece.CheckFace(_selectedPiece.transform.position - _dragStartPos);
    }

    /// <summary>
    /// 实时更新移动范围
    /// </summary>
    public void DragMoveIcon()
    {
        if (_selectedPiece == null)
        {
            return;
        }

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
        _rangeUI.UpdateMove(point);
        
        // move manager同步更新路径
        BattleScene.Ins.BM.moveManager.PreviewMove(_selectedPiece.gameObject, point, _dragRange);
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

    public void ClickMovePoint()
    {
        if (_selectedPiece != null)
        {
            Debug.Log("停止拖动棋子");
            _isDragging = false;
            BattleScene.Ins.BM.camera.SetFollow(_selectedPiece.transform);
            _selectedPiece.unitAttrCenter.CostMP();
            Vector3 targetPos = new Vector3(_rangeUI.moveIcon.transform.position.x,
                _selectedPiece.transform.position.y,
                _rangeUI.moveIcon.transform.position.z);
            _selectedPiece.CheckFace(targetPos - _dragStartPos);
            _selectedPiece.StartMove();
            var piece = _selectedPiece;
            // piece.transform.DOMove(targetPos, 1.0f).OnComplete(() =>
            // {
            //     piece.StopMove();
            // });
            _rangeUI.CloseRange();
            //_selectedPiece = null;
            // 确定开始移动
            BattleScene.Ins.BM.moveManager.ExecuteMove(_selectedPiece.gameObject, piece.StopMove);
        }
    }
}