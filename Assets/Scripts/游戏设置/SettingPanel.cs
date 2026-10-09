using UnityEngine;
using UnityEngine.UI; // 严格使用旧版 UI
using System.Collections.Generic;

/// <summary>显示游戏设置，并保存玩家的修改。</summary>
public class SettingPanel : UIPanel
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
    [SerializeField] private int initialTabIndex;

    [Header("UI - Text Settings SubPage")]
    [SerializeField] private Slider textSpeedSlider;
    [SerializeField] private Text textSpeedValueText; 
    [SerializeField] private Slider autoPlayDelaySlider;
    [SerializeField] private Text autoPlayDelayValueText;
    [SerializeField] private Slider fastForwardIntervalSlider;
    [SerializeField] private Text fastForwardIntervalValueText;
    [SerializeField] private Toggle branchPromptToggle;
    [SerializeField] private Dropdown languageDropdown;

    [Header("UI - Video Settings SubPage")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Dropdown fpsDropdown;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Text brightnessValueText; 
    [SerializeField] private Dropdown particleDropdown;
    [SerializeField] private Slider fpsSlider;
    [SerializeField] private Text fpsValueText;
    [SerializeField] private Toggle particleToggle;
    [SerializeField] private Text pageTitleText;
    [SerializeField] private Text pageCounterText;
    [SerializeField] private GameObject[] selectedTabVisuals;

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
        ConfigureResolutionDropdown();
        InitTabs();
        BindSliderEvents(); 
        LoadAndShowSettings();

        /*// 按钮监听事件的安全保护
        if (saveButton != null) 
            saveButton.onClick.AddListener(SaveAndApplySettings);
            
        if (restoreDefaultsButton != null) 
            restoreDefaultsButton.onClick.AddListener(RestoreDefaultSettings);*/
    }

    /// <summary>调整分辨率选项行高，避免文字被模板裁切。</summary>
    private void ConfigureResolutionDropdown()
    {
        if (resolutionDropdown == null || resolutionDropdown.template == null || resolutionDropdown.itemText == null)
            return;

        // 新 UI 使用 22 号字，旧模板的行高仍为 20，Text 的垂直裁切会隐藏整行文字。
        var label = resolutionDropdown.itemText;
        // Dropdown 的模板平时处于隐藏状态，父级查询必须包含未激活节点。
        var item = label.GetComponentInParent<Toggle>(true);
        if (item == null || !(item.transform is RectTransform itemRect)) return;
        float rowHeight = Mathf.Max(itemRect.rect.height, label.fontSize * Mathf.Max(1f, label.lineSpacing) + 16f);
        itemRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
        if (itemRect.parent is RectTransform content)
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.raycastTarget = false;
        resolutionDropdown.RefreshShownValue();
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
        SwitchTab(initialTabIndex);
    }

    public void SwitchTab(int targetIndex)
    {
        if (tabs == null || targetIndex < 0 || targetIndex >= tabs.Count) return;

        if (pageTitleText != null)
            pageTitleText.text = "设置 / " + new[] { "语言设置", "图像设置", "声音设置" }[Mathf.Clamp(targetIndex, 0, 2)];
        if (pageCounterText != null) pageCounterText.text = $"{targetIndex + 1:00} / {tabs.Count:00}";

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == targetIndex);
            if (selectedTabVisuals != null && i < selectedTabVisuals.Length && selectedTabVisuals[i] != null)
                selectedTabVisuals[i].SetActive(isActive);
            
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
        foreach (var toggle in new[] { fullScreenToggle, particleToggle })
        {
            if (toggle == null) continue;
            var caption = toggle.GetComponentInChildren<Text>();
            if (caption != null)
                toggle.onValueChanged.AddListener(value => caption.text = value ? "已开启" : "已关闭");
        }
        if (fpsSlider != null)
            fpsSlider.onValueChanged.AddListener(UpdateFpsText);
        // 已经具备标准的组件存在性检查，确保未配置的 Slider 不会引发监听报错
        if (textSpeedSlider != null)
            textSpeedSlider.onValueChanged.AddListener((val) => UpdateSliderText(val, textSpeedValueText, DisplayType.Multiplier));

        if (autoPlayDelaySlider != null)
            autoPlayDelaySlider.onValueChanged.AddListener(val => UpdateSliderText(val, autoPlayDelayValueText, DisplayType.Seconds));
        if (fastForwardIntervalSlider != null)
            fastForwardIntervalSlider.onValueChanged.AddListener(val => UpdateSliderText(val, fastForwardIntervalValueText, DisplayType.Seconds));

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

    private enum DisplayType { Percentage, Multiplier, Seconds }

    private void UpdateFpsText(float value)
    {
        if (fpsValueText != null)
            fpsValueText.text = new[] { "30", "60", "无限制" }[Mathf.Clamp(Mathf.RoundToInt(value), 0, 2)];
    }

    private void UpdateSliderText(float value, Text targetText, DisplayType type)
    {
        if (targetText == null) return; // 保护：即使不拖入对应的数值 Text，Slider 也能正常拖动测试

        switch (type)
        {
            case DisplayType.Percentage:
                targetText.text = Mathf.RoundToInt(value * 100f).ToString() + "%";
                break;
            case DisplayType.Seconds:
                targetText.text = value.ToString("F1") + "s";
                break;
            case DisplayType.Multiplier:
                targetText.text = value.ToString("F1") + "x";
                break;
        }
    }

    /// <summary>读取当前设置，并刷新各项控件。</summary>
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
        
        if (autoPlayDelaySlider != null) autoPlayDelaySlider.value = currentSettings.autoPlayDelay;
        if (fastForwardIntervalSlider != null) fastForwardIntervalSlider.value = currentSettings.fastForwardInterval;
        UpdateSliderText(currentSettings.autoPlayDelay, autoPlayDelayValueText, DisplayType.Seconds);
        UpdateSliderText(currentSettings.fastForwardInterval, fastForwardIntervalValueText, DisplayType.Seconds);

        if (branchPromptToggle != null) branchPromptToggle.isOn = currentSettings.showImportantBranchPrompt;
        if (languageDropdown != null) languageDropdown.value = currentSettings.languageIndex;

        // ==========================================
        // 2. 刷新画面页面（所有组件各自独立保护）
        // ==========================================
        if (fullScreenToggle != null) fullScreenToggle.isOn = currentSettings.isFullScreen;
        if (resolutionDropdown != null) resolutionDropdown.value = currentSettings.resolutionIndex;
        if (fpsDropdown != null) fpsDropdown.value = currentSettings.targetFPSIndex;
        if (fpsSlider != null) fpsSlider.value = currentSettings.targetFPSIndex;
        UpdateFpsText(currentSettings.targetFPSIndex);
        
        if (brightnessSlider != null) brightnessSlider.value = currentSettings.brightness;
        UpdateSliderText(currentSettings.brightness, brightnessValueText, DisplayType.Percentage);
        
        if (particleDropdown != null) particleDropdown.value = currentSettings.particleEffectLevel;
        if (particleToggle != null) particleToggle.isOn = currentSettings.particleEffectLevel > 0;
        foreach (var toggle in new[] { fullScreenToggle, particleToggle })
        {
            if (toggle == null) continue;
            var caption = toggle.GetComponentInChildren<Text>();
            if (caption != null) caption.text = toggle.isOn ? "已开启" : "已关闭";
        }

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

    /// <summary>保存控件中的设置，并立即应用。</summary>
    public void SaveAndApplySettings()
    {
        // 拦截保护
        if (GM.Ins == null || GM.Ins.DM == null || GM.Ins.DM.settingsData == null) return;

        PlayerSettingsData dataToUpdate = GM.Ins.DM.settingsData;

        // 工业级无痛存储：只有被拖入场景的 UI 组件才会更新数据。
        // 未配置的 UI 组件将被跳过，它们在配置数据文件（JSON）中的原有值将完美保留，不会被清零或报错！
        if (textSpeedSlider != null) dataToUpdate.textSpeed = textSpeedSlider.value;
        if (autoPlayDelaySlider != null) dataToUpdate.autoPlayDelay = autoPlayDelaySlider.value;
        if (fastForwardIntervalSlider != null) dataToUpdate.fastForwardInterval = fastForwardIntervalSlider.value;
        if (branchPromptToggle != null) dataToUpdate.showImportantBranchPrompt = branchPromptToggle.isOn;
        if (languageDropdown != null) dataToUpdate.languageIndex = languageDropdown.value;

        if (fullScreenToggle != null) dataToUpdate.isFullScreen = fullScreenToggle.isOn;
        if (resolutionDropdown != null) dataToUpdate.resolutionIndex = resolutionDropdown.value;
        if (fpsDropdown != null) dataToUpdate.targetFPSIndex = fpsDropdown.value;
        if (fpsSlider != null) dataToUpdate.targetFPSIndex = Mathf.RoundToInt(fpsSlider.value);
        if (brightnessSlider != null) dataToUpdate.brightness = brightnessSlider.value;
        if (particleDropdown != null) dataToUpdate.particleEffectLevel = particleDropdown.value;
        if (particleToggle != null && particleToggle.isOn != (dataToUpdate.particleEffectLevel > 0))
            dataToUpdate.particleEffectLevel = particleToggle.isOn ? 2 : 0;

        if (masterVolumeSlider != null) dataToUpdate.masterVolume = masterVolumeSlider.value;
        if (bgmVolumeSlider != null) dataToUpdate.bgmVolume = bgmVolumeSlider.value;
        if (sfxVolumeSlider != null) dataToUpdate.sfxVolume = sfxVolumeSlider.value;
        if (voiceVolumeSlider != null) dataToUpdate.voiceVolume = voiceVolumeSlider.value;

        GM.Ins.DM.ApplySettings(dataToUpdate);

        if (statusText != null) statusText.text = "所有设置已成功应用！";
    }

    /// <summary>恢复默认设置，并刷新页面。</summary>
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
