using System;
using UnityEngine;


public class ClickManager : MonoBehaviour
{
    // 正在拖动的棋子
    private PieceController _selectedPiece;

    private void Update()
    {
        if(! BattleScene.Ins.BM.PlayerController.isInTurn) return;
        // 鼠标左键点击时发射射线检测
        if (Input.GetMouseButtonDown(0))
        {
            StartDarg();
        }

        if (Input.GetMouseButton(0))
        {
            DragPiece();
        }

        if (Input.GetMouseButtonUp(0))
        {
            StopDrag();
        }
    }

    private void StartDarg()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (var hit in hits)
        {
            PieceController piece = hit.collider.GetComponent<PieceController>();
            if (piece == null || !piece.isPlayerPiece) continue;
            
            //BattleScene.Ins.BM.camera.SetFollow(piece.transform);
            _selectedPiece = piece;
            _selectedPiece.StartDrag();
        }
    }

    private void DragPiece()
    {
        if (_selectedPiece == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        Vector3 point = Vector3.zero;
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

        _selectedPiece.transform.position = (new Vector3(point.x
            , _selectedPiece.transform.position.y, point.z));
    }

    private void StopDrag()
    {
        if (_selectedPiece != null)
        {
            BattleScene.Ins.BM.camera.SetFollow(_selectedPiece.transform);
            _selectedPiece.StopDrag();
            _selectedPiece = null;
        }
    }
}