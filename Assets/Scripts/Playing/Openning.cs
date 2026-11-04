using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;

public class Openning : MonoBehaviour
{
    [Header("视频")]
    public VideoPlayer videoPlayer;

    public RenderTexture rt;

    [Header("LOGO图片")]
    public Image logoImage;
    public float logoFadeInTime = 1f;
    public float logoShowTime = 2f;
    public float logoFadeOutTime = 1f;

    [Header("背景音乐")]
    public AudioSource bgmSource;

    [Header("文本")]
    public Text[] texts; // 3行文本
    public float textFadeInTime = 2f;
    public float textShowTime = 3f;
    public float textFadeOutTime = 2f;

    [Header("场景")]
    public string nextSceneName;

    private Sequence activeSequence; // 离场时只清理本片头拥有的动画。
    private bool videoFinished;
    private bool leaving; // 正常结束与跳过共用一次转场。
    private const float PrepareTimeout = 15f; // 视频准备失败时的最长等待秒数。

    private void Start()
    {
        if (logoImage != null) logoImage.color = new Color(1, 1, 1, 0);
        foreach (var t in texts)
            if (t != null) t.color = new Color(t.color.r, t.color.g, t.color.b, 0);
        StartCoroutine(PlaySequenceOpen());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Skip();
    }

    /// <summary>跳过片头进入主场景；可绑定按钮，转场中的重复请求不会重复加载。</summary>
    public void Skip()
    {
        if (leaving || GM.Ins == null || !GM.Ins.CanLoadScene(nextSceneName)) return;
        leaving = true;
        StopAllCoroutines();
        CleanupPlayback();
        GM.Ins.LoadScene(nextSceneName);
    }

    private IEnumerator PlaySequenceOpen()
    {
        if (videoPlayer != null && (videoPlayer.source == VideoSource.Url || videoPlayer.clip != null))
        {
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.Prepare();
            float deadline = Time.realtimeSinceStartup + PrepareTimeout;
            while (!videoPlayer.isPrepared && !videoFinished && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!videoFinished && videoPlayer.isPrepared)
            {
                videoPlayer.Play();
                // 以播放完成事件为准；解码器卡住或没有结束事件时仍可继续正式流程。
                deadline = Time.realtimeSinceStartup + (float)(videoPlayer.length > 0 ? videoPlayer.length : 120) + 10f;
                while (!videoFinished && Time.realtimeSinceStartup < deadline) yield return null;
            }
            if (!videoFinished) Debug.LogWarning("片头视频超时，继续播放片头文字。", this);
            videoPlayer.Stop();
        }
        else Debug.LogWarning("片头视频未配置，继续播放片头文字。", this);

        if (logoImage != null)
        {
            activeSequence = DOTween.Sequence().SetUpdate(true);
            activeSequence.Append(logoImage.DOFade(1, logoFadeInTime).SetEase(Ease.Linear));
            activeSequence.AppendInterval(logoShowTime);
            if (videoPlayer != null) activeSequence.AppendCallback(() => videoPlayer.gameObject.SetActive(false));
            activeSequence.Append(logoImage.DOFade(0, logoFadeOutTime));
            yield return activeSequence.WaitForCompletion();
        }
        if (videoPlayer != null) videoPlayer.gameObject.SetActive(false);
        if (bgmSource != null) bgmSource.Play();
        activeSequence = DOTween.Sequence().SetUpdate(true);
        activeSequence.AppendInterval(1f);
        foreach (var t in texts)
            if (t != null) activeSequence.Append(t.DOFade(1, textFadeInTime).SetEase(Ease.Linear));
        activeSequence.AppendInterval(textShowTime);
        foreach (var t in texts)
            if (t != null) activeSequence.Append(t.DOFade(0, textFadeOutTime));
        yield return activeSequence.WaitForCompletion();
        // 入场淡出尚未完成时等待 GM 释放转场权，避免快速跳过丢失请求。
        while (GM.Ins != null && GM.Ins.IsTransitioning) yield return null;
        Skip();
    }

    private void OnVideoFinished(VideoPlayer source) => videoFinished = true;

    private void OnVideoError(VideoPlayer source, string message)
    {
        videoFinished = true;
        Debug.LogWarning($"片头视频播放失败，继续播放片头文字：{message}", this);
    }

    private void CleanupPlayback()
    {
        activeSequence?.Kill();
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }
        if (bgmSource != null) bgmSource.Stop();
    }

    private void OnDestroy() => CleanupPlayback();
}
