using UnityEngine;

/// <summary>统一跳字表现：上浮、后半程淡出，完成后交回管理器对象池。</summary>
public class TipText : MonoBehaviour
{
    [Header("动画参数配置")]
    [SerializeField] private float moveDistance = 2f;
    // 保留预制体原时长，管理器统一乘以2；独立调用也默认翻倍。
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private UnityEngine.UI.Text _text;
    private Vector3 startPosition;
    private Color startColor;
    private float elapsed, playDuration;
    private int originalFontSize;
    private bool playing;
    public float BaseDuration => Mathf.Max(0.01f, duration);
    public bool IsPlaying => playing && gameObject.activeInHierarchy;
    public int FontSize => _text != null ? _text.fontSize : originalFontSize;

    internal void EnsureAbove(float minimumY)
    {
        if (!IsPlaying || transform.position.y >= minimumY) return;
        float shift = minimumY - transform.position.y;
        // 移动整条动画轨迹，不重置计时或透明度，也不让下一帧动画把排好的位置覆盖。
        startPosition += Vector3.up * shift;
        transform.position += Vector3.up * shift;
    }
    private void Awake() => CacheText();
    private void CacheText()
    {
        if (_text == null) _text = GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (_text != null && originalFontSize == 0) originalFontSize = _text.fontSize;
    }
    public void ShowTip(string message, Color? textColor = null, int? fontSize = null,
        float? displayDuration = null)
    {
        CacheText();
        if (_text == null) { playing = false; gameObject.SetActive(false); return; }
        _text.text = message;
        // BestFit会把伤害字号缩回原预制体上限，显式字号应直接生效。
        _text.resizeTextForBestFit = false;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.raycastTarget = false;
        _text.fontSize = Mathf.Max(1, fontSize ?? originalFontSize);
        startColor = textColor ?? Color.white;
        _text.color = startColor;
        startPosition = transform.position;
        playDuration = Mathf.Max(0.01f, displayDuration ?? BaseDuration * 2f);
        elapsed = 0f;
        playing = true;
        gameObject.SetActive(true);
    }
    private void Update() => AdvanceAnimation(Time.deltaTime);
    internal void AdvanceAnimation(float deltaTime)
    {
        if (!IsPlaying) return;
        elapsed += Mathf.Max(0f, deltaTime);
        float t = Mathf.Clamp01(elapsed / playDuration);
        float eased = 1f - (1f - t) * (1f - t);
        transform.position = startPosition + Vector3.up * (moveDistance * eased);
        _text.color = new Color(startColor.r, startColor.g, startColor.b,
            startColor.a * (t <= 0.5f ? 1f : 2f * (1f - t)));
        if (t >= 1f) { playing = false; gameObject.SetActive(false); }
    }
    private void OnDisable() => playing = false;
}
