using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

/// <summary>由 BattleManager 自动接入，场景只需摆放部署区域预制体。</summary>
public sealed class BattleDeploymentController : MonoBehaviour
{
    private BattleManager manager;
    private BattleDeploymentArea[] areas;
    private PieceController selected;
    private GameObject overlay;
    private UnityEngine.UI.Text statusText;
    private GameObject endTurnObject, burstObject;
    private bool endTurnWasActive, burstWasActive;
    private bool active;

    public void Begin(BattleManager battle, BattleDeploymentArea[] deploymentAreas)
    {
        End();
        manager = battle;
        areas = deploymentAreas;
        active = true;
        foreach (var area in areas) { area.Refresh(); area.SetDeploymentVisible(true); }
        var ui = BattleScene.Ins != null && BattleScene.Ins.BM == battle ? BattleScene.Ins.UM : null;
        if (ui != null)
        {
            endTurnObject = ui.endTurnButton != null ? ui.endTurnButton.gameObject : null;
            burstObject = ui.burstButton != null ? ui.burstButton.gameObject : null;
            endTurnWasActive = endTurnObject != null && endTurnObject.activeSelf;
            burstWasActive = burstObject != null && burstObject.activeSelf;
            endTurnObject?.SetActive(false);
            burstObject?.SetActive(false);
            ui.pieceActionListPanel?.gameObject.SetActive(false);
        }
        BuildOverlay(ui != null && ui.turnNumberText != null ? ui.turnNumberText.font : null);
        Physics.SyncTransforms();
        // 保留合法初始站位；其余棋子在所有出生区域中依次找空位。
        foreach (var piece in ControllablePieces())
        {
            if (TryDestination(piece, piece.transform.position, areas, out _, out _)) continue;
            var agent = piece.GetComponent<NavMeshAgent>();
            float radius = agent != null ? agent.radius : 0.5f;
            bool placed = false;
            foreach (var area in areas)
            {
                foreach (var candidate in area.CandidatePositions(radius))
                {
                    if (!TryPlace(piece, candidate, false)) continue;
                    placed = true;
                    break;
                }
                if (placed) break;
            }
        }
        var first = ControllablePieces().FirstOrDefault();
        if (first != null) Select(first);
        ValidateAll();
    }

    private IEnumerable<PieceController> ControllablePieces() => manager.PlayerController.pieces.Where(p =>
        p != null && p.gameObject.activeInHierarchy && p.isPlayerPiece && !p.isDead && !p.cantControl);

    public bool ValidateAll()
    {
        if (!active) return false;
        foreach (var piece in ControllablePieces())
            if (!TryDestination(piece, piece.transform.position, areas, out _, out string error))
            {
                SetStatus($"{PieceName(piece)}：{error}，请调整后开始战斗。");
                return false;
            }
        SetStatus("左键选棋子，再点击绿色区域部署；右键取消选择。准备好后开始战斗。");
        return true;
    }

    public bool TryPlace(PieceController piece, Vector3 position, bool showError = true)
    {
        if (!active || !manager.IsDeploying || !ControllablePieces().Contains(piece)) return false;
        if (!TryDestination(piece, position, areas, out var destination, out var error))
        {
            if (showError) SetStatus(error);
            return false;
        }
        var agent = piece.GetComponent<NavMeshAgent>();
        if (!agent.Warp(destination))
        {
            if (showError) SetStatus("该位置无法放置棋子，请选择区域内其他位置。");
            return false;
        }
        if (agent.isOnNavMesh) agent.ResetPath();
        // 不走战斗移动流程，不扣行动力、不触发沿途攻击或交互。
        Physics.SyncTransforms();
        if (showError) SetStatus($"{PieceName(piece)}已部署。可以继续选择其他棋子。");
        return true;
    }

    public static bool TryDestination(PieceController piece, Vector3 desired,
        IEnumerable<BattleDeploymentArea> regions, out Vector3 destination, out string error)
    {
        destination = desired;
        error = "请选择绿色部署区域内的位置";
        var agent = piece != null ? piece.GetComponent<NavMeshAgent>() : null;
        if (agent == null || !agent.isActiveAndEnabled) { error = "棋子导航未启用"; return false; }
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(desired, out var hit, 0.5f + Mathf.Abs(agent.baseOffset), filter))
        { error = "该位置没有可行走导航面"; return false; }
        destination = hit.position;
        Vector3 sampledPosition = destination;
        Vector3 requestedFootprint = new Vector3(desired.x, sampledPosition.y, desired.z);
        if (!regions.Any(a => a != null && a.isActiveAndEnabled &&
            a.Contains(sampledPosition, agent.radius) && a.Contains(requestedFootprint, agent.radius))) return false;
        // 防止采样跳到相邻楼层，区域高度与实际导航面必须一致。
        if (Mathf.Abs(desired.y - agent.baseOffset - destination.y) > 0.6f &&
            Mathf.Abs(desired.y - destination.y) > 0.6f) return false;
        float radius = Mathf.Max(0.05f, agent.radius);
        Vector3 bottom = destination + Vector3.up * (radius + 0.05f);
        Vector3 top = destination + Vector3.up * Mathf.Max(radius + 0.05f, agent.height - radius);
        foreach (var collider in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (collider.transform == piece.transform || collider.transform.IsChildOf(piece.transform)) continue;
            error = collider.GetComponentInParent<PieceController>() != null ? "这里已有其他棋子" : "这里被障碍物占用";
            return false;
        }
        error = "";
        return true;
    }

    private void Update()
    {
        if (!active || manager == null || !manager.IsDeploying) return;
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { Select(null); return; }
        if (!Input.GetMouseButtonDown(0) || Camera.main == null) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 2000, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
        foreach (var hit in hits)
        {
            var piece = hit.collider.GetComponentInParent<PieceController>();
            if (piece == null || !ControllablePieces().Contains(piece)) continue;
            Select(piece);
            return;
        }
        if (selected == null) { SetStatus("先点击要调整位置的己方棋子。"); return; }
        // 区域可以位于地面或高台，分别按各自的水平面投射鼠标。
        float nearest = float.PositiveInfinity;
        Vector3 target = default;
        foreach (var area in areas)
        {
            if (area == null || !area.isActiveAndEnabled) continue;
            if (!new Plane(Vector3.up, area.transform.position).Raycast(ray, out float distance) || distance >= nearest) continue;
            var candidate = ray.GetPoint(distance);
            if (!area.Contains(candidate)) continue;
            nearest = distance;
            target = candidate;
        }
        if (float.IsPositiveInfinity(nearest)) { SetStatus("只能在绿色部署区域内调整位置。"); return; }
        TryPlace(selected, target);
    }

    private void Select(PieceController piece)
    {
        if (selected != null) selected.rangeUI?.ShowSelect(false);
        selected = piece;
        if (selected == null) return;
        selected.rangeUI?.ShowSelect(true);
        manager.cameraController?.SetFollow(selected.transform);
        SetStatus($"已选择{PieceName(selected)}，点击任一绿色部署区域放置。");
    }

    public void End()
    {
        if (!active) return;
        active = false;
        Select(null);
        if (areas != null) foreach (var area in areas) if (area != null) area.SetDeploymentVisible(false);
        endTurnObject?.SetActive(endTurnWasActive);
        burstObject?.SetActive(burstWasActive);
        if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
        overlay = null;
        statusText = null;
    }

    private void OnDisable() => End();
    private static string PieceName(PieceController piece) => piece.playerData?.NAME ?? piece.name;
    private void SetStatus(string text) { if (statusText != null) statusText.text = text; }

    private void BuildOverlay(Font font)
    {
        overlay = new GameObject("DeploymentUI", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        overlay.transform.SetParent(transform, false);
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var panel = new GameObject("DeploymentPanel", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(overlay.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 24);
        rect.sizeDelta = new Vector2(1000, 100);
        panel.GetComponent<UnityEngine.UI.Image>().color = new Color(0.04f, 0.07f, 0.09f, 0.96f);
        statusText = Text(rect, "Instruction", font, new Vector2(20, 12), new Vector2(-220, -12));
        statusText.fontSize = 22;
        statusText.alignment = TextAnchor.MiddleLeft;
        var buttonObject = new GameObject("ConfirmDeployment", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        var buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.SetParent(rect, false);
        buttonRect.anchorMin = new Vector2(1, 0);
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = new Vector2(-200, 16);
        buttonRect.offsetMax = new Vector2(-16, -16);
        buttonObject.GetComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.42f, 0.3f);
        var label = Text(buttonRect, "Label", font, Vector2.zero, Vector2.zero);
        label.text = "开始战斗";
        label.fontSize = 26;
        label.alignment = TextAnchor.MiddleCenter;
        buttonObject.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(manager.ConfirmDeployment);
    }

    private static UnityEngine.UI.Text Text(Transform parent, string name, Font font, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
        var text = go.GetComponent<UnityEngine.UI.Text>();
        text.font = font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
