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
    [SerializeField] private Text textSpeedValueText; 
    [SerializeField] private Toggle branchPromptToggle;
    [SerializeField] private Dropdown languageDropdown;

    [Header("UI - Video Settings SubPage")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown fpsDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Text brightnessValueText; 
    [SerializeField] private Dropdown particleDropdown;

    [Header("UI - Audio Settings SubPage")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text masterVolumeValueText; 
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Text bgmVolumeValueText;    
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Text sfxVolumeValueText;    
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Text voiceVolumeValueText;  

    [Header("Common UI & Actions")]
    [SerializeField] private Button saveButton;            
    [SerializeField] private Button restoreDefaultsButton; 
    [SerializeField] private Text statusText;

    private void Start()
    {
        InitTabs();
        BindSliderEvents(); 
        LoadAndShowSettings();

        /*// 按钮监听事件的安全保护
        if (saveButton != null) 
            saveButton.onClick.AddListener(SaveAndApplySettings);
            
        if (restoreDefaultsButton != null) 
            restoreDefaultsButton.onClick.AddListener(RestoreDefaultSettings);*/
    }

    #region 选项卡切换逻辑 (Tab System)

    private void InitTabs()
    {
        if (tabs == null) return; // 规避列表未初始化的风险

        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; 
            // 保护：如果某个选项卡的按钮没拖，跳过它，不影响其他按钮
            if (tabs[index].tabButton != null)
            {
                tabs[index].tabButton.onClick.AddListener(() => SwitchTab(index));
            }
        }
        SwitchTab(0); 
    }

    public void SwitchTab(int targetIndex)
    {
        if (tabs == null || targetIndex < 0 || targetIndex >= tabs.Count) return;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == targetIndex);
            
            // 保护：子页面可以为空（比如某些页面还没做出来）
            if (tabs[i].subPage != null) 
                tabs[i].subPage.SetActive(isActive);

            // 保护：标签底图和 Sprite 资源的安全性校验
            if (tabs[i].tabBgImage != null)
            {
                if (isActive && activeTabSprite != null) tabs[i].tabBgImage.sprite = activeTabSprite;
                else if (!isActive && inactiveTabSprite != null) tabs[i].tabBgImage.sprite = inactiveTabSprite;
            }
        }
    }

    #endregion

    #region 数据同步与实时显示逻辑

    private void BindSliderEvents()
    {
        // 已经具备标准的组件存在性检查，确保未配置的 Slider 不会引发监听报错
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

    private void UpdateSliderText(float value, Text targetText, DisplayType type)
    {
        if (targetText == null) return; // 保护：即使不拖入对应的数值 Text，Slider 也能正常拖动测试

        switch (type)
        {
            case DisplayType.Percentage:
                targetText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";
                break;
            case DisplayType.Multiplier:
                targetText.text = value.ToString("F1") + "x";
                break;
        }
    }

    public void LoadAndShowSettings()
    {
        // 核心底线保护：如果直接在主菜单场景单玩、且没有通过初始化场景启动 GM，直接拦截，防止崩溃
        if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.DM.settingsData == null)
        {
            Debug.LogWarning("[SettingPanel] 未检测到全局管理器单例(GM/DM)，当前处于离线测试状态。");
            return;
        }

        PlayerSettingsData currentSettings = GM.Ins.DM.settingsData;

        // ==========================================
        // 1. 刷新文字页面（所有组件各自独立保护）
        // ==========================================
        if (textSpeedSlider != null) textSpeedSlider.value = currentSettings.textSpeed;
        // 优化：传入数据值而非 Slider.value，这样即使 Slider 组件没做，Text 组件也能单测显示数据
        UpdateSliderText(currentSettings.textSpeed, textSpeedValueText, DisplayType.Multiplier);
        
        if (branchPromptToggle != null) branchPromptToggle.isOn = currentSettings.showImportantBranchPrompt;
        if (languageDropdown != null) languageDropdown.value = currentSettings.languageIndex;

        // ==========================================
        // 2. 刷新画面页面（所有组件各自独立保护）
        // ==========================================
        if (fullScreenToggle != null) fullScreenToggle.isOn = currentSettings.isFullScreen;
        if (resolutionDropdown != null) resolutionDropdown.value = currentSettings.resolutionIndex;
        if (fpsDropdown != null) fpsDropdown.value = currentSettings.targetFPSIndex;
        
        if (brightnessSlider != null) brightnessSlider.value = currentSettings.brightness;
        UpdateSliderText(currentSettings.brightness, brightnessValueText, DisplayType.Percentage);
        
        if (particleDropdown != null) particleDropdown.value = currentSettings.particleEffectLevel;

        // ==========================================
        // 3. 刷新声音页面（所有组件各自独立保护）
        // ==========================================
        if (masterVolumeSlider != null) masterVolumeSlider.value = currentSettings.masterVolume;
        UpdateSliderText(currentSettings.masterVolume, masterVolumeValueText, DisplayType.Percentage);
        
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = currentSettings.bgmVolume;
        UpdateSliderText(currentSettings.bgmVolume, bgmVolumeValueText, DisplayType.Percentage);
        
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = currentSettings.sfxVolume;
        UpdateSliderText(currentSettings.sfxVolume, sfxVolumeValueText, DisplayType.Percentage);
        
        if (voiceVolumeSlider != null) voiceVolumeSlider.value = currentSettings.voiceVolume;
        UpdateSliderText(currentSettings.voiceVolume, voiceVolumeValueText, DisplayType.Percentage);
    }

    public void SaveAndApplySettings()
    {
        // 拦截保护
        if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.DM.settingsData == null) return;

        PlayerSettingsData dataToUpdate = GM.Ins.DM.settingsData;

        // 工业级无痛存储：只有被拖入场景的 UI 组件才会更新数据。
        // 未配置的 UI 组件将被跳过，它们在配置数据文件（JSON）中的原有值将完美保留，不会被清零或报错！
        if (textSpeedSlider != null) dataToUpdate.textSpeed = textSpeedSlider.value;
        if (branchPromptToggle != null) dataToUpdate.showImportantBranchPrompt = branchPromptToggle.isOn;
        if (languageDropdown != null) dataToUpdate.languageIndex = languageDropdown.value;

        if (fullScreenToggle != null) dataToUpdate.isFullScreen = fullScreenToggle.isOn;
        if (resolutionDropdown != null) dataToUpdate.resolutionIndex = resolutionDropdown.value;
        if (fpsDropdown != null) dataToUpdate.targetFPSIndex = fpsDropdown.value;
        if (brightnessSlider != null) dataToUpdate.brightness = brightnessSlider.value;
        if (particleDropdown != null) dataToUpdate.particleEffectLevel = particleDropdown.value;

        if (masterVolumeSlider != null) dataToUpdate.masterVolume = masterVolumeSlider.value;
        if (bgmVolumeSlider != null) dataToUpdate.bgmVolume = bgmVolumeSlider.value;
        if (sfxVolumeSlider != null) dataToUpdate.sfxVolume = sfxVolumeSlider.value;
        if (voiceVolumeSlider != null) dataToUpdate.voiceVolume = voiceVolumeSlider.value;

        GM.Ins.DM.ApplySettings(dataToUpdate);

        if (statusText != null) statusText.text = "所有设置已成功应用！";
    }

    public void RestoreDefaultSettings()
    {
        if (GM.Ins == null || GM.Ins.DM == null) return;

        PlayerSettingsData defaultData = new PlayerSettingsData();
        GM.Ins.DM.ApplySettings(defaultData);
        LoadAndShowSettings(); 

        if (statusText != null) statusText.text = "已成功恢复至初始默认设置。";
    }

    #endregion
}