using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.IO;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using MiniExcelLibs; // 需要引入 MiniExcel 命名空间

public class ExtensionlessExcelEditor : OdinMenuEditorWindow
{
    // 指定你存放无后缀 Excel 文件的文件夹路径
    private const string TargetFolderPath = "Assets/StreamingAssets"; 

    [MenuItem("Tools/剧本查看器")]
    private static void OpenWindow()
    {
        var window = GetWindow<ExtensionlessExcelEditor>();
        window.titleContent = new GUIContent("Excel 矩阵编辑器");
        window.Show();
    }

    /// <summary>
    /// 构建左侧导航栏
    /// </summary>
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Config.DrawSearchToolbar = true; // 开启搜索框

        if (!Directory.Exists(TargetFolderPath))
        {
            Directory.CreateDirectory(TargetFolderPath);
            AssetDatabase.Refresh();
        }

        // 使用物理路径扫描所有文件，过滤掉 Unity 的 .meta 文件
        string[] files = Directory.GetFiles(TargetFolderPath)
                                  .Where(f => !f.EndsWith(".meta"))
                                  .ToArray();

        foreach (var filePath in files)
        {
            // 过滤掉 Unity 的 meta 文件
            if (filePath.EndsWith(".meta")) continue;

            string fileName = Path.GetFileName(filePath);
            // 检查文件是否合法（至少要有 4 个字节，且头两个字节是 "PK"）
            if (!IsZipFile(filePath))
            {
                Debug.LogWarning($"[Excel管理器] 跳过非合法压缩包文件: {Path.GetFileName(filePath)}");
                continue; 
            }

            // 【关键改动 1】使用 CreateInstance 在内存中动态创建 ScriptableObject 包装器
            // 这样能确保右侧窗口 100% 识别并完美渲染出来
            var excelWrapper = ScriptableObject.CreateInstance<ExcelFileWrapper>();
            excelWrapper.Initialize(filePath);
            
            tree.Add(fileName, excelWrapper);
        }

        return tree;
    }
    
    // 辅助方法：判断是不是 ZIP/XLSX 文件
    private static bool IsZipFile(string path)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Length < 4) return false;
                int b1 = fs.ReadByte();
                int b2 = fs.ReadByte();
                return b1 == 0x50 && b2 == 0x4B; // 0x50 0x4B 就是 "PK"
            }
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 【核心改动 1】定义一个专属于单元格的结构体
/// </summary>
[System.Serializable]
public struct ExcelCell
{
    [HideLabel]
    //[TextArea(5, 10)] // 核心：最少显示2行，最多5行。这会直接撑大 TableMatrix 的行高，并天然支持自动换行！
    [MultiLineProperty(10)]
    [PreviewField(Height = 20)]
    public string text;
}


/// <summary>
/// 【关键改动 2】继承自 ScriptableObject，使其成为标准的 Unity 检查器目标
/// </summary>
public class ExcelFileWrapper : ScriptableObject
{
    [HideInInspector]
    public string filePath;

    // 【关键改动 3】必须显式加上 [ShowInInspector] 特性！
    // 强制 Odin 穿透 Unity 的限制，去渲染非序列化的二维数组 string[,]
    [ShowInInspector]
    [TableMatrix(HorizontalTitle = "Excel 数据矩阵", SquareCells = true,ResizableColumns = true,DrawElementMethod = "DrawElement")]
    public ExcelCell[,] DataMatrix;

    // ScriptableObject 不能使用带参构造函数，改用 Initialize 函数进行初始化
    public void Initialize(string path)
    {
        filePath = path;
        LoadExcelData();
    }

    private void LoadExcelData()
    {
        if (!File.Exists(filePath)) return;

        try
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            
            using (var stream = new MemoryStream(fileBytes))
            {
                DataTable dt = stream.QueryAsDataTable(useHeaderRow: false, excelType: ExcelType.XLSX);
                
                int excelRows = dt.Rows.Count;
                int excelCols = dt.Columns.Count;

                if (excelRows == 0 || excelCols == 0)
                {
                    DataMatrix = new ExcelCell[5, 5]; 
                    return;
                }

                // 【改动 3】数据转置：矩阵的行数 = Excel的列数，矩阵的列数 = Excel的行数
                DataMatrix = new ExcelCell[excelCols, excelRows];
                for (int i = 0; i < excelRows; i++)
                {
                    for (int j = 0; j < excelCols; j++)
                    {
                        // 原本是 [i, j]，互换为 [j, i] 显示
                        DataMatrix[j, i] = new ExcelCell { text = dt.Rows[i][j]?.ToString() ?? "" };
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"解析文件 {Path.GetFileName(filePath)} 失败: {ex.Message}");
            DataMatrix = new ExcelCell[2, 2];
        }
    }
    
    /// <summary>
    /// 【核心核心】Odin 调用的自定义单元格绘制方法
    /// </summary>
    private static ExcelCell DrawElement(Rect rect, ExcelCell value)
    {
        // 1. 实例化一个 TextArea 的样式，并强制开启自动换行
        GUIStyle cellStyle = new GUIStyle(EditorStyles.textArea);
        cellStyle.wordWrap = true;       // 开启自动换行
        cellStyle.fontSize = 12;         // 设定一个舒适的字号
        cellStyle.alignment = TextAnchor.UpperLeft;

        // 2. 监听改动状态
        EditorGUI.BeginChangeCheck();

        // 3. 在 Odin 分配给我们的长方形 rect 区域内，使用 EditorGUI.TextArea 绘制文本
        string newText = EditorGUI.TextArea(rect, value.text, cellStyle);

        if (EditorGUI.EndChangeCheck())
        {
            value.text = newText;
        }

        return value;
    }

    /*[Button("保存修改", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.2f)]
    public void SaveExcelData()
    {
        if (DataMatrix == null) return;

        var dt = new DataTable();
        int rowCount = DataMatrix.GetLength(0);
        int colCount = DataMatrix.GetLength(1);

        for (int j = 0; j < colCount; j++)
        {
            dt.Columns.Add($"Col_{j}");
        }

        for (int i = 0; i < rowCount; i++)
        {
            var row = dt.NewRow();
            for (int j = 0; j < colCount; j++)
            {
                row[j] = DataMatrix[i, j];
            }
            dt.Rows.Add(row);
        }

        using (var memoryStream = new MemoryStream())
        {
            memoryStream.SaveAs(dt, useHeaderRow: false, excelType: ExcelType.XLSX);
            File.WriteAllBytes(filePath, memoryStream.ToArray());
        }

        Debug.Log($"<color=green>【Excel 编辑器】</color> 已成功保存修改至: {filePath}");
        AssetDatabase.Refresh();
    }*/
}