using UnityEngine;
using System.Collections.Generic;

// 1. 定义音频枚举类型
public enum SoundType
{
    Click=1,      // 点击音
    Success=2,    // 成功/完成
    Warning=3,    // 警告
    Error=4,      // 错误/失败
    Popup=5,      // 弹窗出现
    Cancel=6,      // 取消/返回
    MouseOn = 7,    // 鼠标移入
}

public class AudioFxManager : MonoBehaviour
{
    
    // 2. 定义一个简单的辅助类用于在 Inspector 中配置映射
    [System.Serializable]
    public class AudioMapping
    {
        public SoundType type;
        public AudioClip clip;
    }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource; // 用于播放音效的组件

    // 3. 运行时的查询字典
    private Dictionary<SoundType, AudioClip> audioDict = new Dictionary<SoundType, AudioClip>();
    

    /// <summary>
    /// 接口：触发音效播放
    /// </summary>
    /// <param name="type">音效枚举种类</param>
    /// <param name="volumeScale">音量缩放 (0-1)</param>
    public void PlaySFX(SoundType type, float volumeScale = 1.0f)
    {
        if (audioDict.TryGetValue(type, out AudioClip clip))
        {
            // PlayOneShot 允许重叠播放，不会切断正在播放的同类声音
            sfxSource.PlayOneShot(clip, volumeScale);
            //AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volumeScale);
        }
        else
        {
            Debug.LogWarning($"AudioManager: 未找到类型为 {type} 的音频配置！");
        }
    }
}