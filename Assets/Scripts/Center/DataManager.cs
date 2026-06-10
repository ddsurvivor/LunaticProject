using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using SkillSystem;
using UnityEngine;
using UnityEngine.Audio;

public class DataManager : SerializedMonoBehaviour
{
    [OdinSerialize]
    public Dictionary<int, PLAYERPROFILE> playerprofiles = new();
    private string savePath = Application.streamingAssetsPath + "/Datas/";
    // 发布时改为 Application.persistentDataPath + "/Datas/";
    private int saveSlotCount = 10;
    public PlayerSettingsData settingsData;// 游戏设置数据，全局公用
    
    public LevelUpConfig levelUpConfig;
    public ComponentConfig componentConfig;
    public PieceDataListSO pieceDataListSO;
    public PassiveSkillConfigSO passiveSkillConfigSO;
    public void Init()
    {
        // 测试加载
        //PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + "PlayerProfiles_0.json");
        LoadData();
    }

    public void LoadData()
    {
        playerprofiles.Clear();
        for (int i = 0; i < saveSlotCount; i++)
        {
            int j = i;
            PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + $"PlayerProfiles_{j}.json");
            if(playerprofile == null) continue;
            playerprofiles.Add(j,playerprofile);
        }
        settingsData = JsonTool.LoadJson<PlayerSettingsData>(savePath + "PlayerSettingsData.json");
        if (settingsData == null)
        {
            InitSettings();
        }
        ApplySettings(settingsData);// 加载后立即应用设置
    }

    public PLAYERPROFILE LoadData(int index)
    {
        PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + $"PlayerProfiles_{index}.json");
        return playerprofile;
    }

    public void SaveData(int index)
    {
        //playerprofiles[index] = GM.Ins.PLAYERPROFILE;
        JsonTool.SaveJson(GM.Ins.PLAYERPROFILE,savePath + $"PlayerProfiles_{index}.json");
        playerprofiles[index] = JsonTool.LoadJson<PLAYERPROFILE>(savePath + $"PlayerProfiles_{index}.json");
    }
    [Button("测试保存")]
    public void TestSave(int index)
    {
        JsonTool.SaveJson(GM.Ins.PLAYERPROFILE,savePath + $"PlayerProfiles_{index}.json");
    }
    // [Button("ES3测试保存")]
    // public void ES3SaveDate(int index)
    // {
    //     string path = savePath + $"PlayerProfiles_{index}.json";
    //     ES3.Save("PlayerProfile", GM.Ins.PLAYERPROFILE, path);
    // }



    #region 游戏设置存储

    /// <summary>
    /// 初始化设置
    /// </summary>
    public void InitSettings()
    {
        settingsData = new PlayerSettingsData();
        JsonTool.SaveJson(settingsData, savePath + "PlayerSettingsData.json");
    }
    /*public void ApplySettings(PlayerSettingsData settingsData)
    {
        // 实际应用画质设置
        QualitySettings.SetQualityLevel(settingsData.qualityLevel);
        // 实际应用全屏设置
        Screen.fullScreen = settingsData.isFullScreen;
        // 应用音量 (通常配合 AudioMixer 使用)
        AudioListener.volume = settingsData.masterVolume;
        
        Debug.Log("设置已应用到引擎");
        JsonTool.SaveJson(settingsData,savePath + "PlayerSettingsData.json");
    }*/
    
    
    
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer mainMixer; // 需在 Mixer 中右键 Volume 暴露变量
    private const string MASTER_PARAM = "MasterVolume";
    private const string BGM_PARAM = "BGMVolume";
    private const string SFX_PARAM = "SFXVolume";
    private const string VOICE_PARAM = "VoiceVolume";
    [Header("Video Presets (Strict 16:9)")]
    // 严格限制 16:9 分辨率阵列
    private readonly List<Vector2Int> resolutionPresets = new List<Vector2Int>()
    {
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080),
        new Vector2Int(2560, 1440),
        new Vector2Int(3840, 2160),
        new Vector2Int(1920,1200),
        new Vector2Int(1440,1024),
    };

    // === 核心事件广播机制 ===
    // 游戏内其他模块（如对话组件、UI组件）只需订阅这些事件即可实时响应设置变更
    public static event Action<float> OnTextSpeedChanged;
    public static event Action<bool> OnBranchPromptChanged;
    public static event Action<int> OnLanguageChanged;

    private Color defaultAmbientColor; // 记录初始化时的环境光，用于动态调整亮度
    /// <summary>
    /// 核心方法：消化3类设置数据并进行全局底层硬件/引擎应用
    /// </summary>
    public void ApplySettings(PlayerSettingsData newData)
    {
        if (newData == null) return;
        settingsData = newData;

        // ==========================================
        // 1. 文字设置实装 (通过事件向外通知)
        // ==========================================
        // 如果外部有系统订阅了这些事件，则立刻触发对应逻辑
        OnTextSpeedChanged?.Invoke(settingsData.textSpeed);
        OnBranchPromptChanged?.Invoke(settingsData.showImportantBranchPrompt);
        OnLanguageChanged?.Invoke(settingsData.languageIndex);


        // ==========================================
        // 2. 画面设置实装 (底层物理切换)
        // ==========================================
        Screen.fullScreen = settingsData.isFullScreen;
        // A. 分辨率与全屏判定
        int resIndex = Mathf.Clamp(settingsData.resolutionIndex, 0, resolutionPresets.Count - 1);
        Vector2Int targetRes = resolutionPresets[resIndex];
        Screen.SetResolution(targetRes.x, targetRes.y, settingsData.isFullScreen);

        // B. 帧数限制实装
        switch (settingsData.targetFPSIndex)
        {
            case 0: Application.targetFrameRate = 30; break;
            case 1: Application.targetFrameRate = 60; break;
            case 2: Application.targetFrameRate = -1; break; // -1 代表不限帧（解锁垂直同步后生效）
            default: Application.targetFrameRate = 60; break;
        }

        // C. 画质/粒子效果联动
        // 直接切换 Project Settings -> Quality 中的工业档位 (0:Low, 1:Medium, 2:High)
        QualitySettings.SetQualityLevel(settingsData.particleEffectLevel, true);

        // D. 亮度控制实装 (通用环境光叠加算法)
        // 限制在 0.5 到 1.5 之间。通过缩放全局环境光改变画面感官亮度
        float finalBrightness = Mathf.Clamp(settingsData.brightness, 0.5f, 1.5f);
        RenderSettings.ambientLight = defaultAmbientColor * finalBrightness;


        // ==========================================
        // 3. 声音设置实装 (对数分贝转换)
        // ==========================================
        SetMixerVolume(MASTER_PARAM, settingsData.masterVolume);
        SetMixerVolume(BGM_PARAM, settingsData.bgmVolume);
        SetMixerVolume(SFX_PARAM, settingsData.sfxVolume);
        SetMixerVolume(VOICE_PARAM, settingsData.voiceVolume);


        // ==========================================
        // 4. 持久化存储写入
        // ==========================================
        try
        {
            JsonTool.SaveJson(settingsData,savePath + "PlayerSettingsData.json");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DM] 写入硬盘故障，请检查读写权限: {e.Message}");
        }
    }
    /// <summary>
    /// 音频线性滑块值转对数分贝公式函数
    /// </summary>
    private void SetMixerVolume(string exposedParam, float sliderValue)
    {
        if (mainMixer == null) return;

        // 核心公式：dB = log10(Value) * 20
        // 当 Slider 接近 0 时强制降到 -80dB 实现物理静音，防止 log10(0) 报错
        float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        mainMixer.SetFloat(exposedParam, dB);
    }
    
    #endregion
}
