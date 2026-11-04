using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>将历史角色内嵌技能迁入统一技能库，保留各条配置而不按同名技能合并。</summary>
public static class CharacterSkillCatalogMigration
{
    /// <summary>仅恢复已迁移角色丢失的技能库引用；先核对全部 ID，不重建技能、不覆盖已有技能库或角色配置。</summary>
    [MenuItem("Tools/技能配置/恢复丢失的角色技能库绑定")]
    public static void RestoreMissingLibraryBindings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var library = AssetDatabase.LoadAssetAtPath<SkillPackListSO>("Assets/ScriptableObjects/SkillPackListSO.asset");
        if (library == null || library.skillPacks == null) return;
        var ids = library.skillPacks.Where(s => s != null).Select(s => s.skillId).ToArray();
        if (ids.Any(string.IsNullOrEmpty) || ids.Distinct().Count() != ids.Length)
        {
            Debug.LogError("技能库存在空 ID 或重复 ID，停止恢复角色绑定。");
            return;
        }
        var sources = AssetDatabase.FindAssets("t:PieceDataListSO")
            .Select(guid => AssetDatabase.LoadAssetAtPath<PieceDataListSO>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(source => source != null && source.skillsMigrated && source.pieceDataList != null).ToArray();
        // 全量预检后再修改，避免部分恢复或将缺失技能误绑到同名配置。
        foreach (var source in sources)
            foreach (var piece in source.pieceDataList.Where(p => p != null && p.skillLibrary == null))
            {
                var references = new[] { piece.meleeSkillId, piece.rangedSkillId }
                    .Concat(piece.initialSkillIds ?? new System.Collections.Generic.List<string>())
                    .Where(id => !string.IsNullOrEmpty(id));
                foreach (string id in references)
                    if (library.GetById(id) == null)
                    {
                        Debug.LogError($"{source.name} / {piece.pieceId} 的技能 {id} 不在目录中，停止恢复。");
                        return;
                    }
            }
        int restored = 0;
        foreach (var source in sources)
        {
            var missing = source.pieceDataList.Where(p => p != null && p.skillLibrary == null &&
                (!string.IsNullOrEmpty(p.meleeSkillId) || !string.IsNullOrEmpty(p.rangedSkillId) || p.initialSkillIds?.Count > 0)).ToArray();
            if (missing.Length == 0) continue;
            Undo.RecordObject(source, "恢复角色技能库绑定");
            foreach (var piece in missing) piece.skillLibrary = library;
            EditorUtility.SetDirty(source);
            AssetDatabase.SaveAssetIfDirty(source);
            restored += missing.Length;
        }
        Debug.Log($"已恢复 {restored} 条角色技能库绑定，技能 ID 和效果配置未修改。");
    }

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
