using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>附着现有战斗 Canvas 的顶部速度条；头像从右侧0向左侧100移动。</summary>
public sealed class SpeedTimelineUI : MonoBehaviour
{
    [SerializeField] private RectTransform track;
    [SerializeField] private SpeedTimelineMarkerUI markerTemplate;
    [SerializeField] private float laneOffset = 22f;
    private BattleManager battle;
    private readonly Dictionary<PieceController, SpeedTimelineMarkerUI> markers = new();

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
        foreach (var old in markers.Keys.Where(p => !participants.Contains(p)).ToArray())
        {
            Destroy(markers[old].Rect.gameObject);
            markers.Remove(old);
        }
        var occupied = new Dictionary<(bool, int), int>();
        foreach (var piece in participants)
        {
            if (!markers.TryGetValue(piece, out var marker))
            {
                marker = Instantiate(markerTemplate, track, false);
                marker.name = piece.name;
                marker.gameObject.SetActive(true);
                var portrait = marker.Portrait;
                portrait.color = Color.white;
                portrait.preserveAspect = true;
                portrait.sprite = piece.pieceData?.turnPortrait;
                if (portrait.sprite == null && piece.isPlayerPiece)
                {
                    int id = piece.pieceID;
                    portrait.sprite = Resources.Load<Sprite>(id > 100 ? "CG/PC0" + (id - 100) + "01" : "CG/PC0" + id + "C1");
                }
                if (portrait.sprite == null && piece.pieceDisplay != null && piece.pieceDisplay.pieceSpriteRenderer != null)
                    portrait.sprite = piece.pieceDisplay.pieceSpriteRenderer.sprite;
                markers.Add(piece, marker);
            }
            bool active = battle.ActivePiece == piece;
            marker.SetState(active, piece.isPlayerPiece);
            float lane = piece.isPlayerPiece ? laneOffset : -laneOffset;
            float x = -track.rect.width * Mathf.Clamp01(battle.ActionProgress(piece) / 100f);
            var group = (piece.isPlayerPiece, Mathf.RoundToInt(x / 24));
            occupied.TryGetValue(group, out int overlap);
            occupied[group] = overlap + 1;
            // 满条/起跑时将同阵营头像扇形展开，避免只看见最后一个棋子。
            x += overlap * (x < -track.rect.width / 2 ? 18 : -18);
            x = Mathf.Clamp(x, -track.rect.width, 0);
            lane += overlap % 2 == 0 ? 0 : 5 * Mathf.Sign(lane);
            marker.Rect.anchoredPosition = Vector2.Lerp(marker.Rect.anchoredPosition,
                new Vector2(x, lane), 1 - Mathf.Exp(-20 * Time.unscaledDeltaTime));
            marker.Rect.localScale = Vector3.one * (active ? 1.15f : 1f);
            if (active) marker.Rect.SetAsLastSibling();
        }
    }
}
