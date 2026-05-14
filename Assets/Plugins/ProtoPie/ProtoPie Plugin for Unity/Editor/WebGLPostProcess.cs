using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class WebGLBuildPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.WebGL)
        {
            string buildPath = report.summary.outputPath;
            CopyMappingFileToBuild(buildPath);
        }
    }

    private void CopyMappingFileToBuild(string buildPath)
    {
        // Path to the source JSON file within your Unity project
        string sourcePath = Path.Combine(Application.dataPath, "ProtoPieUnity/mappingTable.asset");
        
        // Destination path within the build directory 
        string destPath = Path.Combine(buildPath, "Build/mappingTable.asset");

        // Ensure the destination directory exists
        string destDir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Copy the file
        File.Copy(sourcePath, destPath, overwrite: true);

        Debug.Log("JSON file copied to build folder: " + destPath);
    }
}
