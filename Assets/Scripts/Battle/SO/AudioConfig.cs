using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector; // 引入 Odin 命名空间

/// <summary>
/// 音频提示类型枚举（显式赋值，防止序列化错乱）
/// </summary>
public enum AudioCueType
{
    [LabelText("点击")] Click = 10,
    [LabelText("关闭")] Close = 20,
    [LabelText("鼠标移入")] Hover = 30,
    [LabelText("展开")] Expand = 40,
    [LabelText("收起")] Collapse = 50,
    [LabelText("确认")] Confirm = 60,
    [LabelText("取消")] Cancel = 70,
    [LabelText("选中")] Select = 80,
    // 聚能充满、启动、退出
    [LabelText("聚能充满")] ChargeFull = 90,
    [LabelText("聚能启动")] ChargeStart = 100,
    [LabelText("聚能退出")] ChargeExit = 110,
}

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Config/Audio Config")]
public class AudioConfig : SerializedScriptableObject
{
    [Title("UI 音效配置表")]
    [DictionaryDrawerSettings(KeyLabel = "音频提示类型", ValueLabel = "资源路径 / 键值")]
    [SerializeField]
    private Dictionary<AudioCueType, string> audioMap = new Dictionary<AudioCueType, string>();

    /// <summary>
    /// 根据音频提示类型获取对应的 string 路径
    /// </summary>
    public string GetAudioPath(AudioCueType type)
    {
        if (audioMap == null)
        {
            Debug.LogError("[AudioConfig] 音频字典未实例化！");
            return string.Empty;
        }

        if (audioMap.TryGetValue(type, out string path))
        {
            return path;
        }

        Debug.LogWarning($"[AudioConfig] 未找到音频提示类型 {type} 对应的配置。");
        return string.Empty;
    }

#if UNITY_EDITOR
    //[Button("自动填充所有音频类型", ButtonSizes.Medium)]
    [GUIColor(0.3f, 0.8f, 0.3f)] 
    private void PopulateAllEnumTypes()
    {
        if (audioMap == null) audioMap = new Dictionary<AudioCueType, string>();
        
        foreach (AudioCueType type in System.Enum.GetValues(typeof(AudioCueType)))
        {
            if (!audioMap.ContainsKey(type))
            {
                audioMap.Add(type, string.Empty);
            }
        }
    }
#endif
}