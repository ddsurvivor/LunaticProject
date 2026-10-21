using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>在真正的 Unity 编辑器中创建预制体，避免手写组件序列化数据。</summary>
public static class BattleAuthoringPrefabBuilder
{
    public const string Folder = "Assets/Prefab/Battle/Authoring";

    /// <summary>生成搭建预制体资源；场景搭建使用三个独立的添加菜单。</summary>
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefab/Battle", "Authoring");
        Sprite marker = CreateMarker();
        Sprite cover = LoadSprite(AssetDatabase.GUIDToAssetPath("ae27a69373b73824da7f771cc71ea1b9"));
        Sprite floor = LoadSprite("Assets/A美术/CombatMap/再生号空间站/RespawnFloor1_001.png");
        Save<BattleObstacle>("障碍物掩体", c => { c.sprite = cover != null ? cover : marker; c.Refresh(); });
        Save<BattlePlatform>("高层平台", c => { c.topSprite = floor != null ? floor : marker; c.Refresh(); });
        Save<BattleDeploymentArea>("角色出生区域", c => { c.sprite = marker; c.Refresh(); });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("战斗搭建：三个预制体已保存至 " + Folder);
    }

    /// <summary>向当前编辑场景添加障碍物，支持撤销并自动选中。</summary>
    [MenuItem("Tools/战斗搭建/添加障碍物掩体")]
    public static void AddObstacle() => AddToScene<BattleObstacle>("障碍物掩体");

    /// <summary>向当前编辑场景添加高层平台，支持撤销并自动选中。</summary>
    [MenuItem("Tools/战斗搭建/添加高层平台")]
    public static void AddPlatform() => AddToScene<BattlePlatform>("高层平台");

    /// <summary>向当前编辑场景添加角色出生区域，支持撤销并自动选中。</summary>
    [MenuItem("Tools/战斗搭建/添加角色出生区域")]
    public static void AddDeploymentArea() => AddToScene<BattleDeploymentArea>("角色出生区域");

    private static void AddToScene<T>(string name) where T : BattlePlaceable
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("添加" + name);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".prefab");
        if (prefab == null)
        {
            Build();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + name + ".prefab");
        }
        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(root, "添加" + name);
        var view = SceneView.lastActiveSceneView;
        Vector3 position = view != null ? view.pivot : Vector3.zero;
        position.y = 0;
        root.transform.position = position;
        root.GetComponent<T>().Refresh();
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        if (view != null) view.FrameSelected();
    }

    private static Sprite LoadSprite(string path) => string.IsNullOrEmpty(path) ? null :
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

    private static void Save<T>(string name, Action<T> configure) where T : BattlePlaceable
    {
        var root = new GameObject(name);
        try
        {
            var component = root.AddComponent<T>();
            configure(component);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Sprite CreateMarker()
    {
        string path = Folder + "/DeploymentRange.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    bool border = x < 3 || x > 124 || y < 3 || y > 124;
                    bool grid = x % 16 == 0 || y % 16 == 0;
                    pixels[y * 128 + x] = new Color32(255, 255, 255, border ? (byte)255 : grid ? (byte)100 : (byte)45);
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 128;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
