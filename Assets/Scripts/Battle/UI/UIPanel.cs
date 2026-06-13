using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ui页面基类
/// </summary>
public class UIPanel : MonoBehaviour
{
    [SerializeField]
    private bool hasInited;
    
    private void OnEnable()
    {
        if (!hasInited)
        {
            Init();
            hasInited = true;
            UpdateDisplay();
        }
        else
        {
            UpdateDisplay();
        }
    }

    public virtual void Init()
    {

    }

    public virtual void UpdateDisplay()
    {

    }

    public virtual void ShowPanel()
    {
        gameObject.SetActive(true);
    }
    public virtual void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    // ── Inspector 配置 ──────────────────────────────
    [Header("面板引用")]
    public CanvasGroup   canvasGroup;
    public Image         borderImage;
    public RectTransform scanLine;
    public Slider        powerBar;
    public Image         statusDot;

    [Header("动效参数")]
    [Range(0.1f, 3f)]
    public float timeScale      = 1f;
    public float popScaleFactor = 0.72f;
    public float popOffsetY     = 18f;
    public Color  borderActiveColor;
    public Color  statusOnColor;
    public float powerBarTarget = 0.84f;

    // ── 私有字段 ────────────────────────────────────
    private RectTransform                _rt;
    private Vector3                      _originalScale;
    private Vector2                      _originalAnchoredPos;
    private List<Text>                   _allTexts;
    private Dictionary<Text, string>     _originalStrings;
    private const string DECODE_CHARS = "ABCDEF0123456789#@$%!?><|/\\~^";

    // ── 生命周期 ─────────────────────────────────────
    private void Awake()
    {
        _rt = GetComponent<RectTransform>();

        _originalScale       = _rt.localScale;
        _originalAnchoredPos = _rt.anchoredPosition;

        _allTexts        = new List<Text>(
                               GetComponentsInChildren<Text>(true));
        _originalStrings = new Dictionary<Text, string>();
        foreach (var t in _allTexts)
            _originalStrings[t] = t.text;
    }

    // ── 公开入口 ─────────────────────────────────────
    public void Open()
    {
        gameObject.SetActive(true);
        StartCoroutine(OpenSequence());
    }

    public void Close()
    {
        canvasGroup.DOFade(0f, T(0.18f))
            .OnComplete(() => gameObject.SetActive(false));
    }

    // ── 主序列 ───────────────────────────────────────
    private IEnumerator OpenSequence()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        Vector3 startScale = _originalScale * popScaleFactor;
        Vector2 startPos   = _originalAnchoredPos + Vector2.down * popOffsetY;

        _rt.localScale       = startScale;
        _rt.anchoredPosition = startPos;

        foreach (var t in _allTexts)
            t.text = RandomString(_originalStrings[t].Length);

        if (scanLine != null)
        {
            scanLine.anchoredPosition = Vector2.up * (_rt.rect.height / 2f);
            scanLine.GetComponent<CanvasGroup>().alpha = 0f;
        }

        if (statusDot   != null) statusDot.color   = Color.clear;
        if (powerBar    != null) powerBar.value    = 0f;
        if (borderImage != null) borderImage.color = Color.clear;

        // Phase 1 · 弹出
        var popSeq = DOTween.Sequence();
        popSeq.Append(_rt.DOScale(_originalScale, T(0.28f)).SetEase(Ease.OutBack));
        popSeq.Join(_rt.DOAnchorPos(_originalAnchoredPos, T(0.28f)).SetEase(Ease.OutCubic));
        popSeq.Join(canvasGroup.DOFade(1f, T(0.22f)));
        yield return popSeq.WaitForCompletion();

        // Phase 2 · 边框激活
        if (borderImage != null)
            yield return borderImage.DOColor(borderActiveColor, T(0.18f)).WaitForCompletion();

        // Phase 3 · 扫描线
        if (scanLine != null)
        {
            var sg = scanLine.GetComponent<CanvasGroup>();
            sg.alpha = 1f;
            yield return scanLine
                .DOAnchorPosY(-_rt.rect.height / 2f, T(0.30f))
                .SetEase(Ease.Linear)
                .WaitForCompletion();
            sg.alpha = 0f;
        }

        // Phase 4 · 所有 Text 子节点依次解码
        foreach (var txt in _allTexts)
        {
            yield return StartCoroutine(
                DecodeText(txt, _originalStrings[txt],
                           ticks: 10, intervalSec: T(0.055f)));
            yield return new WaitForSeconds(T(0.04f));
        }

        // Phase 5 · 进度条
        if (powerBar != null)
            yield return DOTween
                .To(() => powerBar.value, v => powerBar.value = v,
                    powerBarTarget, T(0.50f))
                .SetEase(Ease.OutCubic)
                .WaitForCompletion();

        // Phase 6 · 状态灯 + 交互开放
        if (statusDot != null)
            statusDot.DOColor(statusOnColor, T(0.15f));

        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

    // ── 辅助：文本解码协程 ───────────────────────────
    private IEnumerator DecodeText(Text txt, string real,
                                     int ticks, float intervalSec)
    {
        for (int i = 0; i < ticks; i++)
        {
            int resolved = Mathf.FloorToInt((float)i / ticks * real.Length);
            txt.text = real.Substring(0, resolved)
                     + RandomString(real.Length - resolved);
            yield return new WaitForSeconds(intervalSec);
        }
        txt.text = real;
    }

    // ── 辅助：生成随机乱码字符串 ────────────────────
    private string RandomString(int length)
    {
        var sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(DECODE_CHARS[Random.Range(0, DECODE_CHARS.Length)]);
        return sb.ToString();
    }

    // ── 辅助：时间缩放 ───────────────────────────────
    private float T(float sec) => sec / timeScale;
    
    
}
