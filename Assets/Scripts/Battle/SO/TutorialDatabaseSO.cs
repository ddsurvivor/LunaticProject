using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Tutorial/Tutorial Database", fileName = "Tutorial Database")]
public class TutorialDatabaseSO : ScriptableObject
{
    

    [Header("所有教程配置（只需在这里添加/修改）")]
    public List<TutorialData> allTutorials = new List<TutorialData>();

    // 内部缓存，查找超快
    private Dictionary<string, TutorialData> _lookup;

    /// <summary>
    /// 【推荐接口】根据关卡名称获取教程数据
    /// </summary>
    public TutorialData GetTutorial(string levelName)
    {
        if (string.IsNullOrEmpty(levelName)) return null;

        // 首次使用时构建字典缓存
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, TutorialData>(allTutorials.Count);
            foreach (var tut in allTutorials)
            {
                if (!string.IsNullOrEmpty(tut.triggerLevelName))
                    _lookup[tut.triggerLevelName] = tut;
            }
        }

        _lookup.TryGetValue(levelName, out var data);
        return data;
    }

    /// <summary>
    /// 可选接口：检查某个关卡是否有教程
    /// </summary>
    public bool HasTutorial(string levelName)
    {
        return GetTutorial(levelName) != null;
    }
}

[System.Serializable]
public class TutorialData
{
    [Header("触发关卡名称（必须和Scene.name完全一致）")]
    public string triggerLevelName;

    [Header("教程页面列表")]
    public List<TutorialPage> pages = new List<TutorialPage>();
}

[System.Serializable]
public class TutorialPage
{
    [Header("页面图片")]
    [PreviewField]
    public Sprite image;
    [Header("标题文字")]
    //[TextArea(4, 12)]
    public string title;
    [Header("页面文字")]
    [TextArea(4, 12)]
    public string description;
}