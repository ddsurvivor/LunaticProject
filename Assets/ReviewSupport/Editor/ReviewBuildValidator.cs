#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SteamReviewSupport
{
    internal sealed class ReviewBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        /// <summary>审核开发包缺少模板、剧情或 AVG 场景时中止构建，避免上传不可用的入口。</summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) == 0) return;
            if (report.summary.platform != BuildTarget.StandaloneWindows64 && report.summary.platform != BuildTarget.StandaloneWindows)
                return;
            try
            {
                string source = Path.Combine(Application.streamingAssetsPath, "ReviewSaves");
                var catalog = ReviewSaveStore.Load(source);
                if (!Array.Exists(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == "Assets/Scenes/AVG.unity"))
                    throw new InvalidDataException("Enable Assets/Scenes/AVG.unity in Build Settings.");
                string sceneText = File.ReadAllText("Assets/Scenes/AVG.unity");
                foreach (var entry in catalog.entries)
                {
                    if (Resources.Load<TextAsset>("Story/" + entry.script) == null)
                        throw new InvalidDataException("Missing story: " + entry.script);
                    if (!sceneText.Contains("m_Name: " + entry.map + "\n") && !sceneText.Contains("m_Name: " + entry.map + "\r\n"))
                        throw new InvalidDataException("Missing map name in AVG: " + entry.map);
                }
                Debug.Log($"[SteamReview] Validated {catalog.entries.Length} review entries for Windows Development Build.");
            }
            catch (Exception exception)
            {
                throw new BuildFailedException("Steam review data validation failed: " + exception.Message);
            }
        }
    }
}
#endif
