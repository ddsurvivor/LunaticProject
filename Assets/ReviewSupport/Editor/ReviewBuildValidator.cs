#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SteamReviewSupport
{
    internal sealed class ReviewBuildValidator : BuildPlayerProcessor
    {
        private const string DevelopmentAssets = "Assets/ReviewSupport/Editor/DevelopmentStreamingAssets";

        public override int callbackOrder => 0;

        /// <summary>仅向开发包加入预置存档；Windows 开发包同时校验审核入口。</summary>
        public override void PrepareForBuild(BuildPlayerContext context)
        {
            var options = context.BuildPlayerOptions;
            if ((options.options & BuildOptions.Development) == 0) return;
            // 从 Editor 目录按文件加入，避免正式包或开发包携带源文件 meta。
            foreach (string directory in new[] { "Datas", "ReviewSaves" })
                foreach (string file in Directory.GetFiles(Path.Combine(DevelopmentAssets, directory)))
                    if (!file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        context.AddAdditionalPathToStreamingAssets(file, directory + "/" + Path.GetFileName(file));
            if (options.target != BuildTarget.StandaloneWindows64 && options.target != BuildTarget.StandaloneWindows)
                return;
            try
            {
                string source = Path.Combine(DevelopmentAssets, "ReviewSaves");
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
