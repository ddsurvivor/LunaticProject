using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class NotificationItem : MonoBehaviour
{
    [SerializeField]private CanvasGroup canvasGroup;
    [SerializeField]private ItemGetPanel panel;
    private System.Action<NotificationItem> onCompleteCallback;

    // 初始化并开始播放动画（针对 ItemPack）
    public void Initialize(ItemPack itemPack, System.Action<NotificationItem> onComplete)
    {
        //canvasGroup = GetComponent<CanvasGroup>();
        //panel = GetComponent<ItemGetPanel>();
        panel.ShowPanel(itemPack);
        StartSequence(onComplete);
    }

    // 初始化并开始播放动画（针对 ComponentData）
    public void Initialize(ComponentData componentData, System.Action<NotificationItem> onComplete)
    {
        panel.ShowPanel(componentData);
        StartSequence(onComplete);
    }

    private void StartSequence(System.Action<NotificationItem> onComplete)
    {
        onCompleteCallback = onComplete;
        gameObject.SetActive(true);
        StartCoroutine(AnimateRoutine());
    }

    private IEnumerator AnimateRoutine()
    {
        // 1. 淡入 (0.2秒)
        canvasGroup.alpha = 0;
        float elapsed = 0;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = 1;

        // 2. 停留展示 (1.5秒)
        yield return new WaitForSeconds(1.5f);

        // 3. 淡出 (0.3秒)
        elapsed = 0;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / 0.3f);
            yield return null;
        }

        // 4. 动画结束，触发回收
        gameObject.SetActive(false);
        onCompleteCallback?.Invoke(this);
    }
}