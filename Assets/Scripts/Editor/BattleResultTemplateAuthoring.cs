using UnityEditor;
using UnityEngine;
using static BattleUITemplateAuthoring;

/// <summary>首次创建结果窗口模板，保留 AVG 模块连接和手动布局。</summary>
public static class BattleResultTemplateAuthoring
{
    public const string PrefabPath = "Assets/Resources/UI/BattleResultWindow.prefab";

    /// <summary>生成结果模板及可滚动奖励预告；已有文件不覆盖。</summary>
    [MenuItem("Tools/Battle UI/Create Result Template If Missing")]
    public static void CreateIfMissing()
    {
        if (System.IO.File.Exists(PrefabPath))
        {
            Debug.Log("结果模板已存在，请编辑 prefab 或创建 Variant；生成器不会覆盖它。");
            return;
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Font/SourceHanSansSC/SourceHanSansSC-Regular.otf");
        if (font == null) throw new System.InvalidOperationException("缺少 AVG 字体。");
        var root = new GameObject("BattleResultWindow", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = root.AddComponent<UnityEngine.UI.Image>();
            shade.color = new Color(0, 0, 0, .76f); shade.raycastTarget = true;
            root.AddComponent<ActivePause>();
            var body = Module("Panel", "ResultWindow", rect, new Vector2(720, 460), Vector2.zero);
            var header = Module("Header", "Header", body, new Vector2(600, 56), new Vector2(0, 168));
            var title = header.GetComponentInChildren<UnityEngine.UI.Text>(true);
            SetText(title, "战斗结果", font, 30);
            var scrollRect = Box("Rewards", body, new Vector2(600, 230), new Vector2(0, 8));
            var scroll = scrollRect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;
            var viewport = Box("Viewport", scrollRect, Vector2.zero, Vector2.zero);
            Stretch(viewport); viewport.offsetMax = new Vector2(-24, 0);
            var maskImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            maskImage.color = GameConst.UIColors.SurfaceInset;
            var mask = viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic = false;
            var content = Box("Content", viewport, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            var description = Box("Description", content, Vector2.zero, Vector2.zero).gameObject.AddComponent<UnityEngine.UI.Text>();
            SetText(description, "奖励将在离开战斗时结算", font, 21);
            description.alignment = TextAnchor.UpperLeft;
            description.horizontalOverflow = HorizontalWrapMode.Wrap; description.verticalOverflow = VerticalWrapMode.Overflow;
            scroll.viewport = viewport; scroll.content = content;
            var barRect = Box("Scrollbar", scrollRect, new Vector2(12, 0), Vector2.zero);
            barRect.anchorMin = new Vector2(1, 0); barRect.anchorMax = Vector2.one;
            barRect.pivot = new Vector2(1, .5f);
            var track = barRect.gameObject.AddComponent<UnityEngine.UI.Image>(); track.color = GameConst.UIColors.Surface;
            var handleRect = Box("Handle", barRect, Vector2.zero, Vector2.zero); Stretch(handleRect);
            var handle = handleRect.gameObject.AddComponent<UnityEngine.UI.Image>(); handle.color = GameConst.UIColors.Border;
            var bar = barRect.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
            bar.handleRect = handleRect; bar.targetGraphic = handle; bar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.AutoHide;
            var next = Button(body, "Continue", "继续", 155, font);
            var retry = Button(body, "Retry", "重开战斗", -155, font);
            var view = root.AddComponent<BattleFinishPanel>();
            view.titleText = title; view.descriptionText = description;
            view.continueButton = next.gameObject; view.retryButton = retry.gameObject;
            view.Configure(body, scroll);
            if (!view.HasTemplateBindings) throw new System.InvalidOperationException("结果模板引用不完整。");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var node = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)node.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string label, float x, Font font)
    {
        var rect = Module("Button", name, parent, new Vector2(280, 48), new Vector2(x, -170));
        SetText(rect.GetComponentInChildren<UnityEngine.UI.Text>(true), label, font, 22);
        return rect.GetComponent<UnityEngine.UI.Button>();
    }

}
