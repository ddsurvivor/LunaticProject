using UnityEditor;
using UnityEngine;
using static BattleUITemplateAuthoring;

/// <summary>首次创建暂停模板，保留 AVG 模块的嵌套 prefab 连接，不覆盖已有资产。</summary>
public static class BattlePauseTemplateAuthoring
{
    public const string PrefabPath = "Assets/Resources/UI/BattlePauseWindow.prefab";

    /// <summary>仅首次生成嵌套 AVG 模块的暂停模板；已有资产及手动覆盖保持不变。</summary>
    [MenuItem("Tools/Battle UI/Create Pause Template If Missing")]
    public static void CreateIfMissing()
    {
        if (System.IO.File.Exists(PrefabPath))
        {
            Debug.Log("暂停模板已存在，请编辑 prefab 或创建 Variant；生成器不会覆盖它。");
            return;
        }
        var style = Resources.Load<BattleHealthBarStyle>("UI/BattleHealthBarStyle");
        if (style == null || style.font == null) throw new System.InvalidOperationException("缺少 AVG 字体配置。");
        var root = new GameObject("BattlePauseWindow", typeof(RectTransform));
        root.SetActive(false);
        try
        {
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var shade = root.AddComponent<UnityEngine.UI.Image>();
            shade.color = new Color(0, 0, 0, .76f); shade.raycastTarget = true;
            root.AddComponent<ActivePause>();
            var body = Module("Panel", "PauseWindow", rect, new Vector2(620, 340), Vector2.zero);
            var heading = Module("Header", "Header", body, new Vector2(500, 56), new Vector2(0, 108));
            var title = heading.GetComponentInChildren<UnityEngine.UI.Text>(true);
            SetText(title, "战斗暂停", style.font, 30);
            var resume = Button(body, "Resume", "继续战斗", 35, style.font);
            var retry = Button(body, "Retry", "重新开始", -35, style.font);
            var quit = Button(body, "Quit", "返回主菜单", -105, style.font);
            var view = root.AddComponent<BattlePauseWindow>();
            view.Configure(body, title, resume, retry, quit);
            if (!view.HasTemplateBindings) throw new System.InvalidOperationException("暂停模板引用不完整。");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string label, float y, Font font)
    {
        var rect = Module("Button", name, parent, new Vector2(360, 48), new Vector2(0, y));
        var button = rect.GetComponent<UnityEngine.UI.Button>();
        SetText(rect.GetComponentInChildren<UnityEngine.UI.Text>(true), label, font, 22);
        return button;
    }

}
