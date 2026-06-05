using System;

[Serializable]
public class PlayerSettingsData
{
    // === 1. 文字设置 ===
    public float textSpeed = 1.0f;              // 文本显示速度
    public bool showImportantBranchPrompt = true;// 是否提示重要分支
    public int languageIndex = 0;               // 显示语言 (0: 简体中文, 1: 英文 等)

    // === 2. 画面设置 ===
    public bool isFullScreen = false;
    public int resolutionIndex = 2;             // 分辨率索引
    public int targetFPSIndex = 1;              // 帧数限制索引 (如 30, 60, 无限制)
    public float brightness = 1.0f;             // 亮度
    public int particleEffectLevel = 2;         // 粒子效果程度 (0: 低, 1: 中, 2: 高)

    // === 3. 声音设置 ===
    public float masterVolume = 0.8f;
    public float bgmVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float voiceVolume = 0.8f;            // 角色语音
}