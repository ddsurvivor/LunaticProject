using UnityEditor;
using UnityEngine;

/// <summary>生成可手动编辑的头顶 UI 初始模板；不会覆盖已有 prefab。</summary>
public static class BattleOverheadTemplateAuthoring
{
    public const string PrefabPath = "Assets/Resources/UI/PieceOverheadStatus.prefab";

    /// <summary>显式修正共享模板的背景不透明度和固定像素外框，保留布局、业务绑定及变体继承。</summary>
    [MenuItem("Tools/Battle UI/Fix Compact Overhead Frame")]
    public static void FixCompactFrame()
    {
        var image = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<UnityEngine.UI.Image>();
        if (image is CompactOverheadFrame) return;
        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(image, out string _, out long localId))
            throw new System.InvalidOperationException("无法读取头顶背景组件标识。");
        string scriptGuid = AssetDatabase.AssetPathToGUID("Assets/Scripts/Battle/UI/CompactOverheadFrame.cs");
        if (string.IsNullOrEmpty(scriptGuid)) throw new System.InvalidOperationException("缺少固定斜角组件。");
        string source = System.IO.File.ReadAllText(PrefabPath);
        var block = System.Text.RegularExpressions.Regex.Match(source, @"(?ms)^--- !u!114 &" + localId + @"\r?\n.*?(?=^--- !u!|\z)");
        if (!block.Success) throw new System.InvalidOperationException("无法定位头顶背景序列化组件。");
        // 只替换该 Image 的脚本与默认值，保留 fileID，避免变体中的透明度覆盖和组件引用失效。
        string updated = System.Text.RegularExpressions.Regex.Replace(block.Value, @"(?m)^  m_Script:.*$", "  m_Script: {fileID: 11500000, guid: " + scriptGuid + ", type: 3}");
        updated = System.Text.RegularExpressions.Regex.Replace(updated, @"(?m)^(  m_Color:.*a: )[^}\r\n]+", "${1}0.7");
        System.IO.File.WriteAllText(PrefabPath, source.Substring(0, block.Index) + updated + source.Substring(block.Index + block.Length), new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        if (!(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<UnityEngine.UI.Image>() is CompactOverheadFrame))
            throw new System.InvalidOperationException("固定斜角组件导入失败。");
    }

    /// <summary>一次性升级共享模板；已升级的模板保留后续手动布局。</summary>
    [MenuItem("Tools/Battle UI/Upgrade Compact Overhead Template")]
    public static void UpgradeCompactTemplate()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var view = root.GetComponent<PieceOverheadStatusUI>();
            var config = new SerializedObject(view);
            if (config.FindProperty("compactLayoutVersion").intValue >= 1) return;
            view.ApplyCompactTemplate();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Battle UI/Create Overhead Template If Missing")]
    public static void CreateIfMissing()
    {
        if (System.IO.File.Exists(PrefabPath))
        {
            Debug.Log("头顶模板已存在，请直接编辑 prefab 或创建 Variant；生成器不会覆盖它。");
            return;
        }
        var style = Resources.Load<BattleHealthBarStyle>("UI/BattleHealthBarStyle");
        if (style == null || style.font == null) throw new System.InvalidOperationException("缺少头顶 UI 样式或字体。");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Battle/BattleScene.prefab")
            .GetComponentInChildren<PieceInfoPanel>(true)?.GetComponentInChildren<ActionPointPipsUI>(true);
        var fill = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/A美术/BattleCommonUI/BarFill.png");
        if (source == null || fill == null) throw new System.InvalidOperationException("缺少 AP 模板或填充素材。");
        var root = new GameObject("PieceOverheadStatus", typeof(RectTransform));
        try
        {
            var view = root.AddComponent<PieceOverheadStatusUI>();
            view.BuildTemplate(style.font, fill, source);
            if (!view.HasTemplateBindings) throw new System.InvalidOperationException("头顶模板绑定不完整。");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally { Object.DestroyImmediate(root); }
    }
}
