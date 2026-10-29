using System;
using System.Linq;
using UnityEngine;

public sealed partial class MemeLoadoutPanel
{
    private RectTransform card, listContent;
    private UnityEngine.UI.Text header, subtitle, detail, feedback;
    private UnityEngine.UI.Button confirm;
    private readonly UnityEngine.UI.Button[] slotButtons = new UnityEngine.UI.Button[MemeEquipment.SlotCount];

    private void Build(UnityEngine.UI.Text source)
    {
        Stretch((RectTransform)transform);
        var shade = gameObject.AddComponent<UnityEngine.UI.Image>();
        shade.color = new Color(0, 0, 0, .78f);
        card = NewRect("LoadoutCard", transform);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
        var background = card.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.065f, .08f, .10f, 1);
        header = MakeText("Title", card, source, 24);
        Place(header.rectTransform, 24, 12, 820, 44);
        subtitle = MakeText("CostAndMode", card, source, 17);
        Place(subtitle.rectTransform, 24, 62, 820, 26);
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slot = i;
            slotButtons[i] = MakeButton("Slot" + (i + 1), card, source, "", () => SelectSlot(slot));
            Place((RectTransform)slotButtons[i].transform, 24 + i * 300, 92, 288, 44);
        }
        var clear = MakeButton("ClearSlot", card, source, "清空当前槽", () => SelectMeme(0));
        var clearRect = (RectTransform)clear.transform;
        clearRect.anchorMin = clearRect.anchorMax = clearRect.pivot = new Vector2(1, 1);
        clearRect.anchoredPosition = new Vector2(-24, -92);
        clearRect.sizeDelta = new Vector2(156, 44);
        var body = NewRect("Body", card);
        Stretch(body);
        body.offsetMin = new Vector2(24, 112);
        body.offsetMax = new Vector2(-24, -150);
        var list = NewRect("Catalog", body);
        list.anchorMin = Vector2.zero;
        list.anchorMax = new Vector2(.42f, 1);
        list.offsetMin = Vector2.zero;
        list.offsetMax = new Vector2(-12, 0);
        BuildScroll(list, out listContent);
        var preview = NewRect("Details", body);
        preview.anchorMin = new Vector2(.42f, 0);
        preview.anchorMax = Vector2.one;
        preview.offsetMin = new Vector2(12, 0);
        preview.offsetMax = Vector2.zero;
        BuildScroll(preview, out var previewContent);
        detail = MakeText("DescriptionAndSources", previewContent, source, 19);
        var detailLayout = detail.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        detailLayout.minHeight = 100;
        if (catalog?.memes != null)
            foreach (var definition in catalog.memes.Where(m => m != null).OrderBy(m => m.id))
            {
                var meme = definition;
                bool unlocked = profile.memeProgress?.IsUnlocked(meme.id) == true;
                string status = !meme.enabled || !meme.IsValid() ? "不可用" : unlocked ? "已解锁" : "未解锁";
                var row = MakeButton("Meme" + meme.id, listContent, source, meme.displayName + "  ·  " + status, () => SelectMeme(meme.id));
                var layout = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredHeight = 58;
                layout.minHeight = 58;
            }
        feedback = MakeText("Feedback", card, source, 17);
        var feedbackRect = feedback.rectTransform;
        feedbackRect.anchorMin = feedbackRect.anchorMax = feedbackRect.pivot = new Vector2(0, 0);
        feedbackRect.anchoredPosition = new Vector2(24, 70);
        feedbackRect.sizeDelta = new Vector2(880, 36);
        confirm = MakeButton("Confirm", card, source, battle != null ? "确认换装 · 2 AP" : "确认装配", () => TryConfirm());
        Footer((RectTransform)confirm.transform, -24, 220);
        var cancel = MakeButton("Cancel", card, source, "取消", Close);
        Footer((RectTransform)cancel.transform, -256, 140);
        RefreshSize();
    }

    private void Refresh()
    {
        if (header == null) return;
        header.text = "模因装配  ·  " + player.NAME;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            slotButtons[i].GetComponentInChildren<UnityEngine.UI.Text>().text = "槽位 " + (i + 1) + "  ·  " + MemeLoadoutInfo.Name(catalog, draft[i]);
            slotButtons[i].GetComponent<UnityEngine.UI.Image>().color = i == selectedSlot ? new Color(.20f, .34f, .42f) : new Color(.12f, .16f, .20f);
        }
        detail.text = MemeLoadoutInfo.Describe(catalog?.GetDefinition(inspectedId)) + "\n\n" +
            MemeLoadoutInfo.Sources(player, data, actor, catalog) + "\n\n自由槽只接受已解锁词条。解锁由剧情、成长或配方授予；固有来源不占槽位。";
        RefreshStatus();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(detail.transform.parent as RectTransform);
    }

    private void RefreshStatus()
    {
        if (confirm == null) return;
        RefreshSize();
        string error = ConfirmError();
        confirm.interactable = error == null;
        subtitle.text = battle == null ? "战外装配免费 · 选择槽位后点击词条 · 确认前不会保存草稿" :
            "战内整套换装 2 AP · 当前 " + (actor != null ? actor.unitAttrCenter.CurMovePoint.ToString() : "—") + " AP · 确认才支付";
        feedback.text = message ?? error ?? "配置有效，确认后生效。";
        feedback.color = message != null ? new Color(1, .73f, .48f) : new Color(.75f, .84f, .89f);
    }

    private void RefreshSize()
    {
        var parent = transform.parent as RectTransform;
        if (card == null || parent == null) return;
        card.sizeDelta = new Vector2(Mathf.Min(1000, parent.rect.width - 48), Mathf.Min(690, parent.rect.height - 48));
        float width = card.sizeDelta.x;
        float clearWidth = 156;
        float slotWidth = (width - 48 - clearWidth - 24) / 2;
        for (int i = 0; i < slotButtons.Length; i++) Place((RectTransform)slotButtons[i].transform, 24 + i * (slotWidth + 12), 92, slotWidth, 44);
        header.rectTransform.sizeDelta = new Vector2(width - 48, 44);
        subtitle.rectTransform.sizeDelta = new Vector2(width - 48, 26);
        feedback.rectTransform.sizeDelta = new Vector2(width - 48, 36);
    }

    private static void Footer(RectTransform rect, float right, float width)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(right, 22);
        rect.sizeDelta = new Vector2(width, 42);
    }
    private static void Place(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.gameObject.layer = parent.gameObject.layer;
        return rect;
    }
    private static UnityEngine.UI.Text MakeText(string name, Transform parent, UnityEngine.UI.Text source, int size)
    {
        var rect = NewRect(name, parent);
        var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = source.font;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }
    private static UnityEngine.UI.Button MakeButton(string name, Transform parent, UnityEngine.UI.Text source, string label, Action action)
    {
        var rect = NewRect(name, parent);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(.14f, .20f, .25f);
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());
        var text = MakeText("Label", rect, source, 18);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12, 4);
        text.rectTransform.offsetMax = new Vector2(-12, -4);
        text.alignment = TextAnchor.MiddleCenter;
        text.text = label;
        return button;
    }
    private static void BuildScroll(RectTransform root, out RectTransform content)
    {
        var viewport = NewRect("Viewport", root);
        Stretch(viewport);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.09f, .12f, .15f);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
        content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = Vector2.zero;
        var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 8;
        layout.padding = new RectOffset(10, 10, 10, 10);
        var fit = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fit.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        var scroll = root.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
    }
}
