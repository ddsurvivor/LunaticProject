using System.Collections.Generic;
using UnityEngine;

/// <summary>附着现有战斗 Canvas 的顶部速度条；头像从右侧0向左侧100移动。</summary>
public sealed class SpeedTimelineUI : MonoBehaviour
{
    [SerializeField] private RectTransform track; // 右侧为进度 0，左侧为进度 100。
    [SerializeField] private SpeedTimelineMarkerUI markerTemplate; // 隐藏的头像模板，运行时按棋子复制。
    private BattleManager battle;
    private readonly Dictionary<PieceController, SpeedTimelineMarkerUI> markers = new();
    private readonly HashSet<PieceController> positionedMarkers = new(); // 新头像首次直接进入安全位置，之后才参与缓动。
    private readonly List<PieceController> removed = new();
    private readonly List<PieceController> lanePieces = new();
    private readonly List<float> lanePositions = new();
    private readonly Dictionary<int, Sprite> playerPortraits = new();
    private System.Comparison<PieceController> compareActionOrder;

    /// <summary>在战斗 Canvas 下创建并绑定速度条；未指定预制体时从 Resources 加载，缺失则返回 null。</summary>
    public static SpeedTimelineUI Create(Canvas canvas, BattleManager manager, SpeedTimelineUI prefab = null)
    {
        if (prefab == null) prefab = Resources.Load<SpeedTimelineUI>("UI/SpeedTimeline");
        if (prefab == null)
        {
            Debug.LogError("缺少速度条预制体 Resources/UI/SpeedTimeline", manager);
            return null;
        }
        var ui = Instantiate(prefab, canvas.transform, false);
        ui.battle = manager;
        ui.gameObject.SetActive(true);
        return ui;
    }

    /// <summary>供预制体生成器绑定轨道和头像模板，运行时沿用保存的引用。</summary>
    public void Configure(RectTransform trackRect, SpeedTimelineMarkerUI template)
    {
        track = trackRect;
        markerTemplate = template;
    }

    private void LateUpdate()
    {
        if (battle == null) return; // 允许预制体在编辑器中独立预览。
        if (!battle.SpeedTurnsActive)
        {
            gameObject.SetActive(false);
            return;
        }
        var participants = battle.SpeedParticipants();
        // 人数超过轨道容量时缩小头像，仍为当前者预留放大与间隔。
        float markerScale = Mathf.Clamp((track.rect.width / Mathf.Max(1, participants.Count) - 6f)
            / Mathf.Max(1f, markerTemplate.Rect.rect.width * 1.15f), 0.05f, 1f);
        // 同步动态参战者，死亡或离场后移除对应头像。
        removed.Clear();
        foreach (var old in markers.Keys)
            if (!participants.Contains(old)) removed.Add(old);
        foreach (var old in removed)
        {
            Destroy(markers[old].Rect.gameObject);
            markers.Remove(old);
            positionedMarkers.Remove(old);
        }
        foreach (var piece in participants)
        {
            if (!markers.TryGetValue(piece, out var marker))
            {
                marker = Instantiate(markerTemplate, track, false);
                marker.name = piece.name;
                marker.gameObject.SetActive(true);
                // 优先专用头像，缺失时依次回退角色立绘和棋子精灵。
                Sprite sprite = piece.pieceData?.turnPortrait;
                bool fullBody = sprite == null || piece.pieceData.turnPortraitIsFullBody;
                if (sprite == null && piece.isPlayerPiece)
                {
                    int id = piece.pieceID;
                    if (!playerPortraits.TryGetValue(id, out sprite))
                    {
                        sprite = Resources.Load<Sprite>(id > 100 ? "CG/PC0" + (id - 100) + "01" : "CG/PC0" + id + "C1");
                        playerPortraits[id] = sprite;
                    }
                }
                if (sprite == null && piece.pieceDisplay != null && piece.pieceDisplay.pieceSpriteRenderer != null)
                    sprite = piece.pieceDisplay.pieceSpriteRenderer.sprite;
                marker.BindPortrait(sprite, fullBody, piece.pieceData?.turnPortraitFraming);
                markers.Add(piece, marker);
            }
            bool active = battle.ActivePiece == piece;
            marker.SetState(active, piece.isPlayerPiece);
            marker.SetOverflow(battle.ActionProgress(piece));
            marker.Rect.localScale = Vector3.one * markerScale * (active ? 1.15f : 1f);
            if (active) marker.Rect.SetAsLastSibling();
        }
        LayoutLane(participants, markerScale);
    }

    private void LayoutLane(List<PieceController> participants, float markerScale)
    {
        lanePieces.Clear();
        lanePositions.Clear();
        lanePieces.AddRange(participants);
        compareActionOrder ??= battle.CompareActionOrder;
        // 尚未同步入队的新参战者保持本帧来源次序，不以 InstanceID 代替注册顺序。
        for (int i = 1; i < lanePieces.Count; i++)
        {
            var piece = lanePieces[i];
            int previous = i - 1;
            while (previous >= 0 && compareActionOrder(piece, lanePieces[previous]) < 0)
            {
                lanePieces[previous + 1] = lanePieces[previous];
                previous--;
            }
            lanePieces[previous + 1] = piece;
        }
        if (lanePieces.Count == 0) return;
        float inset = markerTemplate.Rect.rect.width * markerScale * 1.15f * 0.5f + 3;
        float left = -track.rect.width + inset, right = -inset;
        if (left > right) left = right = -track.rect.width * 0.5f;
        float spacing = lanePieces.Count > 1 ? Mathf.Min(inset * 2, (right - left) / (lanePieces.Count - 1)) : 0;
        // 双向挤压在端点内展开相近进度；只调整展示位置，保持真实进度与排序不变。
        for (int i = 0; i < lanePieces.Count; i++)
        {
            float x = Mathf.Clamp(-track.rect.width * Mathf.Clamp01(battle.ActionProgress(lanePieces[i]) / 100f), left, right);
            lanePositions.Add(i == 0 ? x : Mathf.Max(x, lanePositions[i - 1] + spacing));
        }
        lanePositions[lanePositions.Count - 1] = Mathf.Min(right, lanePositions[lanePositions.Count - 1]);
        for (int i = lanePositions.Count - 2; i >= 0; i--)
            lanePositions[i] = Mathf.Min(lanePositions[i], lanePositions[i + 1] - spacing);
        for (int i = 0; i < lanePieces.Count; i++)
        {
            var rect = markers[lanePieces[i]].Rect;
            Vector2 target = new(lanePositions[i], 0);
            Vector2 position = positionedMarkers.Add(lanePieces[i]) ? target
                : Vector2.Lerp(rect.anchoredPosition, target, 1 - Mathf.Exp(-20 * Time.unscaledDeltaTime));
            lanePositions[i] = Mathf.Clamp(position.x, left, right);
        }
        // 缓动、交棒、增员和窗口缩放均可能压缩间距；每帧重新约束，避免只保证最终位置不重叠。
        for (int i = 1; i < lanePositions.Count; i++)
            lanePositions[i] = Mathf.Max(lanePositions[i], lanePositions[i - 1] + spacing);
        lanePositions[lanePositions.Count - 1] = Mathf.Min(right, lanePositions[lanePositions.Count - 1]);
        for (int i = lanePositions.Count - 2; i >= 0; i--)
            lanePositions[i] = Mathf.Min(lanePositions[i], lanePositions[i + 1] - spacing);
        for (int i = 0; i < lanePieces.Count; i++)
            markers[lanePieces[i]].Rect.anchoredPosition = new Vector2(lanePositions[i], 0);
    }

}
