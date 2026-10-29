using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>以实心和空心菱形展示行动力，按实际容量扩充点数并在行内等距排布。</summary>
public sealed class ActionPointPipsUI : MonoBehaviour
{
    [SerializeField] private List<UnityEngine.UI.Image> points = new();
    [SerializeField] private Sprite filledSprite;
    [SerializeField] private Sprite emptySprite;
    private int previewCount;
    private int lastCurrent = -1;
    private int lastMaximum = -1;
    private readonly List<UnityEngine.UI.Image> spentEffects = new();

    /// <summary>刷新当前可用点和容量；负容量按零处理，超出容量的当前值截断。</summary>
    public void SetPoints(int current, int maximum)
    {
        maximum = Mathf.Max(0, maximum);
        current = Mathf.Clamp(current, 0, maximum);
        if (current == lastCurrent && maximum == lastMaximum) return;
        if (points.Count == 0 || points[0] == null) return;
        if (maximum == lastMaximum && current < lastCurrent)
            for (int i = current; i < lastCurrent; i++) AnimateSpentPoint(points[i]);
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

    private void AnimateSpentPoint(UnityEngine.UI.Image source)
    {
        // 使用独立实心图层，原点立即显示真实剩余状态，呼吸刷新不会覆盖消耗动画。
        var effect = Instantiate(source, transform, false);
        effect.name = "SpentAP";
        effect.sprite = filledSprite;
        effect.color = GameConst.UIColors.Ally;
        effect.raycastTarget = false;
        effect.rectTransform.localScale = Vector3.one;
        spentEffects.Add(effect);
        DOTween.Sequence().SetUpdate(true).SetTarget(effect)
            .Append(effect.rectTransform.DOScale(2f, 1f).SetEase(Ease.OutCubic))
            .Join(effect.DOFade(0, 1f).SetEase(Ease.Linear))
            .OnComplete(() => { spentEffects.Remove(effect); if (effect != null) Destroy(effect.gameObject); });
    }

    /// <summary>切换数据源或停用时清除消耗动画与数值快照，防止新角色被误判为消耗了 AP。</summary>
    public void ResetPresentation()
    {
        foreach (var effect in spentEffects)
        {
            if (effect == null) continue;
            DOTween.Kill(effect);
            Destroy(effect.gameObject);
        }
        spentEffects.Clear();
        SetPreview(0);
        lastCurrent = lastMaximum = -1;
    }
    /// <summary>仅预览当前可用点中最右侧的费用点，零费用恢复全部图标。</summary>
    public void SetPreview(int count)
    {
        previewCount = Mathf.Max(0, count);
        ApplyPreview();
    }

    private void LateUpdate() { if (previewCount > 0) ApplyPreview(); }

    private void ApplyPreview()
    {
        float alpha = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2));
        int first = Mathf.Max(0, lastCurrent - previewCount);
        for (int i = 0; i < points.Count && i < lastMaximum; i++)
        {
            Color color = i < lastCurrent ? GameConst.UIColors.Ally : GameConst.UIColors.Border;
            if (previewCount > 0 && i >= first && i < lastCurrent) color.a = alpha;
            points[i].color = color;
        }
    }

    private void OnDisable() => ResetPresentation();

}
