using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class 剧本System
{
    [Header("阅读器鼠标输入")]
    [SerializeField] private bool enableGlobalReaderInput;
    [Tooltip("手动点击推进的最小间隔（秒）；0 表示不限制。按钮操作和自动播放不受影响。")]
    [SerializeField, Min(0f)] private float manualAdvanceInterval = 1f;

    private float lastManualAdvanceTime = float.NegativeInfinity;
    private readonly List<RaycastResult> readerPointerHits = new List<RaycastResult>();

    public float ManualAdvanceInterval => manualAdvanceInterval;

    public void SetManualAdvanceInterval(float seconds)
    {
        manualAdvanceInterval = Mathf.Max(0f, seconds);
    }

    /// <summary>手动推进入口。Next 仍供自动播放和剧本内部指令使用。</summary>
    public bool TryAdvanceByClick()
    {
        if (!isActiveAndEnabled || IsReaderHiding || isWaitingForChoice || 已储存剧本 == null ||
            已阅读 >= 已储存剧本.Length ||
            Time.unscaledTime - lastManualAdvanceTime < manualAdvanceInterval)
            return false;

        lastManualAdvanceTime = Time.unscaledTime;
        Next();
        return true;
    }

    // 在鼠标按下时判断 UI，避免选项或关闭设置的释放事件额外推进一句。
    private bool UpdateReaderInput()
    {
        if (!enableGlobalReaderInput) return false;
        bool clicked = Input.GetMouseButtonDown(0);
        Vector2 wheel = Input.mouseScrollDelta;
        if (!clicked && wheel.sqrMagnitude == 0f) return false;

        EventSystem events = EventSystem.current;
        if (events == null) return false;
        var pointer = new PointerEventData(events)
        {
            position = Input.mousePosition,
            scrollDelta = wheel,
            button = PointerEventData.InputButton.Left
        };
        readerPointerHits.Clear();
        events.RaycastAll(pointer, readerPointerHits);
        GameObject hit = readerPointerHits.Count > 0 ? readerPointerHits[0].gameObject : null;

        // 设置/存档等覆盖面板的空白区域也拦截，不只排除按钮本身。
        if (hit != null && !hit.transform.IsChildOf(transform)) return false;

        if (wheel.sqrMagnitude > 0f && 进度条 != null && 进度条.isActiveAndEnabled)
        {
            // 区域内由 EventSystem 正常派发；区域外才补发，防止一格滚轮移动两次。
            GameObject nativeScrollTarget = hit == null ? null :
                ExecuteEvents.GetEventHandler<IScrollHandler>(hit);
            if (nativeScrollTarget == null) 进度条.OnScroll(pointer);
        }

        if (!clicked) return false;
        if (hit != null && (hit.GetComponentInParent<Selectable>() != null ||
                            ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) != null ||
                            ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit) != null))
            return false;

        return TryAdvanceByClick();
    }
}
