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

    private void EnterCGLogMode(string cgName)
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
        FULLCG.DOKill();
        if (cgLogSprite != null) Destroy(cgLogSprite);
        cgLogSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        FULLCG.sprite = cgLogSprite;
        FULLCG.color = Color.white;
        cgLogImageLayer.gameObject.SetActive(true);
        FULLCG.gameObject.SetActive(true);
        if (HALFCG != null) HALFCG.gameObject.SetActive(false);
        SetCGLogChoiceVisibility(false);
    }

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
        if (cgLogSprite != null) Destroy(cgLogSprite);
    }

    private void ResetCGLogMode()
    {
        ResolveCGLogUI();
        if (isCGLogMode)
        {
            foreach (var item in cgLogDialogueStates)
                if (item.Key != null) item.Key.SetActive(item.Value);
            if (currentTypewriter == frontTypewriter) currentTypewriter = null;
            if (cgLogImageLayer != null)
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
        if (cgLogSprite != null)
        {
            if (FULLCG != null && FULLCG.sprite == cgLogSprite) FULLCG.sprite = null;
            Destroy(cgLogSprite);
            cgLogSprite = null;
        }
    }
}
