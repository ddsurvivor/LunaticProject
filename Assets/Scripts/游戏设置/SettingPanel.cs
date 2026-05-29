using UnityEngine;
using UnityEngine.UI; // 严格使用旧版 UI
using System.Collections.Generic;

public class SettingPanel : MonoBehaviour
{
    [System.Serializable]
    public struct TabItem
    {
        public Button tabButton;       // 选项卡按钮
        public Image tabBgImage;       // 选项卡底图组件
        public GameObject subPage;     // 对应的子页面 GameObject
    }

    [Header("Tab System")]
    [SerializeField] private List<TabItem> tabs;
    [SerializeField] private Sprite activeTabSprite;   
    [SerializeField] private Sprite inactiveTabSprite; 

    [Header("UI - Text Settings SubPage")]
    [SerializeField] private Slider textSpeedSlider;
    [SerializeField] private Text textSpeedValueText; // 新增：文字速度数值显示
    [SerializeField] private Toggle branchPromptToggle;
    [SerializeField] private Dropdown languageDropdown;

    [Header("UI - Video Settings SubPage")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown fpsDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Text brightnessValueText; // 新增：亮度数值显示
    [SerializeField] private Dropdown particleDropdown;

    [Header("UI - Audio Settings SubPage")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text masterVolumeValueText; // 新增：主音量数值显示
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Text bgmVolumeValueText;    // 新增：BGM音量数值显示
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Text sfxVolumeValueText;    // 新增：SFX音量数值显示
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Text voiceVolumeValueText;  // 新增：语音音量数值显示

    [Header("Common UI & Actions")]
    [SerializeField] private Button saveButton;            
    [SerializeField] private Button restoreDefaultsButton; 
    [SerializeField] private Text statusText;

    private void Start()
    {
        InitTabs();
        BindSliderEvents(); // 新增：绑定滑块实时监听
        LoadAndShowSettings();

        if (saveButton != null) 
            saveButton.onClick.AddListener(SaveAndApplySettings);
            
        if (restoreDefaultsButton != null) 
            restoreDefaultsButton.onClick.AddListener(RestoreDefaultSettings);
    }

    #region 选项卡切换逻辑 (Tab System)

    private void InitTabs()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; 
            tabs[i].tabButton.onClick.AddListener(() => SwitchTab(index));
        }
        SwitchTab(0); 
    }

    public void SwitchTab(int targetIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == targetIndex);
            if (tabs[i].subPage != null) tabs[i].subPage.SetActive(isActive);
            if (tabs[i].tabBgImage != null) tabs[i].tabBgImage.sprite = isActive ? activeTabSprite : inactiveTabSprite;
        }
    }

    #endregion

    #region 数据同步与实时显示逻辑

    /// <summary>
    /// 新增：动态监听所有滑块的拖拽状态
    /// </summary>
    private void BindSliderEvents()
    {
        // 当玩家拖动滑块时，立刻触发文本更新（使用 Lambda 表达式简化代码）
        if (textSpeedSlider != null)
            textSpeedSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, textSpeedValueText, DisplayType.Multiplier));

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, brightnessValueText, DisplayType.Percentage));

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, masterVolumeValueText, DisplayType.Percentage));

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, bgmVolumeValueText, DisplayType.Percentage));

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, sfxVolumeValueText, DisplayType.Percentage));

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, voiceVolumeValueText, DisplayType.Percentage));
    }

    private enum DisplayType { Percentage, Multiplier }

    /// <summary>
    /// 通用辅助函数：格式化并更新文本显示
    /// </summary>
    private void UpdateSliderText(float value, Text targetText, DisplayType type)
    {
        if (targetText == null) return;

        switch (type)
        {
            case DisplayType.Percentage:
                // 0.8f -> 80%
                targetText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";
                break;
            case DisplayType.Multiplier:
                // 1.25f -> 1.3x
                targetText.text = value.ToString("F1") + "x";
                break;
        }
    }

    public void LoadAndShowSettings()
    {
        PlayerSettingsData currentSettings = GM.Ins.DM.settingsData;
        if (currentSettings == null) return;

        // 1. 刷新文字页面并强制刷新文本
        textSpeedSlider.value = currentSettings.textSpeed;
        UpdateSliderText(textSpeedSlider.value, textSpeedValueText, DisplayType.Multiplier);
        branchPromptToggle.isOn = currentSettings.showImportantBranchPrompt;
        languageDropdown.value = currentSettings.languageIndex;

        // 2. 刷新画面页面并强制刷新文本
        fullScreenToggle.isOn = currentSettings.isFullScreen;
        resolutionDropdown.value = currentSettings.resolutionIndex;
        fpsDropdown.value = currentSettings.targetFPSIndex;
        brightnessSlider.value = currentSettings.brightness;
        UpdateSliderText(brightnessSlider.value, brightnessValueText, DisplayType.Percentage);
        particleDropdown.value = currentSettings.particleEffectLevel;

        // 3. 刷新声音页面并强制刷新文本
        masterVolumeSlider.value = currentSettings.masterVolume;
        UpdateSliderText(masterVolumeSlider.value, masterVolumeValueText, DisplayType.Percentage);
        
        bgmVolumeSlider.value = currentSettings.bgmVolume;
        UpdateSliderText(bgmVolumeSlider.value, bgmVolumeValueText, DisplayType.Percentage);
        
        sfxVolumeSlider.value = currentSettings.sfxVolume;
        UpdateSliderText(sfxVolumeSlider.value, sfxVolumeValueText, DisplayType.Percentage);
        
        voiceVolumeSlider.value = currentSettings.voiceVolume;
        UpdateSliderText(voiceVolumeSlider.value, voiceVolumeValueText, DisplayType.Percentage);
    }

    public void SaveAndApplySettings()
    {
        PlayerSettingsData dataToUpdate = GM.Ins.DM.settingsData;

        dataToUpdate.textSpeed = textSpeedSlider.value;
        dataToUpdate.showImportantBranchPrompt = branchPromptToggle.isOn;
        dataToUpdate.languageIndex = languageDropdown.value;

        dataToUpdate.isFullScreen = fullScreenToggle.isOn;
        dataToUpdate.resolutionIndex = resolutionDropdown.value;
        dataToUpdate.targetFPSIndex = fpsDropdown.value;
        dataToUpdate.brightness = brightnessSlider.value;
        dataToUpdate.particleEffectLevel = particleDropdown.value;

        dataToUpdate.masterVolume = masterVolumeSlider.value;
        dataToUpdate.bgmVolume = bgmVolumeSlider.value;
        dataToUpdate.sfxVolume = sfxVolumeSlider.value;
        dataToUpdate.voiceVolume = voiceVolumeSlider.value;

        GM.Ins.DM.ApplySettings(dataToUpdate);

        if (statusText != null) statusText.text = "所有设置已成功应用！";
    }

    public void RestoreDefaultSettings()
    {
        PlayerSettingsData defaultData = new PlayerSettingsData();
        GM.Ins.DM.ApplySettings(defaultData);
        LoadAndShowSettings(); // 内部会同步刷新所有文本数值

        if (statusText != null) statusText.text = "已成功恢复至初始默认设置。";
    }

    #endregion
}