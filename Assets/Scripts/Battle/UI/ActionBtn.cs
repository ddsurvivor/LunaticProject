using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>行动行的统一表现与输入；分类锁定独立于资源不足禁用。</summary>
public sealed class ActionBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public UnityEngine.UI.Text actionNameText;
    public UnityEngine.UI.Text costText;
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private RectTransform visual;
    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private UnityEngine.UI.Image background;
    [SerializeField] private UnityEngine.UI.Image outline;
    [SerializeField] private UnityEngine.UI.Image arrow;
    [SerializeField, Range(1f, 1.15f)] private float hoverScale = 1.06f;
    [SerializeField, Min(0)] private float hoverScaleDuration = 0.12f;
    private Func<bool> canUse;
    private Func<string> title;
    private Func<ActionResourceCost> queryCost;
    private Action clicked, entered, exited;
    private Sequence reveal;
    private Tween hoverTween;
    private float scaleTarget = 1f;
    private bool category;
    private bool expanded; // 父分类展开锁定，不等同于资源不足禁用。
    private bool available;
    private bool hovered;
    private bool pressed;
    private bool shown; // 完成本行出现动画后才接收输入。
    private int lastAP = -1;

    public bool IsHovered => hovered;
    public bool IsAvailable => available;
    public float HoverScaleMultiplier => hoverScale;

    private void Awake()
    {
        if (button == null) { enabled = false; return; }
        visual.pivot = new Vector2(0, 0.5f);
        visual.anchoredPosition = Vector2.zero;
        button.transition = UnityEngine.UI.Selectable.Transition.None;
        button.onClick.AddListener(HandleClick);
    }

    /// <summary>绑定展示与业务回调；不会直接扣费，复用前清除旧预览和动画。</summary>
    public void Bind(Func<string> label, bool isCategory, Func<bool> usable, Func<ActionResourceCost> cost,
        Action click, Action enter, Action exit)
    {
        ResetBinding();
        title = label;
        category = isCategory;
        canUse = usable;
        queryCost = cost;
        clicked = click;
        entered = enter;
        exited = exit;
        arrow.gameObject.SetActive(category);
        costText.gameObject.SetActive(!category);
        lastAP = -1;
        Refresh();
    }

    /// <summary>按本批次统一时间轴出现；动画只驱动视觉子节点，不干扰布局占位。</summary>
    public void Reveal(float delay, float duration)
    {
        reveal?.Kill();
        shown = false;
        visibility.alpha = 0;
        visibility.blocksRaycasts = false;
        visual.anchoredPosition = new Vector2(12, 0);
        reveal = DOTween.Sequence().SetUpdate(true);
        reveal.Insert(delay, visibility.DOFade(1, duration).SetEase(Ease.OutCubic));
        reveal.Insert(delay, visual.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.OutCubic));
        reveal.OnComplete(() =>
        {
            shown = true;
            visibility.blocksRaycasts = true;
            reveal = null;
        });
    }

    /// <summary>刷新可用性及双语标签；不可用时保留详情悬停，只关闭可操作反馈。</summary>
    public void Refresh()
    {
        if (title == null) return;
        string text = title();
        if (actionNameText.text != text) actionNameText.text = text;
        bool next = canUse != null && canUse();
        available = next;
        if (!available) pressed = false;
        button.interactable = available;
        if (!category && queryCost != null)
        {
            int ap = queryCost().AP;
            if (ap != lastAP) { costText.text = ap + " AP"; lastAP = ap; }
        }
        ApplyState();
    }

    /// <summary>展开父项保持选中并拒绝再次点击，不使用资源不足的灰色样式。</summary>
    public void SetExpanded(bool value)
    {
        if (value && hovered) ExitHover();
        expanded = value;
        pressed = false;
        arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, value ? -90 : 0);
        ApplyState();
    }

    /// <summary>清除复用对象的回调、预览、动画和显示状态。</summary>
    public void ResetBinding()
    {
        ExitHover();
        reveal?.Kill();
        reveal = null;
        hoverTween?.Kill();
        hoverTween = null;
        scaleTarget = 1f;
        title = null;
        canUse = null;
        queryCost = null;
        clicked = entered = exited = null;
        expanded = hovered = pressed = shown = false;
        if (visual != null)
        {
            visual.anchoredPosition = Vector2.zero;
            visual.localScale = Vector3.one;
        }
        if (visibility != null)
        {
            visibility.alpha = 1;
            visibility.blocksRaycasts = false;
        }
        if (arrow != null) arrow.rectTransform.localRotation = Quaternion.identity;
    }

    private bool Responds => shown && available && !expanded && isActiveAndEnabled;
    private void HandleClick() { if (Responds) clicked?.Invoke(); }
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 详情进入独立于资源可用性，禁用的技能和道具仍可阅读说明。
        if (!shown || expanded || !isActiveAndEnabled) return;
        hovered = true;
        ApplyState();
        entered?.Invoke();
    }
    public void OnPointerExit(PointerEventData eventData) => ExitHover();
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Responds || eventData.button != PointerEventData.InputButton.Left) return;
        pressed = true;
        ApplyState();
    }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; ApplyState(); }

    private void ExitHover()
    {
        bool wasHovered = hovered;
        hovered = pressed = false;
        if (wasHovered) exited?.Invoke();
        ApplyState();
    }

    private void ApplyState()
    {
        if (background == null) return;
        bool selected = expanded || pressed;
        background.color = selected ? GameConst.UIColors.SurfaceInset : hovered && available
            ? GameConst.UIColors.Surface : GameConst.UIColors.Background;
        outline.color = selected ? GameConst.UIColors.Accent : hovered && available
            ? GameConst.UIColors.Ally : available ? GameConst.UIColors.Border : GameConst.UIColors.Disabled;
        actionNameText.color = available ? GameConst.UIColors.Text : GameConst.UIColors.TextMuted;
        costText.color = available ? GameConst.UIColors.Ally : GameConst.UIColors.Disabled;
        arrow.color = expanded ? GameConst.UIColors.Accent : available ? GameConst.UIColors.Text : GameConst.UIColors.Disabled;
        float target = available && hovered && !expanded ? hoverScale : 1f;
        if (!Mathf.Approximately(scaleTarget, target))
        {
            scaleTarget = target;
            hoverTween?.Kill();
            hoverTween = visual.DOScale(target, hoverScaleDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }
    }

    private void OnDisable() => ResetBinding();
}
