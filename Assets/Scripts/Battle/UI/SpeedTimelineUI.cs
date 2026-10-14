using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>附着现有战斗 Canvas 的顶部速度条；头像从右侧0向左侧100移动。</summary>
public sealed class SpeedTimelineUI : MonoBehaviour
{
    private sealed class Marker
    {
        public RectTransform Rect;
        public UnityEngine.UI.Image Frame;
    }
    private BattleManager battle;
    private readonly Dictionary<PieceController, Marker> markers = new();
    private RectTransform track;

    public static SpeedTimelineUI Create(Canvas canvas, BattleManager manager)
    {
        var root = new GameObject("SpeedTimeline", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.25f, 1);
        rect.anchorMax = new Vector2(0.75f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -24);
        rect.sizeDelta = new Vector2(0, 104);
        var ui = root.AddComponent<SpeedTimelineUI>();
        ui.battle = manager;
        var background = root.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0.04f, 0.06f, 0.10f, 0.88f);
        background.raycastTarget = false;
        ui.track = ImageRect("Track", rect, new Vector2(0.5f, 0.5f), new Vector2(0, 3),
            new Color(0.45f, 0.55f, 0.65f, 0.7f)).rectTransform;
        ui.track.anchorMin = new Vector2(0, 0.5f);
        ui.track.anchorMax = new Vector2(1, 0.5f);
        ui.track.offsetMin = new Vector2(36, -1.5f);
        ui.track.offsetMax = new Vector2(-36, 1.5f);
        ImageRect("Ready", ui.track, new Vector2(0, 0.5f), new Vector2(4, 74), new Color(1, 0.8f, 0.3f));
        ImageRect("Start", ui.track, new Vector2(1, 0.5f), new Vector2(3, 74), new Color(0.5f, 0.6f, 0.7f));
        return ui;
    }

    private static UnityEngine.UI.Image ImageRect(string name, Transform parent, Vector2 anchor, Vector2 size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        obj.transform.SetParent(parent, false);
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.raycastTarget = false;
        image.color = color;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = anchor;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private void LateUpdate()
    {
        if (battle == null || !battle.SpeedTurnsActive)
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
                var frame = ImageRect(piece.name, track, new Vector2(1, 0.5f), new Vector2(42, 42), Color.white);
                var portrait = ImageRect("Portrait", frame.transform, new Vector2(0.5f, 0.5f), new Vector2(36, 36), Color.white);
                portrait.preserveAspect = true;
                portrait.sprite = piece.pieceData?.turnPortrait;
                if (portrait.sprite == null && piece.isPlayerPiece)
                {
                    int id = piece.pieceID;
                    portrait.sprite = Resources.Load<Sprite>(id > 100 ? "CG/PC0" + (id - 100) + "01" : "CG/PC0" + id + "C1");
                }
                if (portrait.sprite == null && piece.pieceDisplay != null && piece.pieceDisplay.pieceSpriteRenderer != null)
                    portrait.sprite = piece.pieceDisplay.pieceSpriteRenderer.sprite;
                marker = new Marker { Rect = frame.rectTransform, Frame = frame };
                markers.Add(piece, marker);
            }
            bool active = battle.ActivePiece == piece;
            marker.Frame.color = active ? new Color(1, 0.8f, 0.25f) :
                piece.isPlayerPiece ? new Color(0.3f, 0.75f, 1) : new Color(1, 0.35f, 0.35f);
            // 两条细分轨道避免同进度的敌我头像完全遮挡。
            float lane = piece.isPlayerPiece ? 23 : -23;
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
