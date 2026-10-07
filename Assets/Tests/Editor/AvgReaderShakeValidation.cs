using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AvgReaderShakeValidation
{
    private const string PrefabPath = "Assets/Prefab/UI/剧本阅读器新.prefab";
    private static readonly string Request = Path.GetFullPath("Temp/AvgReaderShakeValidation.request");

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

    private static RectTransform Group(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    [MenuItem("Tests/AVG/Setup and Validate Reader Shake Layers")]
    public static void SetupAndValidate()
    {
        string output = Path.GetFullPath("Temp/AvgReaderShakeValidation/result.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        GameObject prefab = null;
        try
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Run outside Play Mode.");
            prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            var reader = prefab.GetComponent<剧本System>();
            if (prefab.transform.Find("ShakeContent") == null)
            {
                var children = prefab.transform.Cast<Transform>().ToArray();
                var positions = children.Select(t => t.position).ToArray();
                int dialogueStart = Array.FindIndex(children, t => t.name == "Dialogue");
                int foregroundStart = Array.FindIndex(children, t => t.name == "Item");
                Check(dialogueStart > 0 && foregroundStart > dialogueStart, "Unexpected reader hierarchy.");
                var content = Group(prefab.transform, "ShakeContent");
                var dialogue = Group(prefab.transform, "DialogueUI");
                var foreground = Group(prefab.transform, "ShakeForeground");
                for (int i = 0; i < children.Length; i++)
                {
                    children[i].SetParent(i < dialogueStart ? content : i < foregroundStart ? dialogue : foreground, true);
                    Check(Vector3.Distance(children[i].position, positions[i]) < 0.001f, "Reparenting changed position: " + children[i].name);
                }
                content.SetSiblingIndex(0);
                dialogue.SetSiblingIndex(1);
                foreground.SetSiblingIndex(2);
                reader.震动目标 = content;
                reader.附加震动目标 = new[] { foreground };
                EditorUtility.SetDirty(reader);
            }
            var staticUI = prefab.transform.Find("DialogueUI");
            Check(prefab.transform.childCount == 3, "Reader must have three ordered visual layers.");
            Check(reader.震动目标 == prefab.transform.Find("ShakeContent"), "Shake target binding.");
            Check(reader.附加震动目标.Length == 1 && reader.附加震动目标[0] == prefab.transform.Find("ShakeForeground"), "Foreground shake binding.");
            Check(!staticUI.IsChildOf(reader.震动目标), "Dialogue cannot be inside shaking content.");
            Check(reader.Content.transform.IsChildOf(staticUI), "Dialogue text content must stay fixed.");
            Check(reader.进度条.transform.IsChildOf(staticUI), "Reader scroll view must stay fixed.");
            Check(reader.说话人TextObject.transform.IsChildOf(staticUI), "Speaker label must stay fixed.");
            Check(staticUI.Find("btnRoot") != null, "Toolbar must stay fixed.");
            Check(reader.BG.transform.IsChildOf(reader.震动目标) && reader.SPEAKERBG.transform.IsChildOf(reader.震动目标), "Background and speaker must shake.");
            Check(reader.FULLCG.transform.IsChildOf(reader.附加震动目标[0]), "Full-screen CG must shake on foreground layer.");
            Check(prefab.transform.GetChild(0).name == "ShakeContent" && prefab.transform.GetChild(2).name == "ShakeForeground", "Visual overlay order changed.");
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            File.WriteAllText(output, "PASS: reader prefab grouped and saved using Unity Prefab API; dialogue/text/options/toolbar fixed, background/speaker/CG/foreground shake, original positions and draw order preserved.");
        }
        catch (Exception error)
        {
            File.WriteAllText(output, "FAIL: " + error);
            Debug.LogException(error);
        }
        finally
        {
            if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
