using UnityEngine;

/// <summary>沿用战斗教程字体和面板样式，显示不拦截世界点击的当前教学目标。</summary>
public sealed class SpaceStationTrainingHint : MonoBehaviour
{
    private UnityEngine.UI.Text label;

    public static SpaceStationTrainingHint Create(UnityEngine.UI.Text template)
    {
        var root = new GameObject("SpaceStationTrainingHint", typeof(RectTransform));
        root.transform.SetParent(template.GetComponentInParent<Canvas>(true).rootCanvas.transform, false);
        root.layer = template.gameObject.layer;
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -155);
        rect.sizeDelta = new Vector2(600, 104);
        var background = root.AddComponent<UnityEngine.UI.Image>();
        var panel = template.GetComponentInParent<TutorialUI>(true).GetComponent<UnityEngine.UI.Image>();
        background.sprite = panel != null ? panel.sprite : null;
        background.color = new Color(.055f, .085f, .11f, .94f);
        background.raycastTarget = false;
        var view = root.AddComponent<SpaceStationTrainingHint>();
        view.label = Instantiate(template, root.transform, false);
        var textRect = view.label.rectTransform;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18, 10);
        textRect.offsetMax = new Vector2(-18, -10);
        view.label.fontSize = 21;
        view.label.resizeTextForBestFit = false;
        view.label.horizontalOverflow = HorizontalWrapMode.Wrap;
        view.label.verticalOverflow = VerticalWrapMode.Truncate;
        view.label.alignment = TextAnchor.MiddleLeft;
        view.label.color = Color.white;
        view.label.raycastTarget = false;
        view.label.gameObject.SetActive(true);
        return view;
    }

    /// <summary>只更新当前目标说明；教学流程由真实战斗事件推进。</summary>
    public void SetInstruction(string text) => label.text = text;
}
