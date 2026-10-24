using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>将历史角色内嵌技能迁入统一技能库，保留各条配置而不按同名技能合并。</summary>
public static class CharacterSkillCatalogMigration
{
    /// <summary>迁移所有角色 SO；只在编辑模式运行，可重复执行且不覆盖已拆分的技能引用。</summary>
    [MenuItem("Tools/技能配置/迁移角色技能到统一技能库")]
    public static void Migrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Migrate;
            return;
        }
        var library = AssetDatabase.LoadAssetAtPath<SkillPackListSO>("Assets/ScriptableObjects/SkillPackListSO.asset");
        if (library == null) return;
        foreach (var skill in library.skillPacks.ToArray()) library.Register(skill);
        foreach (string guid in AssetDatabase.FindAssets("t:PieceDataListSO"))
        {
            var source = AssetDatabase.LoadAssetAtPath<PieceDataListSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (source == null || source.skillsMigrated) continue;
            // 先完成全部角色的转存，再写入版本标记，避免下一次导入重复添加技能。
            Undo.RecordObjects(new UnityEngine.Object[] { source, library }, "迁移角色技能");
            foreach (var piece in source.pieceDataList) piece?.MigrateSkills(library);
            source.skillsMigrated = true;
            EditorUtility.SetDirty(library);
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssetIfDirty(library);
            AssetDatabase.SaveAssetIfDirty(source);
            Debug.Log($"{source.name} 的角色技能已迁入技能库，共 {library.skillPacks.Count} 条技能配置。");
        }
    }
}
