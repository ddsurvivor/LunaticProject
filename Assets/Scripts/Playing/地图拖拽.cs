using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class 地图拖拽 : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [SerializeField, LabelText("地图底图")]
    [Tooltip("绑定当前地图下的底图 Image；未绑定时使用脚本所在物体的矩形。底图应为无旋转的完整矩形。")]
    private UnityEngine.UI.Image 地图底图;
    [SerializeField, LabelText("启用滚轮缩放")]
    private bool 启用滚轮缩放 = true;
    [SerializeField, Min(0.01f), LabelText("滚轮缩放速度")]
    private float 滚轮缩放速度 = 0.15f;
    [SerializeField, Min(0.01f), LabelText("最小缩放")]
    private float 最小缩放 = 1f;
    [SerializeField, Min(0.01f), LabelText("最大缩放")]
    private float 最大缩放 = 3f;

    private RectTransform 限制范围;
    private RectTransform 目标;
    private readonly Vector3[] 四角 = new Vector3[4];

    private bool 初始化()
    {
        if (目标 == null) 目标 = GetComponent<RectTransform>();
        限制范围 = 目标.parent as RectTransform;
        return 限制范围 != null;
    }

    private void Awake() { 初始化(); }

    // 也处理窗口尺寸变化、地图切换以及剧情动画结束后的缩放重置。
    private void LateUpdate()
    {
        if (!大地图System.是可以点击地图事件 || !初始化()) return;
        保证覆盖范围();
        目标.anchoredPosition = 范围限制(目标.anchoredPosition);
    }

    //[Button("根据底图适配外层尺寸")]
    private void 根据底图适配尺寸()
    {
        if (!初始化() || 地图底图 == null || 地图底图.transform == 目标 ||
            !地图底图.transform.IsChildOf(目标))
        {
            Debug.LogWarning("请绑定脚本所在地图物体下面的底图 Image，并确保地图父物体为 RectTransform。", this);
            return;
        }
        Canvas.ForceUpdateCanvases();
        Bounds bounds = 获取边界(目标);
        if (bounds.size.x <= 0f || bounds.size.y <= 0f) return;

        // 固定直接子物体的实际尺寸和位置，避免外层改尺寸导致拉伸锚点的底图再次变大。
        var children = new RectTransform[目标.childCount];
        var sizes = new Vector2[children.Length];
        var positions = new Vector3[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            children[i] = 目标.GetChild(i) as RectTransform;
            if (children[i] == null) continue;
            sizes[i] = children[i].rect.size;
            positions[i] = children[i].localPosition;
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "根据底图适配地图尺寸");
        }
#endif
        // 将外层矩形边缘对齐底图；不移动地图内容。
        Vector2 pivot = new Vector2(-bounds.min.x / bounds.size.x, -bounds.min.y / bounds.size.y);
        Vector3 position = 目标.localPosition;
        目标.pivot = pivot;
        目标.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bounds.size.x);
        目标.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bounds.size.y);
        目标.localPosition = position;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null) continue;
            children[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sizes[i].x);
            children[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sizes[i].y);
            children[i].localPosition = positions[i];
        }
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(目标);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(目标);
            foreach (var child in children)
            {
                if (child == null) continue;
                UnityEditor.EditorUtility.SetDirty(child);
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(child);
            }
        }
#endif
        Debug.Log($"地图适配尺寸：{bounds.size.x:F1} × {bounds.size.y:F1}（地图本地单位）", this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        初始化();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!大地图System.是可以点击地图事件 || !初始化()) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(限制范围,
                eventData.position, eventData.pressEventCamera, out var current) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(限制范围,
                eventData.position - eventData.delta, eventData.pressEventCamera, out var previous))
        {
            目标.anchoredPosition = 范围限制(目标.anchoredPosition + current - previous);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!启用滚轮缩放 || !大地图System.是可以点击地图事件 || !初始化()) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(限制范围, eventData.position, eventData.enterEventCamera)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(目标,
                eventData.position, eventData.enterEventCamera, out var point)) return;

        Vector3 before = 限制范围.InverseTransformPoint(目标.TransformPoint(point));
        float minimum = 获取最小缩放();
        float scale = Mathf.Clamp(目标.localScale.x * Mathf.Exp(eventData.scrollDelta.y * 滚轮缩放速度),
            minimum, Mathf.Max(minimum, 最大缩放));
        目标.localScale = new Vector3(scale, scale, 目标.localScale.z);
        Vector3 after = 限制范围.InverseTransformPoint(目标.TransformPoint(point));
        目标.anchoredPosition = 范围限制(目标.anchoredPosition + (Vector2)(before - after));
    }

    private float 获取最小缩放()
    {
        Bounds bounds = 获取边界(限制范围);
        if (bounds.size.x <= 0.0001f || bounds.size.y <= 0.0001f) return Mathf.Max(0.01f, 最小缩放);
        float cover = Mathf.Max(限制范围.rect.width / bounds.size.x * Mathf.Abs(目标.localScale.x), 限制范围.rect.height / bounds.size.y * Mathf.Abs(目标.localScale.y));
        return Mathf.Max(0.01f, 最小缩放, cover);
    }

    private void 保证覆盖范围()
    {
        float minimum = 获取最小缩放();
        if (目标.localScale.x > 0f && 目标.localScale.y > 0f)
        {
            // 等比例放大，保留已有非均匀缩放配置。
            float factor = Mathf.Max(1f, minimum / 目标.localScale.x, minimum / 目标.localScale.y);
            if (factor > 1.00001f)
                目标.localScale = new Vector3(目标.localScale.x * factor, 目标.localScale.y * factor, 目标.localScale.z);
        }
    }

    private Vector2 范围限制(Vector2 position)
    {
        Bounds bounds = 获取边界(限制范围);
        Vector2 delta = position - 目标.anchoredPosition;
        Rect viewport = 限制范围.rect;
        position.x += 轴修正(bounds.min.x + delta.x, bounds.max.x + delta.x, viewport.xMin, viewport.xMax);
        position.y += 轴修正(bounds.min.y + delta.y, bounds.max.y + delta.y, viewport.yMin, viewport.yMax);
        return position;
    }

    private static float 轴修正(float min, float max, float viewMin, float viewMax)
    {
        if (max - min < viewMax - viewMin) return (viewMin + viewMax - min - max) * 0.5f;
        if (min > viewMin) return viewMin - min;
        if (max < viewMax) return viewMax - max;
        return 0f;
    }

    private Bounds 获取边界(RectTransform space)
    {
        bool useImage = 地图底图 != null && 地图底图.transform.IsChildOf(目标);
        RectTransform source = useImage ? 地图底图.rectTransform : 目标;
        Rect rect = source.rect;
        // Simple Image 开启 Preserve Aspect 时，矩形留白不能算作地图覆盖区域。
        Sprite sprite = useImage ? (地图底图.overrideSprite != null ? 地图底图.overrideSprite : 地图底图.sprite) : null;
        if (useImage && 地图底图.preserveAspect && 地图底图.type == UnityEngine.UI.Image.Type.Simple && sprite != null && sprite.rect.height > 0f)
        {
            float aspect = sprite.rect.width / sprite.rect.height;
            Vector2 size = rect.size;
            if (aspect > size.x / size.y) size.y = size.x / aspect;
            else size.x = size.y * aspect;
            rect = new Rect(rect.position + Vector2.Scale(rect.size - size, source.pivot), size);
        }
        四角[0] = new Vector3(rect.xMin, rect.yMin);
        四角[1] = new Vector3(rect.xMin, rect.yMax);
        四角[2] = new Vector3(rect.xMax, rect.yMax);
        四角[3] = new Vector3(rect.xMax, rect.yMin);
        var bounds = new Bounds(space.InverseTransformPoint(source.TransformPoint(四角[0])), Vector3.zero);
        for (int i = 1; i < 四角.Length; i++) bounds.Encapsulate(space.InverseTransformPoint(source.TransformPoint(四角[i])));
        return bounds;
    }
}
