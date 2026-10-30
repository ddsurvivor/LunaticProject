using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class BattleStartUIPanel : MonoBehaviour
{
    [SerializeField] private GameObject bg;
    [SerializeField] private RectTransform bgBlock;
    [SerializeField] private RectTransform startIcon;
    [SerializeField] private CanvasGroup canvasGroup;
    private Sequence animationSequence;
    private TimeManager pauseClock;
    private UnityEngine.UI.Text battleTitle;
    private UnityEngine.UI.Image originalIcon;
    private bool originalIconEnabled;
    /// <summary>开始窗口正在占用输入；关闭或取消动画后立即释放。</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>重复播放先取消旧动画；战斗内持有暂停，UI动画按真实时间完成。</summary>
    public void PlayBattleStartAnimation(float totalDuration)
    {
        CancelAnimation();
        if (bgBlock == null || startIcon == null) return;
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
        gameObject.SetActive(true); IsPlaying = true;
        canvasGroup.alpha = 1; canvasGroup.blocksRaycasts = true; canvasGroup.interactable = false;
        bgBlock.localPosition = startIcon.localPosition = Vector3.zero;
        var scene = BattleScene.Ins;
        if (scene != null && scene.UM != null && scene.UM.battleStartUIPanel == this)
        {
            pauseClock = scene.TM; pauseClock.AcquirePause(this);
            originalIcon = startIcon.GetComponent<UnityEngine.UI.Image>();
            if (originalIcon != null) { originalIconEnabled = originalIcon.enabled; originalIcon.enabled = false; }
            if (battleTitle == null)
            {
                var node = new GameObject("BattleStartTitle", typeof(RectTransform)); node.transform.SetParent(startIcon, false);
                var rect = (RectTransform)node.transform; rect.sizeDelta = new Vector2(700, 100);
                battleTitle = node.AddComponent<UnityEngine.UI.Text>();
                battleTitle.font = scene.UM.skillNamText.font; battleTitle.fontSize = 48;
                battleTitle.alignment = TextAnchor.MiddleCenter; battleTitle.raycastTarget = false;
            }
            battleTitle.text = "战斗开始"; battleTitle.gameObject.SetActive(true);
        }
        float duration = Mathf.Max(.05f, totalDuration);
        animationSequence = DOTween.Sequence().SetUpdate(true);
        animationSequence.Append(bgBlock.DOLocalMoveX(0, duration * .3f).From(-1922f));
        animationSequence.Join(startIcon.DOLocalMoveX(0, duration * .7f).From(-1922f));
        animationSequence.Append(canvasGroup.DOFade(0, duration * .3f));
        animationSequence.OnComplete(CancelAnimation);
    }

    /// <summary>关闭或离场时停止全部所属Tween并释放本窗口暂停。</summary>
    public void CancelAnimation()
    {
        var sequence = animationSequence; animationSequence = null;
        sequence?.Kill(); IsPlaying = false;
        if (pauseClock != null) pauseClock.ReleasePause(this);
        pauseClock = null;
        if (originalIcon != null) originalIcon.enabled = originalIconEnabled;
        if (battleTitle != null) battleTitle.gameObject.SetActive(false);
        if (canvasGroup != null) { canvasGroup.blocksRaycasts = false; canvasGroup.alpha = 0; }
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (IsPlaying || animationSequence != null) CancelAnimation();
    }
}
