#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNITY_STANDALONE_WIN)
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace SteamReviewSupport
{
    internal sealed class ReviewBuildRuntime : MonoBehaviour
    {
        private ReviewCatalog catalog;
        private string sourceDirectory;
        private string saveDirectory;
        private UnityEngine.UI.Text helpText;
        private GameObject helpPanel;
        private string status = "Starting review support...";

        /// <summary>在编辑器 Play Mode 或 Windows Development Player 首场景 Awake 前安装审核档；无需场景挂载。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            var root = new GameObject("SteamReviewSupport");
            DontDestroyOnLoad(root);
            root.AddComponent<ReviewBuildRuntime>().Initialize();
        }

        private void Initialize()
        {
            sourceDirectory = Path.Combine(Application.streamingAssetsPath, "ReviewSaves");
            saveDirectory = Path.Combine(Application.persistentDataPath, "Datas");
            try
            {
                catalog = ReviewSaveStore.Load(sourceDirectory);
                foreach (var entry in catalog.entries)
                    if (Resources.Load<TextAsset>("Story/" + entry.script) == null)
                        throw new InvalidDataException("Missing story: " + entry.script);
                int installed = ReviewSaveStore.Install(sourceDirectory, saveDirectory, catalog, false);
                Report($"Ready. Installed {installed} missing review saves. Existing saves preserved.");
            }
            catch (Exception exception)
            {
                catalog = null;
                Report("Review setup failed: " + exception.Message);
                Debug.LogException(exception);
            }
            CreateHelp();
        }

        private void Update()
        {
            if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;
            if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) return;
            if (DigitPressed(1)) helpPanel.SetActive(!helpPanel.activeSelf);
            if (catalog == null) return;
            try
            {
                if (DigitPressed(9))
                {
                    if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.IsTransitioning) return;
                    int installed = ReviewSaveStore.Install(sourceDirectory, saveDirectory, catalog, true);
                    GM.Ins.DM.LoadData();
                    foreach (var panel in FindObjectsOfType<SavePanel>()) panel.UpdateDisplay();
                    Report($"Restored {installed} review saves. Previous files backed up in Datas/ReviewBackups.");
                }
                if (DigitPressed(0))
                {
                    Directory.CreateDirectory(saveDirectory);
                    Application.OpenURL(new Uri(saveDirectory + Path.DirectorySeparatorChar).AbsoluteUri);
                }
                foreach (var entry in catalog.entries)
                    if (DigitPressed(entry.shortcutNumber)) Jump(entry);
            }
            catch (Exception exception)
            {
                Report("Review action failed: " + exception.Message);
                Debug.LogException(exception);
            }
        }

        private static bool DigitPressed(int digit)
        {
            return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + digit)) ||
                   Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + digit));
        }

        private void Jump(ReviewEntry entry)
        {
            if (GM.Ins == null || GM.Ins.DM == null || !GM.Ins.CanLoadScene("AVG"))
            {
                Report("Please wait until the title screen or current transition is ready.");
                return;
            }
            // 每次反序列化原始模板，不依赖已被游玩修改的用户槽位。
            var profile = JsonConvert.DeserializeObject<PLAYERPROFILE>(ReviewSaveStore.ReadTemplate(sourceDirectory, entry));
            if (大地图System.instance != null && 大地图System.instance.剧情 != null)
                大地图System.instance.剧情.EndReadingStory();
            PlayingSystem.特殊剧情 = "";
            Time.timeScale = 1f;
            GM.Ins.PLAYERPROFILE = profile;
            GM.Ins.LoadPlayingScene("AVG");
            Report("Loading " + entry.title + " (" + entry.script + "). Unsaved progress was replaced.");
        }

        private void Report(string message)
        {
            status = message;
            Debug.Log("[SteamReview] " + message + " Save directory: " + saveDirectory);
            RefreshHelp();
        }

        private void CreateHelp()
        {
            var canvasObject = new GameObject("ReviewHelpCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            helpPanel = new GameObject("ReviewHelp", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            helpPanel.transform.SetParent(canvasObject.transform, false);
            var rect = helpPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-16, -16);
            rect.sizeDelta = new Vector2(650, 450);
            var background = helpPanel.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.025f, 0.035f, 0.055f, 0.94f);
            background.raycastTarget = false;
            var textObject = new GameObject("Instructions", typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(helpPanel.transform, false);
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18, 16);
            textRect.offsetMax = new Vector2(-18, -16);
            helpText = textObject.GetComponent<UnityEngine.UI.Text>();
            helpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            helpText.fontSize = 21;
            helpText.color = Color.white;
            helpText.alignment = TextAnchor.UpperLeft;
            helpText.raycastTarget = false;
            helpText.supportRichText = false;
            RefreshHelp();
        }

        private void RefreshHelp()
        {
            if (helpText == null) return;
            string mode = Application.isEditor ? "EDITOR PLAY MODE" : "DEVELOPMENT BUILD";
            string text = "STEAM REVIEW / " + mode + "\nCtrl+Alt+1: Hide/show this guide\n";
            if (catalog != null)
                foreach (var entry in catalog.entries)
                    text += $"Ctrl+Alt+{entry.shortcutNumber}: {entry.title} [slot {entry.slot + 1:00}]\n";
            helpText.text = text + "Ctrl+Alt+9: Restore review slots (backs up existing files)\n" +
                "Ctrl+Alt+0: Open save folder\nJumps discard UNSAVED progress; disk saves stay intact.\n\n" + status;
        }
    }
}
#endif
