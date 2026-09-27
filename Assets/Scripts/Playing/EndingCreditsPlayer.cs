using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>
/// 片尾字幕：从视口底部滚动到顶部，支持图片/视频背景、加速和调试跳过。
/// </summary>
public class EndingCreditsPlayer : MonoBehaviour
{
    public enum BackgroundMode { None, Image, Video }

    [Header("字幕")]
    [SerializeField] private RectTransform viewport;
    [SerializeField] private UnityEngine.UI.Text creditsText;
    [Tooltip("可选，优先使用该文本文件的内容")]
    [SerializeField] private TextAsset textAsset;
    [TextArea(8, 30)]
    [SerializeField] private string credits = "制作人员\n\n感谢游玩";
    [Min(1f)] [SerializeField] private float scrollSpeed = 60f;
    [Min(0f)] [SerializeField] private float edgePadding = 40f;
    [SerializeField] private bool playOnEnable = true;

    [Header("加速")]
    [SerializeField] private bool enableFastForward = true;
    [SerializeField] private KeyCode fastForwardKey = KeyCode.LeftShift;
    [Min(1f)] [SerializeField] private float fastForwardMultiplier = 4f;

    [Header("调试跳过")]
    [SerializeField] private bool enableSkipKey = true;
    [Tooltip("仅在编辑器和 Development Build 中响应跳过按键")]
    [SerializeField] private bool debugBuildOnly = true;
    [SerializeField] private KeyCode skipKey = KeyCode.F10;

    [Header("背景")]
    [SerializeField] private BackgroundMode backgroundMode = BackgroundMode.Image;
    [SerializeField] private UnityEngine.UI.Image backgroundImage;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private UnityEngine.UI.RawImage videoImage;
    [Tooltip("使用专属 VideoPlayer；RenderTexture 在播放时自动创建和释放")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool loopVideo = true;

    [Header("播放事件")]
    [Tooltip("正常结束或跳过均触发；参数为是否跳过")]
    [SerializeField] private UnityEvent<bool> onFinished = new UnityEvent<bool>();

    public bool IsPlaying { get; private set; }
    public UnityEvent OnFinished;

    private RectTransform textRect;
    private RenderTexture videoTexture;
    private VideoPlayer activeVideoPlayer;
    private bool fastForwardRequested;
    private bool starting;

    private void OnEnable()
    {
        if (playOnEnable && !starting) Play();
    }

    /// <summary>从头播放，也可绑定到 UnityEvent。隐藏的面板会自动显示。</summary>
    public void Play()
    {
        if (starting) return;
        starting = true;
        gameObject.SetActive(true);
        starting = false;
        Stop();

        if (!isActiveAndEnabled) return;
        if (viewport == null || creditsText == null ||
            creditsText.transform.parent != viewport || creditsText.font == null)
        {
            Debug.LogError("片尾字幕需要 Viewport、带字体的 UGUI Text，且 Text 必须是 Viewport 的直接子物体。", this);
            return;
        }

        textRect = creditsText.rectTransform;
        creditsText.text = textAsset != null ? textAsset.text : credits;
        creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        creditsText.verticalOverflow = VerticalWrapMode.Overflow;
        creditsText.resizeTextForBestFit = false;
        creditsText.alignment = TextAnchor.UpperCenter;
        creditsText.raycastTarget = false;

        // 水平方向保留美术设置的边距，垂直方向统一按文本顶部定位。
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        Canvas.ForceUpdateCanvases();
        ResizeText();
        Vector2 position = textRect.anchoredPosition;
        position.y = -edgePadding;
        textRect.anchoredPosition = position;

        IsPlaying = true;
        StartBackground();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        if (enableSkipKey && (!debugBuildOnly || Application.isEditor || Debug.isDebugBuild)
            && Input.GetKeyDown(skipKey))
        {
            Skip();
            return;
        }

        // 重新计算高度以适配分辨率或 Canvas 布局变化。
        ResizeText();
        bool accelerated = enableFastForward &&
            (fastForwardRequested || Input.GetKey(fastForwardKey));
        float speed = Mathf.Max(1f, scrollSpeed) *
            (accelerated ? Mathf.Max(1f, fastForwardMultiplier) : 1f);
        textRect.anchoredPosition += Vector2.up * (speed * Time.unscaledDeltaTime);

        // 文本底部完全越过视口顶部后结束。
        if (textRect.anchoredPosition.y - textRect.rect.height >= viewport.rect.height + edgePadding)
            Finish(false);
    }

    private void ResizeText()
    {
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(1f, creditsText.preferredHeight));
    }

    /// <summary>供屏幕按钮的 PointerDown/PointerUp 设置加速状态。</summary>
    public void SetFastForward(bool value)
    {
        fastForwardRequested = value;
    }

    /// <summary>替换字幕内容；下次 Play 生效，清除文本文件优先级。</summary>
    public void SetCredits(string value)
    {
        textAsset = null;
        credits = value ?? string.Empty;
    }

    /// <summary>主动跳过；按键的调试限制不影响此接口。</summary>
    public void Skip()
    {
        if (IsPlaying) Finish(true);
    }

    /// <summary>停止但不发送结束事件，用于面板关闭或外部取消。</summary>
    public void Stop()
    {
        IsPlaying = false;
        fastForwardRequested = false;
        StopBackground();
    }

    private void Finish(bool skipped)
    {
        Stop();
        onFinished.Invoke(skipped);
        OnFinished?.Invoke();
    }

    private void StartBackground()
    {
        if (backgroundImage != null)
        {
            backgroundImage.sprite = backgroundSprite;
            backgroundImage.enabled = backgroundMode != BackgroundMode.None && backgroundSprite != null;
            backgroundImage.raycastTarget = false;
        }
        if (videoImage != null)
        {
            videoImage.enabled = false;
            videoImage.raycastTarget = false;
        }
        if (backgroundMode != BackgroundMode.Video) return;
        if (videoPlayer == null || videoImage == null || videoClip == null)
        {
            Debug.LogWarning("片尾视频背景缺少 VideoPlayer、RawImage 或 VideoClip，使用背景图片继续播放字幕。", this);
            return;
        }

        activeVideoPlayer = videoPlayer;
        activeVideoPlayer.Stop();
        activeVideoPlayer.playOnAwake = false;
        activeVideoPlayer.source = VideoSource.VideoClip;
        activeVideoPlayer.clip = videoClip;
        activeVideoPlayer.isLooping = loopVideo;
        activeVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        activeVideoPlayer.aspectRatio = VideoAspectRatio.FitInside;

        // 使用固定大小的目标纹理，避免高分辨率视频产生过大的显存开销。
        videoTexture = new RenderTexture(1920, 1080, 0);
        videoTexture.Create();
        activeVideoPlayer.targetTexture = videoTexture;
        videoImage.texture = videoTexture;
        activeVideoPlayer.prepareCompleted += OnVideoPrepared;
        activeVideoPlayer.errorReceived += OnVideoError;
        activeVideoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        if (!IsPlaying || source != activeVideoPlayer) return;
        videoImage.enabled = true;
        source.Play();
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (source != activeVideoPlayer) return;
        Debug.LogWarning("片尾背景视频播放失败，字幕继续播放：" + message, this);
        StopBackground();
    }

    private void StopBackground()
    {
        if (activeVideoPlayer != null)
        {
            activeVideoPlayer.prepareCompleted -= OnVideoPrepared;
            activeVideoPlayer.errorReceived -= OnVideoError;
            activeVideoPlayer.Stop();
            activeVideoPlayer.targetTexture = null;
            activeVideoPlayer = null;
        }
        if (videoImage != null)
        {
            videoImage.enabled = false;
            if (videoImage.texture == videoTexture) videoImage.texture = null;
        }
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
            videoTexture = null;
        }
    }

    private void OnDisable()
    {
        Stop();
    }
}