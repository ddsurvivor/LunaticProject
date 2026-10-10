using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// 构建前登记音频资源，并确保随游戏更新 Addressables。
/// </summary>
public class AudioAddressablesBuildProcessor : BuildPlayerProcessor
{
    // Addressables 的构建回调顺序为 1，音频检查必须先执行。
    public override int callbackOrder => 0;

    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null || settings.DefaultGroup == null)
            throw new BuildFailedException("缺少 Addressables 配置，无法构建音频资源。");

        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        var addresses = new HashSet<string>(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles("Assets/SOUND", "*", SearchOption.AllDirectories))
        {
            string extension = Path.GetExtension(file).ToLowerInvariant();
            if (extension != ".wav" && extension != ".mp3" && extension != ".ogg" &&
                extension != ".aif" && extension != ".aiff") continue;

            string path = file.Replace('\\', '/');
            // 旧音频从 StreamingAssets 移入后，可能仍保留 DefaultImporter。
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                throw new BuildFailedException($"无法导入音频：{path}");

            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                entry.address = Path.GetFileNameWithoutExtension(path);
                entry.SetLabel("SOUND", true, true);
            }

            if (!addresses.Add(entry.address))
                throw new BuildFailedException($"音频 Addressables 地址重复：{entry.address}");

            var schema = entry.parentGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null || !schema.IncludeInBuild || !schema.IncludeAddressInCatalog)
                throw new BuildFailedException($"音频分组未开启构建或地址收录：{entry.parentGroup.Name}");
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioAddressables] 已检查 {addresses.Count} 个音频地址，将随游戏重新构建资源包。");
    }
}
