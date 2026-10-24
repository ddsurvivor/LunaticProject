using System.Collections.Generic;
using UnityEngine;

/// <summary>以实心和空心菱形展示行动力，按实际容量扩充点数并在行内等距排布。</summary>
public sealed class ActionPointPipsUI : MonoBehaviour
{
    [SerializeField] private List<UnityEngine.UI.Image> points = new();
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private Sprite emptySprite;
    private int lastCurrent = -1;
    private int lastMaximum = -1;

    /// <summary>刷新当前可用点和容量；负容量按零处理，超出容量的当前值截断。</summary>
    public void SetPoints(int current, int maximum)
    {
        maximum = Mathf.Max(0, maximum);
        current = Mathf.Clamp(current, 0, maximum);
        if (current == lastCurrent && maximum == lastMaximum) return;
        if (points.Count == 0 || points[0] == null) return;
        while (points.Count < maximum)
            points.Add(Instantiate(points[0], transform, false));

        float width = ((RectTransform)transform).rect.width;
        float step = Mathf.Min(27f, width / Mathf.Max(1, maximum));
        float size = Mathf.Min(22f, step * 0.82f);
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            point.gameObject.SetActive(i < maximum);
            if (i >= maximum) continue;
            point.sprite = i < current ? filledSprite : emptySprite;
            point.color = i < current ? GameConst.UIColors.Ally : GameConst.UIColors.Border;
            point.rectTransform.anchorMin = point.rectTransform.anchorMax = new Vector2(0, 0.5f);
            point.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            point.rectTransform.anchoredPosition = new Vector2(step * (i + 0.5f), 0);
            point.rectTransform.sizeDelta = Vector2.one * size;
        }
        lastCurrent = current;
        lastMaximum = maximum;
    }
}
