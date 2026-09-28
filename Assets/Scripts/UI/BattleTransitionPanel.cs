using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>转场表现由此组件提供，所有加载协程统一由常驻 GM 执行。</summary>
public class BattleTransitionPanel : MonoBehaviour
{
    [Header("UI 元素引用")]
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private List<Sprite> images = new List<Sprite>();
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private UnityEngine.UI.Image progressBar;
    [SerializeField] private UnityEngine.UI.Image progressBar2;
    [SerializeField] private UnityEngine.UI.Text progressText;

    [Header("战斗转场配置")]
    [Min(0f)] [SerializeField] private float fadeDuration = 1f;
    [Min(0f)] [SerializeField] private float fakeLoadingDuration = 2f;

    private void Awake()
    {
        Hide();
    }

    // 保留已有 Inspector / 代码绑定，转交 GM 防止并行加载。
    public void TransitionToBattle(string sceneName)
    {
        GM.Ins.StartBattle(sceneName);
    }

    public void TransitionEndBattle(string sceneName, string endLog)
    {
        GM.Ins.ReturnFromBattle(sceneName, endLog);
    }

    public IEnumerator RunTransition(string sceneName, bool showLoading, float normalFadeDuration, Action onLoaded)
    {
        EnsureFadeUI();
        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = true;
        fadeCanvasGroup.alpha = 0f;
        if (loadingPanel != null) loadingPanel.SetActive(false);
        float duration = showLoading ? fadeDuration : normalFadeDuration;
        AsyncOperation operation = null;
        try
        {
            yield return Fade(1f, duration);
            if (showLoading)
            {
                if (backgroundImage != null && images != null && images.Count > 0)
                    backgroundImage.sprite = images[UnityEngine.Random.Range(0, images.Count)];
                if (loadingPanel != null) loadingPanel.SetActive(true);
                SetProgress(0f);
            }

            operation = TryLoadScene(sceneName);
            if (operation == null)
            {
                yield return Fade(0f, duration);
                yield break;
            }

            operation.allowSceneActivation = false;
            float elapsed = 0f;
            float minimumDuration = showLoading ? Mathf.Max(0f, fakeLoadingDuration) : 0f;
            while (elapsed < minimumDuration || operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (showLoading)
                {
                    float timeProgress = minimumDuration > 0f ? elapsed / minimumDuration : 1f;
                    SetProgress(Mathf.Min(timeProgress, operation.progress / 0.9f, 1f));
                }
                yield return null;
            }

            if (showLoading)
            {
                SetProgress(1f);
                yield return new WaitForSecondsRealtime(0.1f);
            }
            operation.allowSceneActivation = true;
            while (!operation.isDone) yield return null;
            // 让新场景 Start 完成，再初始化地图和剧情；全程仍有黑幕遮挡。
            yield return null;
            if (loadingPanel != null) loadingPanel.SetActive(false);
            onLoaded?.Invoke();
            yield return Fade(0f, duration);
        }
        finally
        {
            if (operation != null && !operation.isDone) operation.allowSceneActivation = true;
            Hide();
        }
    }

    private AsyncOperation TryLoadScene(string sceneName)
    {
        try
        {
            return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogError($"无法加载场景 {sceneName}：{exception.Message}", this);
            return null;
        }
    }

    private void SetProgress(float value)
    {
        if (progressBar != null) progressBar.fillAmount = value;
        if (progressBar2 != null) progressBar2.fillAmount = value;
        if (progressText != null) progressText.text = $"{value * 100f:F0}%";
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = fadeCanvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        fadeCanvasGroup.alpha = target;
    }

    private void EnsureFadeUI()
    {
        if (fadeCanvasGroup != null) return;
        // 没有预设战斗面板的场景也能使用普通黑屏转场。
        var canvasObject = new GameObject("SceneTransitionCanvas", typeof(RectTransform),
            typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var black = new GameObject("Black", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
        black.transform.SetParent(canvasObject.transform, false);
        var rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.GetComponent<UnityEngine.UI.Image>().color = Color.black;
        fadeCanvasGroup = black.GetComponent<CanvasGroup>();
    }

    private void Hide()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
