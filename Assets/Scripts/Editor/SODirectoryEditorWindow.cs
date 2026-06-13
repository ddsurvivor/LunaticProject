using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using System.IO;

public class SODirectoryEditorWindow : OdinMenuEditorWindow
{
    // 指定要读取的文件夹路径
    private const string TargetFolderPath = "Assets/ScriptableObjects";

    [MenuItem("Tools/SO 批量管理器")]
    private static void OpenWindow()
    {
        var window = GetWindow<SODirectoryEditorWindow>();
        window.titleContent = new GUIContent("SO 管理器");
        window.Show();
    }

    /// <summary>
    /// 构建左侧的菜单树
    /// </summary>
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        
        // 配置菜单树属性：支持搜索框
        tree.Config.DrawSearchToolbar = true;

        // 检查目标文件夹是否存在
        if (!AssetDatabase.IsValidFolder(TargetFolderPath))
        {
            Debug.LogWarning($"目标路径不存在: {TargetFolderPath}，请先创建该文件夹。");
            return tree;
        }

        // 寻找指定文件夹下的所有 ScriptableObject 资源
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { TargetFolderPath });

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);

            if (so != null)
            {
                // 计算相对路径，使左侧导航栏可以保留子文件夹的层级结构
                string relativePath = assetPath.Substring(TargetFolderPath.Length).TrimStart('/');
                // 移除文件后缀名 .asset
                string menuPath = Path.Combine(Path.GetDirectoryName(relativePath), Path.GetFileNameWithoutExtension(relativePath));
                // 将标准化的斜杠用于菜单路径
                menuPath = menuPath.Replace("\\", "/");

                // 将 SO 资源添加到左侧导航树中
                tree.Add(menuPath, so);
            }
        }

        // 默认按名称排序（可选）
        tree.SortMenuItemsByName();

        return tree;
    }

    /// <summary>
    /// 在窗口顶部绘制一个工具栏（用于刷新和保存）
    /// </summary>
    protected override void OnBeginDrawEditors()
    {
        // 获取当前选中的菜单项
        OdinMenuItem selectedItem = this.MenuTree.Selection.SelectedValue as OdinMenuItem;

        // 开始绘制顶部横向工具栏
        SirenixEditorGUI.BeginHorizontalToolbar();
        {
            // 弹性空白，把按钮推到右边
            GUILayout.FlexibleSpace();

            // 刷新按钮：重新生成左侧菜单树
            if (SirenixEditorGUI.ToolbarButton("刷新列表"))
            {
                this.ForceMenuTreeRebuild();
            }

            // 保存按钮：强制序列化并保存所有未保存的资产
            if (SirenixEditorGUI.ToolbarButton("保存所有修改"))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("所有修改已保存！");
            }
        }
        SirenixEditorGUI.EndHorizontalToolbar();
    }
}