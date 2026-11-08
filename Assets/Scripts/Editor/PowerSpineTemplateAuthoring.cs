using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using static BattleUITemplateAuthoring;

/// <summary>生成战外设施的共享模板与侧栏入口；只创建缺失资产，保留后续手动布局。</summary>
public static class PowerSpineTemplateAuthoring
{
    public const string PanelPath = "Assets/Resources/UI/PowerSpinePanel.prefab";
    public const string EntryPath = "Assets/Resources/UI/PowerSpineEntry.prefab";

    /// <summary>使用 AVG 嵌套模块创建设施 UI；已有模板和 Variant 不被覆盖。</summary>
    [MenuItem("Tools/Facilities/Create Power Spine Templates If Missing")]
    public static void CreateIfMissing()
    {
        var style = Resources.Load<BattleHealthBarStyle>("UI/BattleHealthBarStyle");
        if (style == null || style.font == null) throw new InvalidOperationException("缺少 AVG 字体配置");
        if (!File.Exists(EntryPath))
        {
            var entry = Module("Button", "PowerSpineEntry", null, new Vector2(1152, 160), Vector2.zero);
            try
            {
                entry.anchorMin = entry.anchorMax = new Vector2(.5f, 0); entry.anchoredPosition = new Vector2(0, -140);
                var label = entry.GetComponentInChildren<UnityEngine.UI.Text>(true);
                SetText(label, "动力脊设施", style.font, 80);
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(80, 0); label.rectTransform.offsetMax = new Vector2(-80, 0);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
                PrefabUtility.RecordPrefabInstancePropertyModifications(label);
                PrefabUtility.RecordPrefabInstancePropertyModifications(entry);
                PrefabUtility.SaveAsPrefabAsset(entry.gameObject, EntryPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(entry.gameObject); }
        }
        if (File.Exists(PanelPath)) return;
        var root = new GameObject("PowerSpinePanel", typeof(RectTransform)); root.SetActive(false);
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, .8f);
            var body = Module("Panel", "Window", rect, new Vector2(760, 560), Vector2.zero);
            var header = Module("Header", "Header", body, new Vector2(660, 60), new Vector2(0, 220));
            SetText(header.GetComponentInChildren<UnityEngine.UI.Text>(true), "动力脊设施", style.font, 30);
            var details = Label("Details", body, new Vector2(650, 260), new Vector2(0, 45), style.font, 23);
            var feedback = Label("Feedback", body, new Vector2(650, 58), new Vector2(0, -115), style.font, 20);
            var upgrade = Button("Upgrade", "升级动力脊", body, new Vector2(-170, -185), style.font);
            var save = Button("Save", "打开存档", body, new Vector2(170, -185), style.font);
            var close = Button("Close", "关闭", body, new Vector2(0, -245), style.font);
            var panel = root.AddComponent<PowerSpinePanel>(); panel.Configure(details, feedback, upgrade, close, save);
            if (!panel.HasTemplateBindings) throw new InvalidOperationException("设施模板引用不完整");
            PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
    }

    private static UnityEngine.UI.Button Button(string name, string label, Transform parent, Vector2 position, Font font)
    {
        var rect = Module("Button", name, parent, new Vector2(300, 44), position);
        SetText(rect.GetComponentInChildren<UnityEngine.UI.Text>(true), label, font, 22);
        return rect.GetComponent<UnityEngine.UI.Button>();
    }
    private static UnityEngine.UI.Text Label(string name, Transform parent, Vector2 size, Vector2 position, Font font, int fontSize)
    {
        var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        var rect = (RectTransform)node.transform; rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
        var text = node.GetComponent<UnityEngine.UI.Text>(); SetText(text, "", font, fontSize); text.alignment = TextAnchor.MiddleLeft;
        return text;
    }
}
