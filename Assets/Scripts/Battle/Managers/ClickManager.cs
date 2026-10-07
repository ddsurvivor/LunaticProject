using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;


public partial class ClickManager : MonoBehaviour
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
    
    // 记录上一次的移动位置
    private PieceController lastMovePiece;
    private Vector3 lastStartPos;
    

    private void Update()
    {
        if (BattleScene.Ins.BM.HasCombatPresentation) return;
        if (!BattleScene.Ins.BM.PlayerController.isInTurn) return;

        // 鼠标左键点击时发射射线检测
        if (Input.GetMouseButtonDown(0))
        {
            // 判定是否点击到UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // 如果点击到的UI是RangUI，则不return
                if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject !=
                    null ){
                    
                    //Debug.LogWarning($"点击到了UI:{UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()}，return");
                    return;
                }
            }
            /*if(BattleScene.Ins.UM.pieceActionListPanel.gameObject.activeInHierarchy)
            {return;}*/
            if(_selectedPiece!=null && _selectedPiece.IsUsingSkill) return;

            if (!_isDragging)
            {
                ClickPiece();
            }
        }

        if (_selectedPiece != null && _isDragging)
        {
            // 判定是否点击到UI
            // if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            // {
            //     return;
            // }

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

        if (Input.GetMouseButtonDown(0) && _isDragging)
        {
            // 判定是否点击到UI
            /*if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }*/

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
            ClearEnemyTargetLines(true);
            if (_isDragging)
            {
                _selectedPiece.transform.position = _dragStartPos;
                _selectedPiece.pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
                _selectedPiece = null;
                _isDragging = false;
                _rangeUI.CloseRange();
                BattleScene.Ins.BM.moveManager.ResetPreviewState();
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

    public void LateUpdate()
    {
        var scene = BattleScene.Ins;
        if (scene == null || scene.BM == null || !scene.BM.CanInspectEnemyTargets)
        {
            SetHoveredPiece(null);
            ClearEnemyTargetLines();
            return;
        }
        bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        SetHoveredPiece(overUI ? null : FindPieceUnderPointer());
        UpdateTargetLinePresentation();
    }

    private PieceController FindPieceUnderPointer()
    {
        if (Camera.main == null) return null;
        var hits = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
        PieceController nearest = null;
        float distance = float.PositiveInfinity;
        foreach (var hit in hits)
        {
            var piece = hit.collider.GetComponentInParent<PieceController>();
            if (piece == null || piece.isDead || !piece.gameObject.activeInHierarchy || hit.distance >= distance) continue;
            if (piece is EnemyController enemy && !enemy.isActived) continue;
            nearest = piece;
            distance = hit.distance;
        }
        return nearest;
    }

    private void ClickPieceByIndex(int index)
    {
        var playerPieces = BattleScene.Ins.BM.PlayerController.pieces;
        if (index < playerPieces.Count)
        {
            var piece = playerPieces[index];
            if (piece.cantControl || piece.isDead) return;
            CancelMovementPreview();
            ClearEnemyTargetLines(true);
            _selectedPiece?.CancelSelect();
            _selectedPiece = piece;
            piece.OnSelect();
            Debug.Log($"通过快捷键点击棋子{piece.name}");
            BattleScene.Ins.UM.ShowPieceActionPanel(piece);
            BattleScene.Ins.UM.ShowPieceState(piece);
            BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
            BattleScene.Ins.BM.cameraController.SetFollow(piece.transform);
        }
    }

    private void ClickPiece()
    {
        var piece = FindPieceUnderPointer();
        if (piece == null) return;
        CancelMovementPreview();
        ClearEnemyTargetLines(true);
        if (piece is EnemyController enemy)
        {
            PinEnemyTargetLine(enemy);
            return;
        }
        if (!piece.isPlayerPiece || piece.cantControl || piece.isDead) return;
        _selectedPiece?.CancelSelect();
        _selectedPiece = piece;
        piece.OnSelect();
        BattleScene.Ins.UM.ShowPieceActionPanel(piece);
        BattleScene.Ins.UM.ShowPieceState(piece);
        BattleScene.Ins.UM.pieceInfoPanel.OnSelectPiece(piece);
        BattleScene.Ins.BM.cameraController.SetFollow(piece.transform);
        GM.Ins.AM.PlayAudio(AudioCueType.Select);
    }

    private void CancelMovementPreview()
    {
        if (!_isDragging) return;
        if (dragMove && _selectedPiece != null)
        {
            _selectedPiece.transform.position = _dragStartPos;
            _selectedPiece.pieceDisplay?.ChangeDisplayState(PieceDisplayState.Idle);
        }
        _isDragging = false;
        _rangeUI?.CloseRange();
        BattleScene.Ins?.BM?.moveManager?.ResetPreviewState();
    }

    public void StartDarg(PieceController piece)
    {
        if (piece == null || piece.isDead || !piece.isPlayerPiece) return;
        CancelMovementPreview();
        ClearEnemyTargetLines(true);
        BattleScene.Ins.BM.moveManager.ResetPreviewState();
        BattleScene.Ins.BM.cameraController.SetFollow(null);
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
            BattleScene.Ins.BM.cameraController.SetFollow(_selectedPiece.transform);
            if (!_selectedPiece.unitAttrCenter.CostMP(ActionType.移动))
            {
                _rangeUI.CloseRange();
                return;
            }
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
            BattleScene.Ins.BM.cameraController.SetFollow(_selectedPiece.transform);
            if (!BattleScene.Ins.BM.moveManager.TryGetPreviewPosition(_selectedPiece.gameObject, out var targetPos) ||
                !_selectedPiece.unitAttrCenter.CostMP(ActionType.移动))
            {
                _rangeUI.CloseRange();
                return;
            }
            _selectedPiece.CheckFace(targetPos - _dragStartPos);
            _selectedPiece.StartMove();
            // 记录并显示撤回
            lastMovePiece = _selectedPiece;
            lastStartPos = _dragStartPos;
            BattleScene.Ins.UM.ShowUndoMoveButton(
                _selectedPiece.unitAttrCenter.GetBuffStacks(BuffType.SpontaneousMemeticAttack) == 0);
            var piece = _selectedPiece;
            // piece.transform.DOMove(targetPos, 1.0f).OnComplete(() =>
            // {
            //     piece.StopMove();
            // });
            _rangeUI.CloseRange();
            //_selectedPiece = null;
            
            // 确定开始移动
            if (BattleScene.Ins.BM.moveManager.ExecuteMove(_selectedPiece.gameObject, piece.StopMove) <= 0f)
                piece.StopMove();
            
        }
    }

    /// <summary>
    /// 撤回移动
    /// </summary>
    public void CancelMove()
    {
        if (lastMovePiece != null && (lastMovePiece.isDead ||
            lastMovePiece.unitAttrCenter.GetBuffStacks(BuffType.SpontaneousMemeticAttack) != 0)) return;
        if (lastMovePiece != null)
        {
            // 撤回上一次的移动
            lastMovePiece.transform.position = lastStartPos;
            lastMovePiece.StopMove();
            
            // 撤回消耗的移动点数
            int count = GM.Ins.DM.gameConstSO.GetActionPointCost(ActionType.移动);
            lastMovePiece.unitAttrCenter.AddMP(count);
            lastMovePiece = null;
            BattleScene.Ins.UM.ShowUndoMoveButton(false);
        }
    }
}
