using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BindingProbe
{
    static BindingProbe() { EditorApplication.delayCall += Run; }
    /// <summary>记录实际绑定，调用正式恢复入口后校验全部引用及初始装配。</summary>
    public static void Run()
    {
        try
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Run;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists("tmp/SkillBindingRepair/reimported"))
            {
                File.WriteAllText("tmp/SkillBindingRepair/reimported", "requested");
                AssetDatabase.ImportAsset("Assets/ScriptableObjects/SkillPackListSO.asset", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset("Assets/ScriptableObjects/PieceDataListSO.asset", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            }
            var library = AssetDatabase.LoadAssetAtPath<SkillPackListSO>("Assets/ScriptableObjects/SkillPackListSO.asset");
            var pieces = AssetDatabase.LoadAssetAtPath<PieceDataListSO>("Assets/ScriptableObjects/PieceDataListSO.asset");
            File.WriteAllText("tmp/SkillBindingRepair/after-reimport.txt", "Missing libraries=" + pieces.pieceDataList.Count(p => p.skillLibrary == null));
            string configuration = string.Join("\n", pieces.pieceDataList.Select(p => p.pieceId + ":" + p.meleeSkillId + ":" + p.rangedSkillId + ":" + string.Join(",", p.initialSkillIds)));
            string skillConfiguration = JsonUtility.ToJson(library);
            CharacterSkillCatalogMigration.RestoreMissingLibraryBindings();
            if (configuration != string.Join("\n", pieces.pieceDataList.Select(p => p.pieceId + ":" + p.meleeSkillId + ":" + p.rangedSkillId + ":" + string.Join(",", p.initialSkillIds)))) throw new Exception("Binding IDs changed");
            if (skillConfiguration != JsonUtility.ToJson(library)) throw new Exception("Skill definitions changed");
            var lines = new System.Collections.Generic.List<string> { "Playing=" + EditorApplication.isPlaying, "Library=" + library.skillPacks.Count };
            lines.Add("AssetGuid=" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(library)));
            var serialized = new SerializedObject(pieces);
            var refs = serialized.FindProperty("serializationData.ReferencedUnityObjects");
            for (int i = 0; i < refs.arraySize; i++) lines.Add("REF " + i + "=" + AssetDatabase.GetAssetPath(refs.GetArrayElementAtIndex(i).objectReferenceValue));
            lines.Add("ENTRY=" + (int)Sirenix.Serialization.EntryType.ExternalReferenceByIndex);
            lines.AddRange(library.skillPacks.Select(s => "SKILL " + s.skillId + " " + s.skillName));
            lines.AddRange(pieces.pieceDataList.Select(p => $"PIECE {p.pieceId} {p.pieceName} library={p.skillLibrary != null} melee={p.meleeSkillId}:{p.MeleeAttack != null} ranged={p.rangedSkillId}:{p.RangedAttack != null} initial={string.Join(",", p.initialSkillIds)} resolved={p.DefaultSkills.Count}"));
            int attacks = 0, active = 0;
            foreach (var p in pieces.pieceDataList)
            {
                if (p.skillLibrary != library) throw new Exception("Missing library: " + p.pieceId);
                foreach (string id in new[] { p.meleeSkillId, p.rangedSkillId }.Where(id => !string.IsNullOrEmpty(id)))
                {
                    if (library.GetById(id) == null) throw new Exception("Missing attack: " + id);
                    attacks++;
                }
                if (p.DefaultSkills.Count != p.initialSkillIds.Count) throw new Exception("Missing initial skill: " + p.pieceId);
                var loadout = new SkillLoadoutState();
                loadout.Initialize(p);
                if (loadout.Resolve(library).Count != p.initialSkillIds.Count) throw new Exception("Missing loadout: " + p.pieceId);
                active += p.DefaultSkills.Count;
            }
            lines.Add($"PASS pieces={pieces.pieceDataList.Count} attacks={attacks} active={active} library={library.skillPacks.Count}; IDs/effects unchanged; fresh loadouts resolved");
            File.WriteAllLines("tmp/SkillBindingRepair/live.txt", lines);
        }
        catch (Exception e) { File.WriteAllText("tmp/SkillBindingRepair/live.txt", e.ToString()); }
    }
}
