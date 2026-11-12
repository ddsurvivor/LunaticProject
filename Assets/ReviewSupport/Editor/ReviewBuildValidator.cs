#if UNITY_EDITOR
using System.IO;
using UnityEditor.Build;
using UnityEngine;

namespace SteamReviewSupport
{
    internal sealed class ReviewBuildValidator : BuildPlayerProcessor
    {
        public override int callbackOrder => 0;

        /// <summary>所有 Player 构建均禁止携带调试存档；发现源目录残留时中止构建。</summary>
        public override void PrepareForBuild(BuildPlayerContext context)
        {
            string streamingAssets = Application.streamingAssetsPath;
            // 调试模板只保留在 Editor 目录，避免 Unity 自动将误放的文件复制进包。
            if (Directory.Exists(Path.Combine(streamingAssets, "ReviewSaves")) ||
                (Directory.Exists(streamingAssets) &&
                 Directory.GetFiles(streamingAssets, "PlayerProfiles_*.json", SearchOption.AllDirectories).Length > 0))
                throw new BuildFailedException("StreamingAssets 中存在调试存档，请将其移至 Assets/ReviewSupport/Editor/DevelopmentStreamingAssets 后重新构建。");
        }
    }
}
#endif
