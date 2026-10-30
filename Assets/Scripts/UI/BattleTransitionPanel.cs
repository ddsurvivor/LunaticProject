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
    [Header("加载提示")]
    [SerializeField] private LoadingTipCatalogSO tipCatalog;
    [Min(0.1f)] [SerializeField] private float tipRotationInterval = 6f;
    [Min(0f)] [SerializeField] private float tipClickInterval = 0.5f;
    private UnityEngine.UI.Text tipText;
    private readonly List<LoadingTipCatalogSO.Entry> availableTips = new List<LoadingTipCatalogSO.Entry>();
    private readonly List<string> tipPages = new List<string>();
    private bool tipsActive; // 仅加载面板显示期间处理输入和非缩放计时。
    private int tipIndex, tipPageIndex;
    private float nextTipAt, nextTipClickAt;

    [Header("战斗转场配置")]
    [Min(0f)] [SerializeField] private float fadeDuration = 1f;
    [Min(0f)] [SerializeField] private float fakeLoadingDuration = 2f;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (!tipsActive || loadingPanel == null || !loadingPanel.activeInHierarchy) return;
        TickTips(Time.unscaledTime, Input.GetMouseButtonDown(0));
    }

    private void TickTips(float now, bool clicked)
    {
        if (!tipsActive) return;
        if (clicked && now >= nextTipClickAt)
        {
            nextTipClickAt = now + Mathf.Max(0f, tipClickInterval);
            NextTip(now);
        }
        else if (now >= nextTipAt) NextTip(now);
    }

    private void OnDisable() => Hide();

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
        if (showLoading && loadingPanel == null) CreateFallbackLoadingPanel();
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
                BeginTips();
                SetProgress(0f);
            }

            operation = TryLoadScene(sceneName);
            if (operation == null)
            {
                StopTips();
                if (loadingPanel != null) loadingPanel.SetActive(false);
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
            StopTips();
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

    private void BeginTips()
    {
        StopTips();
        if (tipCatalog == null) tipCatalog = Resources.Load<LoadingTipCatalogSO>("UI/LoadingTipCatalog");
        if (tipCatalog != null && tipCatalog.tips != null)
            foreach (var entry in tipCatalog.tips)
                if (entry != null && !string.IsNullOrWhiteSpace(entry.body)) availableTips.Add(entry);
        if (loadingPanel == null || availableTips.Count == 0) return;
        EnsureTipUI();
        tipText.transform.parent.gameObject.SetActive(true);
        tipsActive = true;
        tipIndex = 0;
        nextTipClickAt = Time.unscaledTime;
        BuildTipPages();
        ShowTip(Time.unscaledTime);
    }

    private void NextTip(float now)
    {
        if (availableTips.Count == 0) return;
        if (++tipPageIndex >= tipPages.Count)
        {
            tipIndex = (tipIndex + 1) % availableTips.Count;
            BuildTipPages();
        }
        ShowTip(now);
    }

    private void ShowTip(float now)
    {
        tipText.text = tipPages[tipPageIndex];
        tipText.transform.parent.gameObject.SetActive(true);
        nextTipAt = now + Mathf.Max(0.1f, tipRotationInterval);
    }

    private void BuildTipPages()
    {
        tipPages.Clear();
        tipPageIndex = 0;
        var tip = availableTips[tipIndex];
        string value = string.IsNullOrWhiteSpace(tip.title) ? tip.body : tip.title + "\n" + tip.body;
        Canvas.ForceUpdateCanvases();
        var size = tipText.rectTransform.rect.size;
        var settings = tipText.GetGenerationSettings(size);
        settings.fontSize = 18;
        settings.resizeTextForBestFit = false;
        var generator = new TextGenerator();
        // 对超出区域的内容分页，避免自动缩放到最小字号后丢失正文。
        while (value.Length > 0)
        {
            int low = 1, high = value.Length, count = 1;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                if (generator.GetPreferredHeight(value.Substring(0, mid), settings) <= Mathf.Max(1f, size.y))
                {
                    count = mid;
                    low = mid + 1;
                }
                else high = mid - 1;
            }
            if (count < value.Length && char.IsHighSurrogate(value[count - 1]) && char.IsLowSurrogate(value[count])) count++;
            tipPages.Add(value.Substring(0, count));
            value = value.Substring(count);
        }
    }

    private void StopTips()
    {
        tipsActive = false;
        availableTips.Clear();
        tipPages.Clear();
        if (tipText != null) tipText.transform.parent.gameObject.SetActive(false);
    }

    private void EnsureTipUI()
    {
        if (tipText != null) return;
        // 提示区随加载面板销毁，并用相对锚点适配现有的不同分辨率。
        var panel = new GameObject("LoadingTips", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        panel.transform.SetParent(loadingPanel.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.16f, 0.70f);
        rect.anchorMax = new Vector2(0.84f, 0.87f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        background.color = new Color(0f, 0f, 0f, 0.75f);
        background.raycastTarget = false;
        var text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        text.transform.SetParent(panel.transform, false);
        tipText = text.GetComponent<UnityEngine.UI.Text>();
        tipText.font = progressText != null && progressText.font != null ? progressText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        tipText.color = Color.white;
        tipText.alignment = TextAnchor.MiddleCenter;
        tipText.fontSize = 26;
        tipText.resizeTextForBestFit = true;
        tipText.resizeTextMinSize = 14;
        tipText.resizeTextMaxSize = 26;
        tipText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tipText.verticalOverflow = VerticalWrapMode.Truncate;
        tipText.supportRichText = false;
        tipText.raycastTarget = false;
        var textRect = tipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18, 8);
        textRect.offsetMax = new Vector2(-18, -8);
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

    private void CreateFallbackLoadingPanel()
    {
        loadingPanel = new GameObject("LoadingPanel", typeof(RectTransform));
        loadingPanel.transform.SetParent(fadeCanvasGroup.transform, false);
        var rect = (RectTransform)loadingPanel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void Hide()
    {
        StopTips();
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}
