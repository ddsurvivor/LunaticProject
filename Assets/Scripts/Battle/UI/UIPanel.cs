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
    
     [Header("面板引用")]
    public CanvasGroup canvasGroup;

    [Header("动效参数")]
    [Range(0.1f, 3f)]
    public float timeScale      = 1f;
    public float popScaleFactor = 0.72f;
    public float popOffsetY     = 18f;

    private RectTransform            _rt;
    private Vector3                  _originalScale;
    private Vector2                  _originalAnchoredPos;
    private List<Text>               _allTexts;
    private Dictionary<Text, string> _originalStrings;
    private const string DECODE_CHARS = "ABCDEF0123456789#@$%!?><|/\\~^";

    private bool _hasInitCheck;
    private void InitCheck()
    {
        _hasInitCheck = true;
        _rt                  = GetComponent<RectTransform>();
        _originalScale       = _rt.localScale;
        _originalAnchoredPos = _rt.anchoredPosition;

        _allTexts        = new List<Text>(GetComponentsInChildren<Text>(true));
        _originalStrings = new Dictionary<Text, string>();
        foreach (var t in _allTexts)
            _originalStrings[t] = t.text;
    }

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

    private IEnumerator OpenSequence()
    {
        if(!_hasInitCheck) InitCheck();
        // 重置
        canvasGroup.alpha          = 1f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        _rt.localScale       = _originalScale * popScaleFactor;
        _rt.anchoredPosition = _originalAnchoredPos + Vector2.down * popOffsetY;
        /*if (_allTexts!=null && _allTexts.Count > 0)
        {
            
        foreach (var t in _allTexts)
            t.text = RandomString(_originalStrings[t].Length);
        }*/

        // Phase 1 · 弹出
        var seq = DOTween.Sequence();
        seq.Append(_rt.DOScale(_originalScale, T(0.28f)).SetEase(Ease.OutBack));
        seq.Join(_rt.DOAnchorPos(_originalAnchoredPos, T(0.28f)).SetEase(Ease.OutCubic));
        //seq.Join(canvasGroup.DOFade(1f, T(0.28f)));
        yield return seq.WaitForCompletion();

        /*// Phase 2 · 所有 Text 子节点依次解码
        foreach (var txt in _allTexts)
        {
            yield return StartCoroutine(
                DecodeText(txt, _originalStrings[txt],
                           ticks: 10, intervalSec: T(0.055f)));
            yield return new WaitForSeconds(T(0.04f));
        }*/

        canvasGroup.interactable   = true;
        canvasGroup.blocksRaycasts = true;
    }

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

    private string RandomString(int length)
    {
        var sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(DECODE_CHARS[Random.Range(0, DECODE_CHARS.Length)]);
        return sb.ToString();
    }

    private float T(float sec) => sec / timeScale;
    
    
}
