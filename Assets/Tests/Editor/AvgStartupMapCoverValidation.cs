using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AvgStartupMapCoverValidation
{
    private const string ScenePath = "Assets/Scenes/AVG.unity";
    private static readonly string Request = Path.GetFullPath("Temp/AvgStartupMapCoverValidation.request");

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.update -= CheckRequest;
        EditorApplication.update += CheckRequest;
    }

    private static void CheckRequest()
    {
        if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        File.Delete(Request);
        SetupAndValidate();
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    [MenuItem("Tests/AVG/Setup and Validate Startup Map Cover")]
    public static void SetupAndValidate()
    {
        string output = Path.GetFullPath("Temp/AvgStartupMapCoverValidation/result.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Run outside Play Mode.");
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            Check(!scene.isDirty, "AVG has unsaved edits; save them before running setup.");
            var manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<大地图System>(true)).Single();
            var canvas = manager.mapRoot.GetComponentInParent<Canvas>();
            Check(canvas != null && canvas.name == "大地图Canvas", "Expected map Canvas.");
            var dialogueCanvas = manager.剧情.GetComponentInParent<Canvas>();
            Check(dialogueCanvas != canvas && dialogueCanvas.sortingOrder > canvas.sortingOrder, "Dialogue must render above map cover.");
            var existing = canvas.transform.Find("StartupMapCover");
            var cover = existing != null ? existing.gameObject : new GameObject("StartupMapCover", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            cover.layer = canvas.gameObject.layer;
            var rect = cover.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            rect.SetAsLastSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            var image = cover.GetComponent<UnityEngine.UI.Image>();
            image.sprite = null;
            image.color = Color.black;
            image.raycastTarget = true;
            cover.SetActive(true);
            var serialized = new SerializedObject(manager);
            serialized.FindProperty("startupMapCover").objectReferenceValue = cover;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Check(rect.GetSiblingIndex() == canvas.transform.childCount - 1, "Cover must be in front of map UI.");
            Check(rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.offsetMin == Vector2.zero && rect.offsetMax == Vector2.zero, "Cover must fill the screen.");
            Check(image.color == Color.black && image.raycastTarget && cover.activeSelf, "Cover must default to opaque black and block map clicks.");
            manager.HideStartupMapCover();
            Check(!cover.activeSelf, "Manager must hide the cover when dialogue is ready.");
            cover.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);
            Check(EditorSceneManager.SaveScene(scene), "Could not save AVG scene.");
            File.WriteAllText(output, "PASS: default active, opaque black, full screen, front of map, behind dialogue, blocks clicks, manager hides cover, scene saved.");
        }
        catch (Exception error)
        {
            File.WriteAllText(output, "FAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
