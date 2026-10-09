using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class 剧本System
{
    [Header("CGLOG 前景正文")]
    [SerializeField] private GameObject frontTextRoot;
    [SerializeField] private Text frontText;
    private bool isCGLogMode;
    private 打字机 frontTypewriter;
    private Transform cgLogDialogueRoot;
    private readonly Dictionary<GameObject, bool> cgLogDialogueStates = new Dictionary<GameObject, bool>();
    private Transform cgLogImageLayer;
    private bool cgLogHalfCGActive;
    private Sprite cgLogSprite;
    private Tween cgLogTransition;
    private float cgLogFadeOutTime;
    private bool cgLogFadingOut;

    public bool IsCGLogMode => isCGLogMode;

    private bool ResolveCGLogUI()
    {
        if (frontTextRoot == null)
        {
            foreach (var node in GetComponentsInChildren<Transform>(true))
                if (node.name == "frontTextRoot") { frontTextRoot = node.gameObject; break; }
        }
        if (frontText == null && frontTextRoot != null)
            frontText = frontTextRoot.GetComponentInChildren<Text>(true);
        if (cgLogDialogueRoot == null) cgLogDialogueRoot = transform.Find("DialogueUI");
        return FULLCG != null && frontTextRoot != null && frontText != null;
    }

    /// <summary>显示全屏 CG，并切换为前景正文模式。</summary>
    private void EnterCGLogMode(string cgName, float fadeInTime = 0f, float fadeOutTime = 0f)
    {
        if (!ResolveCGLogUI())
        {
            Debug.LogError("CGLOG 缺少 FULLCG 或 frontTextRoot 下的 Text 节点。");
            return;
        }
        var texture = Resources.Load<Texture2D>("CG/" + cgName);
        if (texture == null)
        {
            Debug.LogError("CGLOG 无法加载图片：" + cgName);
            return;
        }

        bool replacingCG = isCGLogMode;
        float previousFadeOut = cgLogFadeOutTime;
        cgLogTransition?.Kill();
        cgLogTransition = null;
        if (!isCGLogMode)
        {
            cgLogDialogueStates.Clear();
            if (cgLogDialogueRoot != null)
                foreach (Transform child in cgLogDialogueRoot)
                    if (child.name != "btnRoot") cgLogDialogueStates[child.gameObject] = child.gameObject.activeSelf;
            cgLogHalfCGActive = HALFCG != null && HALFCG.gameObject.activeSelf;
            cgLogImageLayer = FULLCG.transform.parent == transform ? FULLCG.transform : FULLCG.transform.parent;
        }
        isCGLogMode = true;
        cgLogFadeOutTime = fadeOutTime;
        FULLCG.DOKill();
        cgLogImageLayer.gameObject.SetActive(true);
        FULLCG.gameObject.SetActive(true);
        if (HALFCG != null) HALFCG.gameObject.SetActive(false);
        SetCGLogChoiceVisibility(false);
        if (replacingCG && previousFadeOut > 0f)
        {
            var sequence = DOTween.Sequence().SetTarget(FULLCG);
            sequence.Append(FULLCG.DOFade(0f, previousFadeOut));
            sequence.AppendCallback(() => ApplyCGLogSprite(texture, fadeInTime > 0f ? 0f : 1f));
            if (fadeInTime > 0f) sequence.Append(FULLCG.DOFade(1f, fadeInTime));
            cgLogTransition = sequence;
        }
        else
        {
            ApplyCGLogSprite(texture, fadeInTime > 0f ? 0f : 1f);
            if (fadeInTime > 0f) cgLogTransition = FULLCG.DOFade(1f, fadeInTime);
        }
    }

    private void ApplyCGLogSprite(Texture2D texture, float alpha)
    {
        if (cgLogSprite != null) Destroy(cgLogSprite);
        cgLogSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        FULLCG.sprite = cgLogSprite;
        FULLCG.color = new Color(1f, 1f, 1f, alpha);
    }

    /// <summary>替换前景正文，以正文一半的速度逐字显示。</summary>
    private void ShowCGLogText(string text)
    {
        if (!ResolveCGLogUI()) return;
        frontTextRoot.SetActive(true);
        frontText.gameObject.SetActive(true);
        frontText.DOKill();
        var color = frontText.color;
        color.a = 1f;
        frontText.color = color;
        if (frontTypewriter == null)
        {
            frontTypewriter = frontText.GetComponent<打字机>();
            if (frontTypewriter == null) frontTypewriter = frontText.gameObject.AddComponent<打字机>();
            frontTypewriter._textComponent = frontText;
            var template = 剧本预制体 != null ? 剧本预制体.GetComponent<打字机>() : null;
            // _typeSpeed 是逐字间隔；速度为正文的 50% 时，间隔需加倍。
            // CharacterDelay 仍统一应用设置菜单中的文本速度倍率。
            frontTypewriter._typeSpeed = (template != null ? template._typeSpeed : frontTypewriter._typeSpeed) / 0.5f;
        }
        currentTypewriter = frontTypewriter;
        frontTypewriter.播放文本(text, this);
    }

    /// <summary>显示选项时隐藏前景正文，选择后恢复。</summary>
    private void SetCGLogChoiceVisibility(bool showChoices)
    {
        if (!isCGLogMode) return;
        foreach (var item in cgLogDialogueStates)
            if (item.Key != null) item.Key.SetActive(showChoices && item.Value);
        frontTextRoot.SetActive(!showChoices);
        if (!showChoices && frontTypewriter != null) currentTypewriter = frontTypewriter;
    }

    private void OnDestroy()
    {
        CancelReaderHide();
        cgLogTransition?.Kill();
        if (cgLogSprite != null) Destroy(cgLogSprite);
    }

    /// <summary>退出前景正文模式，并按需淡出 CG。</summary>
    private void ResetCGLogMode(bool fadeOut = false)
    {
        ResolveCGLogUI();
        bool wasFadingOut = cgLogFadingOut;
        cgLogTransition?.Kill();
        cgLogTransition = null;
        bool animateExit = fadeOut && isCGLogMode && cgLogFadeOutTime > 0f && FULLCG != null;
        if (!animateExit && wasFadingOut && cgLogImageLayer != null)
            cgLogImageLayer.gameObject.SetActive(false);
        if (isCGLogMode)
        {
            foreach (var item in cgLogDialogueStates)
                if (item.Key != null) item.Key.SetActive(item.Value);
            if (currentTypewriter == frontTypewriter) currentTypewriter = null;
            if (!animateExit && cgLogImageLayer != null)
            {
                cgLogImageLayer.gameObject.SetActive(false);
            }
            if (HALFCG != null) HALFCG.gameObject.SetActive(cgLogHalfCGActive);
        }
        isCGLogMode = false;
        cgLogDialogueStates.Clear();
        if (frontTypewriter != null) frontTypewriter.清空文本();
        if (frontTextRoot != null) frontTextRoot.SetActive(false);
        if (frontText != null) frontText.text = "";
        if (animateExit)
        {
            var retiringSprite = cgLogSprite;
            cgLogSprite = null;
            cgLogFadingOut = true;
            cgLogTransition = FULLCG.DOFade(0f, cgLogFadeOutTime)
                .OnComplete(() =>
                {
                    if (cgLogImageLayer != null) cgLogImageLayer.gameObject.SetActive(false);
                })
                .OnKill(() =>
                {
                    cgLogFadingOut = false;
                    if (FULLCG != null && FULLCG.sprite == retiringSprite) FULLCG.sprite = null;
                    if (retiringSprite != null) Destroy(retiringSprite);
                });
        }
        else if (cgLogSprite != null)
        {
            if (FULLCG != null && FULLCG.sprite == cgLogSprite) FULLCG.sprite = null;
            Destroy(cgLogSprite);
            cgLogSprite = null;
        }
    }
}
