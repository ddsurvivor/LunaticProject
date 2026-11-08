using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>只创建缺失的战果提示模板，沿用 AVG 模块并保留手动布局。</summary>
public static class BattleOutcomePreviewAuthoring
{
    public const string Path = "Assets/Resources/UI/BattleOutcomePreview.prefab";
    /// <summary>创建非交互的屏幕边缘提示；不会覆盖已有模板。</summary>
    [MenuItem("Tools/Battle UI/Create Outcome Preview If Missing")]
    public static void CreateIfMissing()
    {
        if (File.Exists(Path)) return;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/AvgCommon/Panel.prefab");
        var style = Resources.Load<BattleHealthBarStyle>("UI/BattleHealthBarStyle");
        if (source == null || style?.font == null) throw new InvalidOperationException("缺少 AVG 模板或字体");
        var root = (GameObject)PrefabUtility.InstantiatePrefab(source); root.name = "BattleOutcomePreview"; root.SetActive(false);
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-24, -150); rect.sizeDelta = new Vector2(440, 270);
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true)) graphic.raycastTarget = false;
            var label = new GameObject("Summary", typeof(RectTransform), typeof(UnityEngine.UI.Text)); label.transform.SetParent(rect, false);
            var area = (RectTransform)label.transform; area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(18, 16); area.offsetMax = new Vector2(-18, -16);
            var text = label.GetComponent<UnityEngine.UI.Text>(); text.font = style.font; text.fontSize = 19; text.color = GameConst.UIColors.Text;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 16; text.resizeTextMaxSize = 19;
            text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft;
            root.AddComponent<BattleOutcomePreviewUI>().Configure(text);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            PrefabUtility.SaveAsPrefabAsset(root, Path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
