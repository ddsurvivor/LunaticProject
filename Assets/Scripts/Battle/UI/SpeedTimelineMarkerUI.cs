using UnityEngine;
using Sirenix.OdinInspector;

[System.Serializable]
public sealed class TurnPortraitFraming
{
    [LabelText("覆盖默认构图")] public bool useOverride;
    [LabelText("使用整图区域（含留白）"), ShowIf(nameof(useOverride))] public bool useFullSpriteRect; // 显式放弃 Tight 网格边界，默认保持自动取景。
    [LabelText("有效区域（全图值为自动）"), ShowIf(nameof(ShowContentRect))] public Rect contentRect = new(0, 0, 1, 1); // 旧配置的非全图区域继续生效，不必重新勾选。
    [LabelText("缩放"), Min(0.1f), ShowIf(nameof(useOverride))] public float zoom = 1f;
    [LabelText("位移"), ShowIf(nameof(useOverride))] public Vector2 offset; // 相对于蒙版宽高，正值向右、向上。
    [LabelText("上半身高度占比"), Range(0.1f, 1f), ShowIf(nameof(useOverride))] public float upperBodyFraction = 0.42f;
    private bool ShowContentRect => useOverride && !useFullSpriteRect;
}

public sealed class SpeedTimelineMarkerUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private RectTransform portraitViewport;
    [SerializeField] private UnityEngine.UI.Text missingPortrait;
    [SerializeField] private UnityEngine.UI.Image factionAccent;
    [SerializeField] private GameObject activeHighlight; // 仅当前行动角色显示的强调框。
    public RectTransform Rect => (RectTransform)transform;
    public UnityEngine.UI.Image Portrait => portrait;
    private Sprite boundSprite;
    private Vector2 viewportSize;
    private Rect contentBounds;
    private bool fullBody;
    private TurnPortraitFraming framing;
    private bool cachedOverride;
    private bool cachedFullSpriteRect;
    private Rect cachedContentRect;
    private float cachedZoom, cachedFraction;
    private Vector2 cachedOffset;
    private UnityEngine.UI.Text overflowText; // 满条后的额外进度，避免挤压布局隐藏真实数值。
    private static readonly System.Collections.Generic.Dictionary<Sprite, Rect> boundsCache = new();

    /// <summary>供预制体生成器绑定头像、独立裁切窗口、缺图提示和蒙版外的状态装饰。</summary>
    public void Configure(UnityEngine.UI.Image face, UnityEngine.UI.Image accent, GameObject highlight,
        RectTransform viewport, UnityEngine.UI.Text fallback)
    {
        portrait = face;
        factionAccent = accent;
        activeHighlight = highlight;
        portraitViewport = viewport;
        missingPortrait = fallback;
    }

    /// <summary>绑定速度条专用头像或全身立绘；仅调整 UI 构图，不修改战场 Sprite 的 pivot。</summary>
    public void BindPortrait(Sprite sprite, bool isFullBody, TurnPortraitFraming settings)
    {
        boundSprite = sprite;
        fullBody = isFullBody;
        framing = settings;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        if (missingPortrait != null) missingPortrait.gameObject.SetActive(sprite == null);
        portrait.color = Color.white;
        portrait.raycastTarget = false;
        portrait.preserveAspect = false;
        RefreshFraming();
    }

    private void LateUpdate()
    {
        bool custom = framing != null && framing.useOverride;
        bool changed = cachedOverride != custom || custom && (cachedFullSpriteRect != framing.useFullSpriteRect || cachedContentRect != framing.contentRect
            || cachedZoom != framing.zoom || cachedFraction != framing.upperBodyFraction || cachedOffset != framing.offset);
        if (portraitViewport != null && (portraitViewport.rect.size != viewportSize || changed)) RefreshFraming();
    }

    private void RefreshFraming()
    {
        if (portraitViewport == null) return;
        viewportSize = portraitViewport.rect.size;
        bool custom = framing != null && framing.useOverride;
        cachedOverride = custom;
        if (custom)
        {
            cachedFullSpriteRect = framing.useFullSpriteRect;
            cachedContentRect = framing.contentRect;
            cachedZoom = framing.zoom;
            cachedFraction = framing.upperBodyFraction;
            cachedOffset = framing.offset;
        }
        // 默认全图值作为自动区域的占位；旧的非默认区域保留，只有显式整图选项才包含全部留白。
        Rect fullRect = new(0, 0, 1, 1);
        contentBounds = custom && framing.useFullSpriteRect ? fullRect
            : custom && framing.contentRect != fullRect ? ClampBounds(framing.contentRect) : GetContentBounds(boundSprite);
        if (boundSprite == null) return;
        Vector2 size = boundSprite.rect.size;
        Vector2 contentSize = Vector2.Scale(size, contentBounds.size);
        float height = contentSize.y * (fullBody ? (custom ? Mathf.Clamp(framing.upperBodyFraction, 0.1f, 1f) : 0.42f) : 1f);
        // Image 使用居中的矩形坐标；Sprite pivot 只用于还原网格有效范围，不能直接当作 UI 锚点。
        float scale = fullBody ? viewportSize.y / Mathf.Max(1, height)
            : Mathf.Min(viewportSize.x / Mathf.Max(1, contentSize.x), viewportSize.y / Mathf.Max(1, height));
        scale *= custom ? Mathf.Max(0.1f, framing.zoom) : 1f;
        Vector2 center = new(size.x * contentBounds.center.x,
            size.y * contentBounds.yMax - height * 0.5f);
        var imageRect = portrait.rectTransform;
        imageRect.anchorMin = imageRect.anchorMax = imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.sizeDelta = size * scale;
        imageRect.anchoredPosition = (size * 0.5f - center) * scale
            + (custom ? Vector2.Scale(viewportSize, framing.offset) : Vector2.zero);
    }

    private static Rect ClampBounds(Rect value)
    {
        float x = Mathf.Clamp(value.xMin, 0, 0.99f), y = Mathf.Clamp(value.yMin, 0, 0.99f);
        return UnityEngine.Rect.MinMaxRect(x, y, Mathf.Clamp(value.xMax, x + 0.01f, 1), Mathf.Clamp(value.yMax, y + 0.01f, 1));
    }

    private static Rect GetContentBounds(Sprite sprite)
    {
        if (sprite == null) return new Rect(0, 0, 1, 1);
        if (boundsCache.TryGetValue(sprite, out var bounds)) return bounds;
        var vertices = sprite.vertices;
        Vector2 min = Vector2.one, max = Vector2.zero;
        // Tight 网格可排除透明边缘；Full Rect 资源通过角色构图配置补偿，不要求纹理开启 Read/Write。
        foreach (var vertex in vertices)
        {
            Vector2 pixel = vertex * sprite.pixelsPerUnit + sprite.pivot;
            Vector2 normalized = new(pixel.x / sprite.rect.width, pixel.y / sprite.rect.height);
            min = Vector2.Min(min, normalized);
            max = Vector2.Max(max, normalized);
        }
        bounds = vertices.Length > 0 ? ClampBounds(UnityEngine.Rect.MinMaxRect(min.x, min.y, max.x, max.y)) : new Rect(0, 0, 1, 1);
        boundsCache[sprite] = bounds;
        return bounds;
    }

    /// <summary>按阵营设置色条，并切换当前行动角色的高亮表现。</summary>
    public void SetState(bool active, bool ally)
    {
        factionAccent.color = ally ? new Color(0.38f, 0.75f, 0.85f) : new Color(0.9f, 0.4f, 0.36f);
        activeHighlight.SetActive(active);
    }

    /// <summary>显示超过满条的真实进度；当前行动者按调度器提供的显示进度处理。</summary>
    public void SetOverflow(float progress)
    {
        if (progress <= 100f)
        {
            if (overflowText != null) overflowText.gameObject.SetActive(false);
            return;
        }
        if (overflowText == null)
        {
            var label = new GameObject("OverflowProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
            label.transform.SetParent(transform, false);
            overflowText = label.GetComponent<UnityEngine.UI.Text>();
            overflowText.font = missingPortrait != null ? missingPortrait.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            overflowText.fontSize = 14;
            overflowText.fontStyle = FontStyle.Bold;
            overflowText.alignment = TextAnchor.MiddleCenter;
            overflowText.color = Color.white;
            overflowText.raycastTarget = false;
            var rect = overflowText.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(64, 20);
            rect.anchoredPosition = new Vector2(0, 22);
            var shadow = label.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1, -1);
        }
        overflowText.gameObject.SetActive(true);
        overflowText.text = progress.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }
}
