using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Unity 2021 批处理入口：复制 AVG 风格素材，导入九宫格，并保存通用组件及速度条。</summary>
public static class SpeedTimelinePrefabBuilder
{
    const string Art = "Assets/Art/UI/AvgCommon";
    const string Components = "Assets/Prefab/UI/AvgCommon";
    const string Timeline = "Assets/Resources/UI/SpeedTimeline.prefab"; // 与运行时 Resources 加载路径对应。
    static readonly Color Gold = new Color32(219, 185, 116, 255);
    static Font font;
    static Sprite panel, title, button, selected, grid, accent, close, scroll;

    /// <summary>批处理入口：重新生成 UI 素材与预制体，校验引用并输出预览；批处理成功返回 0，失败返回 1。</summary>
    public static void BuildAndValidate()
    {
        try
        {
            Build();
            Validate();
            Preview();
            File.WriteAllText("Artifacts/UI/validation.txt", "PASS: 8 sprites, 6 reusable prefabs, speed timeline references, sliced borders and non-blocking graphics.\n");
            Debug.Log("SpeedTimeline assets generated and verified.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }

    static Sprite Import(string source, string name, Vector4 border)
    {
        string path = Art + "/" + name + ".png";
        Directory.CreateDirectory(Art);
        File.Copy("Assets/A美术/AVG新版UI/" + source + ".png", path, true);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        // 九宫格需保留完整矩形，避免紧密网格裁掉边角。
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Build()
    {
        Directory.CreateDirectory(Components);
        Directory.CreateDirectory("Assets/Resources/UI");
        Directory.CreateDirectory("Artifacts/UI");
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/SourceHanSansSC-Regular-2.otf");
        panel = Import("返回开始菜单/切角面板", "PanelCut", new Vector4(28, 28, 28, 28));
        title = Import("返回开始菜单/标题栏", "HeaderCut", new Vector4(26, 26, 26, 26));
        button = Import("对话框/选项", "ButtonNormal", new Vector4(40, 12, 28, 12));
        selected = Import("对话框/选项选中", "ButtonSelected", new Vector4(40, 12, 28, 12));
        grid = Import("返回开始菜单/细网格", "Grid", Vector4.zero);
        accent = Import("返回开始菜单/标题强调", "Accent", Vector4.zero);
        close = Import("返回开始菜单/关闭/48×48", "Close", Vector4.zero);
        scroll = Import("对话框/滚动条", "Scrollbar", Vector4.zero);

        // 先保存通用组件，再组装运行时加载的完整速度条。
        var root = Rect("Panel", null, new Vector2(560, 240));
        Image(root, panel, Color.white);
        Save(root, "Panel");
        root = Rect("Header", null, new Vector2(400, 54));
        Image(root, title, Color.white);
        Label(root, "标题", 22, new Vector2(0, 0), new Vector2(340, 40), Gold);
        Save(root, "Header");
        root = Rect("Button", null, new Vector2(280, 48));
        var background = Image(root, button, Color.white);
        background.raycastTarget = true;
        var control = root.gameObject.AddComponent<UnityEngine.UI.Button>();
        control.targetGraphic = background;
        control.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
        control.spriteState = new UnityEngine.UI.SpriteState { highlightedSprite = selected, pressedSprite = selected, selectedSprite = selected };
        Label(root, "确认", 20, Vector2.zero, new Vector2(200, 40), Color.white);
        Save(root, "Button");
        root = Rect("Separator", null, new Vector2(500, 2));
        Image(root, null, Gold * new Color(1, 1, 1, 0.5f));
        Save(root, "Separator");
        root = Rect("Track", null, new Vector2(560, 70));
        TrackDecoration(root);
        Save(root, "Track");
        var marker = CreateMarker(null);
        PrefabUtility.SaveAsPrefabAsset(marker.gameObject, Components + "/Portrait.prefab");
        UnityEngine.Object.DestroyImmediate(marker.gameObject);

        root = Rect("SpeedTimeline", null, new Vector2(800, 172));
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1);
        root.pivot = new Vector2(0.5f, 1);
        root.anchoredPosition = new Vector2(0, -18);
        Image(root, panel, Color.white);
        var layout = root.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.ignoreLayout = true;
        var header = Rect("Header", root, new Vector2(220, 32));
        header.anchorMin = header.anchorMax = new Vector2(0, 1);
        header.pivot = new Vector2(0, 1);
        header.anchoredPosition = new Vector2(18, -9);
        Image(header, title, Color.white);
        Label(header, "行动序列", 18, Vector2.zero, new Vector2(180, 28), Gold);
        Label(root, "◀", 16, new Vector2(0, -19), new Vector2(30, 24), Gold, new Vector2(0.5f, 1));
        Label(root, "速度决定行动顺序", 13, new Vector2(-120, -24), new Vector2(200, 24), new Color32(145, 160, 176, 255), new Vector2(1, 1));
        var track = Rect("Track", root, Vector2.zero);
        track.anchorMin = new Vector2(0.12f, 0.22f);
        track.anchorMax = new Vector2(0.94f, 0.73f);
        track.sizeDelta = Vector2.zero;
        TrackDecoration(track);
        Label(root, "100", 20, new Vector2(46, -76), new Vector2(60, 28), Gold, new Vector2(0, 1));
        Label(root, "行动", 13, new Vector2(46, -103), new Vector2(60, 24), Gold, new Vector2(0, 1));
        Label(root, "我方", 11, new Vector2(120, 15), new Vector2(40, 20), new Color32(97, 191, 217, 255), new Vector2(0, 0));
        Label(root, "敌方", 11, new Vector2(170, 15), new Vector2(40, 20), new Color32(230, 102, 92, 255), new Vector2(0, 0));
        Label(root, "0 · 起点", 12, new Vector2(-60, 15), new Vector2(90, 22), new Color32(145, 160, 176, 255), new Vector2(1, 0));
        marker = CreateMarker(track);
        marker.gameObject.name = "PortraitTemplate";
        // 模板仅供运行时复制，不作为实际参战者显示。
        marker.gameObject.SetActive(false);
        root.gameObject.AddComponent<SpeedTimelineUI>().Configure(track, marker);
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Timeline);
        UnityEngine.Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
    }

    static void TrackDecoration(RectTransform root)
    {
        var body = Rect("Rail", root, Vector2.zero);
        body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
        body.sizeDelta = Vector2.zero;
        Image(body, title, new Color(0.6f, 0.7f, 0.8f, 0.75f));
        for (int i = 0; i <= 10; i++)
        {
            var tick = Rect("Tick" + i, root, new Vector2(i == 0 ? 2 : 1, i % 5 == 0 ? 60 : 6));
            tick.anchorMin = tick.anchorMax = new Vector2(i / 10f, 0.5f);
            Image(tick, null, i == 0 ? Gold : new Color(0.65f, 0.72f, 0.8f, 0.25f));
        }
    }

    static SpeedTimelineMarkerUI CreateMarker(Transform parent)
    {
        var root = Rect("Portrait", parent, new Vector2(40, 40));
        root.anchorMin = root.anchorMax = new Vector2(1, 0.5f);
        Image(root, title, Color.white);
        var highlight = Rect("ActiveHighlight", root, new Vector2(46, 46));
        Image(highlight, panel, Color.white);
        var portrait = Rect("PortraitImage", root, new Vector2(34, 34));
        var face = Image(portrait, null, new Color32(40, 53, 67, 255));
        face.preserveAspect = true;
        var stripe = Rect("FactionAccent", root, new Vector2(34, 2));
        stripe.anchoredPosition = new Vector2(0, -18);
        var tint = Image(stripe, null, new Color32(97, 191, 217, 255));
        highlight.gameObject.SetActive(false);
        var marker = root.gameObject.AddComponent<SpeedTimelineMarkerUI>();
        marker.Configure(face, tint, highlight.gameObject);
        return marker;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    static UnityEngine.UI.Image Image(RectTransform rect, Sprite sprite, Color color)
    {
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = sprite; image.color = color; image.raycastTarget = false;
        image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        return image;
    }

    static void Label(RectTransform parent, string value, int size, Vector2 pos, Vector2 dimensions, Color color, Vector2? anchor = null)
    {
        var rect = Rect("Label", parent, dimensions);
        rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        var text = rect.gameObject.AddComponent<UnityEngine.UI.Text>();
        text.font = font; text.fontSize = size; text.color = color;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.alignment = TextAnchor.MiddleCenter; text.text = value; text.raycastTarget = false;
    }

    static void Save(RectTransform root, string name)
    {
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Components + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root.gameObject);
    }

    static void Validate()
    {
        foreach (string name in new[] { "PanelCut", "HeaderCut", "ButtonNormal", "ButtonSelected", "Grid", "Accent", "Close", "Scrollbar" })
            if (AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + name + ".png") == null) throw new Exception("Missing sprite " + name);
        foreach (string name in new[] { "Panel", "Header", "Button", "Separator", "Track", "Portrait" })
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Components + "/" + name + ".prefab") == null) throw new Exception("Missing component " + name);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Timeline);
        if (prefab == null || prefab.GetComponent<SpeedTimelineUI>() == null) throw new Exception("Missing timeline controller");
        var serialized = new SerializedObject(prefab.GetComponent<SpeedTimelineUI>());
        if (!serialized.FindProperty("track").objectReferenceValue || !serialized.FindProperty("markerTemplate").objectReferenceValue)
            throw new Exception("Unbound timeline references");
        // 速度条只展示信息，任何子图形都不能遮挡战斗点击。
        if (prefab.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Any(g => g.raycastTarget))
            throw new Exception("Timeline must not block clicks");
        foreach (var image in prefab.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            if (image.sprite != null && image.sprite.border != Vector4.zero && image.type != UnityEngine.UI.Image.Type.Sliced)
                throw new Exception("Bordered sprite is not sliced");
    }

    static void Preview()
    {
        // 使用独立预览场景渲染，避免向当前编辑的关卡写入展示对象。
        var scene = EditorSceneManager.NewPreviewScene();
        var canvasRoot = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasRoot, scene);
        var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        var cr = (RectTransform)canvasRoot.transform; cr.sizeDelta = new Vector2(1200, 700);
        var back = Rect("Background", cr, cr.sizeDelta); Image(back, null, new Color32(13, 20, 26, 255));
        Label(cr, "AVG  /  通用 UI 组件", 26, new Vector2(0, 290), new Vector2(1000, 45), Gold);
        var timeline = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Timeline), cr, false);
        var tr = (RectTransform)timeline.transform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f); tr.anchoredPosition = new Vector2(0, 150);
        var template = timeline.GetComponentInChildren<SpeedTimelineMarkerUI>(true);
        for (int i = 0; i < 6; i++)
        {
            var marker = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
            marker.gameObject.SetActive(true); marker.SetState(i == 0, i % 2 == 0);
            marker.Portrait.sprite = Resources.Load<Sprite>("CG/PC0" + (i % 3 + 1) + "C1");
            marker.Portrait.color = Color.white;
            marker.Rect.anchoredPosition = new Vector2(-656 + i * 120, i % 2 == 0 ? 22 : -22);
        }
        string[] names = { "Panel", "Header", "Button", "Separator", "Track", "Portrait" };
        for (int i = 0; i < names.Length; i++)
        {
            float x = (i % 3 - 1) * 350; float y = -60 - i / 3 * 160;
            var obj = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Components + "/" + names[i] + ".prefab"), cr, false);
            var r = (RectTransform)obj.transform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            if (names[i] == "Panel") r.sizeDelta = new Vector2(300, 95);
            if (names[i] == "Header") r.sizeDelta = new Vector2(300, 54);
            if (names[i] == "Track") r.sizeDelta = new Vector2(300, 70);
            if (names[i] == "Separator") r.sizeDelta = new Vector2(300, 2);
            if (names[i] == "Portrait")
            {
                var m = obj.GetComponent<SpeedTimelineMarkerUI>();
                m.Portrait.sprite = Resources.Load<Sprite>("CG/PC01C1");
                m.Portrait.color = Color.white;
            }
            Label(cr, names[i], 14, new Vector2(x, y - 66), new Vector2(300, 24), Gold);
        }
        var camGo = new GameObject("PreviewCamera", typeof(Camera));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
        var cam = camGo.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 350;
        cam.scene = scene;
        cam.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        canvas.worldCamera = cam;
        cam.transform.position = new Vector3(0, 0, -1000); cam.farClipPlane = 2000;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var rt = new RenderTexture(1200, 700, 24); cam.targetTexture = rt;
        Canvas.ForceUpdateCanvases(); cam.Render();
        var previous = RenderTexture.active; RenderTexture.active = rt;
        var texture = new Texture2D(1200, 700, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, 1200, 700), 0, 0); texture.Apply();
        File.WriteAllBytes("Artifacts/UI/SpeedTimelinePreview.png", texture.EncodeToPNG());
        RenderTexture.active = previous; cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(texture); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        EditorSceneManager.ClosePreviewScene(scene);
    }
}
