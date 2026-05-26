using UnityEngine;
using UnityEngine.UI; // 使用旧版 Text
using DG.Tweening;

[RequireComponent(typeof(Text))]
public class TipText : MonoBehaviour
{
    [Header("动画参数配置")]
    [SerializeField] private float moveDistance = 60f;     
    [SerializeField] private float duration = 1.2f;        
    private Ease moveEase = Ease.OutQuad; 

    [SerializeField]
    private Text _text;
    private Sequence _tipSequence; // 保存序列引用以便复用时清理

    private void Awake()
    {
        //_text = GetComponent<Text>();
    }

    /// <summary>
    /// 外部调用的公共接口：初始化并播放提示动画
    /// </summary>
    /// <param name="message">需要显示的文本内容</param>
    public void ShowTip(string message)
    {
        if (_text == null) return;

        // 安全机制：如果该对象被提早复用，先杀死正在进行的动画
        _tipSequence?.Kill();

        // 1. 设置文本内容
        _text.text = message;

        // 2. 重置透明度
        Color originalColor = _text.color;
        _text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

        // 3. 创建动画序列
        _tipSequence = DOTween.Sequence();

        // 4. 并行播放：位移 + 淡出
        _tipSequence.Join(transform.DOLocalMoveY(transform.localPosition.y + moveDistance, duration).SetEase(moveEase));
        //_tipSequence.Join(_text.DOFade(0f, duration));
        _tipSequence.Insert(duration/2f, _text.DOFade(0f, duration/2f));
        // 5. 动画完成后【自动关闭自身】以供对象池回收
        _tipSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }

    private void OnDestroy()
    {
        // 良好的习惯：物体销毁时清理未完成的 Tweener，防止内存泄漏
        _tipSequence?.Kill();
    }
}