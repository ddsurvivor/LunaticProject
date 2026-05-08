using UnityEngine;
using System.Collections;

/// <summary>
/// 场景物体反馈脚本：激活时自动播放缩放、淡出动画并播放音效。
/// 适用于使用 SpriteRenderer 的 2D 场景物体。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SpriteEffectPlayer : MonoBehaviour
{
    [Header("动画配置")]
    [Tooltip("动画持续总时间")]
    [SerializeField] private float duration = 0.6f;
    [Tooltip("相对于初始缩放的放大倍数")]
    [SerializeField] private float scaleMultiplier = 1.25f;

    [Header("音效配置")]
    [SerializeField] private AudioClip feedbackSfx;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    [Header("状态控制")]
    [Tooltip("动画播放完毕后是否禁用物体")]
    [SerializeField] private bool autoDeactivate = true;

    [SerializeField]private SpriteRenderer _spriteRenderer;
    [SerializeField]private AudioSource _audioSource;
    private Vector3 _baseScale;
    private Color _baseColor;

    private void Awake()
    {
        // 缓存组件引用以提高性能
        //_spriteRenderer = GetComponent<SpriteRenderer>();
        //_audioSource = GetComponent<AudioSource>();
        
        // 记录物体的原始状态，以便在 SetActive(true) 时能正确重置
        _baseScale = transform.localScale;
        _baseColor = _spriteRenderer.color;

        // 基础音效组件设置
        _audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        // 每次物体激活时，停止可能存在的旧协程并开启新动画
        StopAllCoroutines();
        StartCoroutine(PlaySpriteAnimation());
    }

    private IEnumerator PlaySpriteAnimation()
    {
        // 1. 初始化状态重置
        transform.localScale = _baseScale;
        _spriteRenderer.color = _baseColor;

        // 2. 触发音效
        if (feedbackSfx != null)
        {
            AudioSource.PlayClipAtPoint(feedbackSfx, Camera.main.transform.position, volume);
            //_audioSource.PlayOneShot(feedbackSfx, volume);
        }

        // 3. 执行补间动画
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // 缩放：Lerp 到目标倍数
            transform.localScale = Vector3.Lerp(_baseScale, _baseScale * scaleMultiplier, progress);

            // 透明度：从原始 Alpha 逐渐减淡至 0
            Color lerpColor = _baseColor;
            lerpColor.a = Mathf.Lerp(_baseColor.a, 0f, progress);
            _spriteRenderer.color = lerpColor;

            yield return null;
        }

        // 4. 完成后的回收逻辑
        if (autoDeactivate)
        {
            gameObject.SetActive(false);
        }
    }
}