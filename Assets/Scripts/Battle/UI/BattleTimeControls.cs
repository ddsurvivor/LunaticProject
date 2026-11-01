using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>战斗右上角快进和设置入口；快捷键使用真实时间区分短按与临时长按。</summary>
[DefaultExecutionOrder(-100)]
public sealed class BattleTimeControls : MonoBehaviour
{
    private UIManager ui;
    private TimeManager clock;
    private BattleManager battle;
    private SettingPanel settings;
    private UnityEngine.UI.Toggle toggle;
    private UnityEngine.UI.Text speedText;
    private BattleTimeControlIcon arrow;
    private KeyCode shortcut;
    private float holdThreshold; // 真实秒数，不受战斗倍率影响。
    private bool pressing;
    private bool beforePress; // 松手、暂停或失焦时恢复的按下前快进状态。
    private float pressedAt;
    private bool focused = true;

    /// <summary>在现有战斗 Canvas 创建一次入口，设置窗口复用现有实例或指定 prefab。</summary>
    public static BattleTimeControls Create(Canvas canvas, UIManager manager, SettingPanel settingsPrefab,
        KeyCode key, float threshold)
    {
        var root = Rect("BattleTimeControls", canvas.transform, new Vector2(152, 60));
        root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
        root.anchoredPosition = new Vector2(-18, -18);
        var controls = root.gameObject.AddComponent<BattleTimeControls>();
        controls.ui = manager;
        controls.clock = BattleScene.Ins.TM;
        controls.battle = BattleScene.Ins.BM;
        controls.shortcut = key;
        controls.holdThreshold = Mathf.Max(.01f, threshold);
        controls.settings = canvas.GetComponentInChildren<SettingPanel>(true);
        if (controls.settings == null && settingsPrefab != null)
        {
            // 先在隐藏父级中实例化，确保 ActivePause 在窗口首次启用前已就绪。
            var staging = Rect("SettingsStaging", canvas.transform, Vector2.zero);
            staging.gameObject.SetActive(false);
            controls.settings = Instantiate(settingsPrefab, staging, false);
            controls.settings.gameObject.SetActive(false);
            controls.settings.transform.SetParent(canvas.transform, false);
            Destroy(staging.gameObject);
        }
        if (controls.settings != null)
        {
            if (controls.settings.GetComponent<ActivePause>() == null)
                controls.settings.gameObject.AddComponent<ActivePause>();
            // AVG 由外层入口负责返回；战斗实例启用 prefab 内已有的关闭按钮。
            foreach (var close in controls.settings.GetComponentsInChildren<CustomAdvancedButton>(true))
                if (close.name == "closeBtn")
                {
                    var rect = (RectTransform)close.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                    rect.anchoredPosition = new Vector2(-18, -18);
                    rect.sizeDelta = new Vector2(60, 60);
                    rect.localScale = Vector3.one;
                    foreach (var visual in close.GetComponentsInChildren<RectTransform>(true))
                        if (visual != rect)
                        {
                            visual.anchorMin = Vector2.zero;
                            visual.anchorMax = Vector2.one;
                            visual.offsetMin = visual.offsetMax = Vector2.zero;
                            visual.localScale = Vector3.one;
                        }
                    close.gameObject.SetActive(true);
                }
        }

        var fast = Rect("FastForward", root, new Vector2(72, 60));
        fast.anchoredPosition = new Vector2(-40, 0);
        var background = fast.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.055f, .085f, .11f, .96f);
        controls.toggle = fast.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        controls.toggle.targetGraphic = background;
        controls.toggle.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        controls.arrow = Icon(fast, false);
        controls.arrow.rectTransform.anchoredPosition = new Vector2(0, 7);
        var label = Rect("Speed", fast, new Vector2(64, 20));
        label.anchoredPosition = new Vector2(0, -17);
        controls.speedText = label.gameObject.AddComponent<UnityEngine.UI.Text>();
        controls.speedText.font = manager.pieceInfoPanel.GetComponentInChildren<UnityEngine.UI.Text>(true).font;
        controls.speedText.fontSize = 16;
        controls.speedText.horizontalOverflow = HorizontalWrapMode.Overflow;
        controls.speedText.verticalOverflow = VerticalWrapMode.Overflow;
        controls.speedText.alignment = TextAnchor.MiddleCenter;
        controls.speedText.raycastTarget = false;
        controls.toggle.onValueChanged.AddListener(controls.ToggleSpeed);

        var gear = Rect("Settings", root, new Vector2(60, 60));
        gear.anchoredPosition = new Vector2(36, 0);
        var gearBackground = gear.gameObject.AddComponent<UnityEngine.UI.Image>();
        gearBackground.color = background.color;
        var button = gear.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = gearBackground;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        button.interactable = controls.settings != null;
        Icon(gear, true);
        button.onClick.AddListener(controls.OpenSettings);
        controls.RefreshVisual();
        return controls;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var node = new GameObject(name, typeof(RectTransform));
        node.layer = parent.gameObject.layer;
        node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static BattleTimeControlIcon Icon(Transform parent, bool gear)
    {
        var rect = Rect("Icon", parent, new Vector2(34, 30));
        var icon = rect.gameObject.AddComponent<BattleTimeControlIcon>();
        icon.IsGear = gear;
        icon.raycastTarget = false;
        icon.color = new Color(.94f, .94f, .87f);
        return icon;
    }

    private bool Running => battle != null && battle.IsBattleRunning &&
        (GM.Ins == null || !GM.Ins.IsTransitioning);
    private bool CanUseShortcut => Running && focused && !clock.IsPaused && !ui.BlocksWorldInput;

    private void Update()
    {
        if (clock == null) return;
        bool allowed = CanUseShortcut;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != null && selected.GetComponent<UnityEngine.UI.InputField>() != null) allowed = false;
        bool keyDown = shortcut != KeyCode.None && Input.GetKeyDown(shortcut);
        // 先于 EventSystem 清除战斗快捷键的 Submit 目标，避免空格同时确认按钮、切换聚能等操作。
        if (allowed && keyDown && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        TickInput(Time.unscaledTime, keyDown,
            shortcut != KeyCode.None && Input.GetKey(shortcut),
            shortcut != KeyCode.None && Input.GetKeyUp(shortcut), allowed);
        if (!Running && clock.IsFastForward) clock.SetFastForward(false);
        toggle.interactable = allowed;
        RefreshVisual();
    }

    private void TickInput(float now, bool down, bool held, bool up, bool allowed)
    {
        if (!allowed) { CancelPress(); return; }
        if (down && !pressing)
        {
            pressing = true;
            beforePress = clock.IsFastForward;
            pressedAt = now;
        }
        if (!pressing) return;
        bool longPress = now - pressedAt >= holdThreshold;
        if (up)
        {
            pressing = false;
            clock.SetFastForward(longPress ? beforePress : !beforePress);
        }
        else if (!held) CancelPress();
        else if (longPress) clock.SetFastForward(true);
    }

    private void CancelPress()
    {
        if (!pressing) return;
        pressing = false;
        clock.SetFastForward(Running && beforePress);
    }

    private void ToggleSpeed(bool enabled)
    {
        if (CanUseShortcut)
        {
            CancelPress();
            clock.SetFastForward(enabled);
        }
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        RefreshVisual();
    }

    /// <summary>打开现有设置并独立持有暂停；关闭后由 ActivePause 恢复原快进状态。</summary>
    public void OpenSettings()
    {
        if (!Running || ui.BlocksWorldInput || settings == null) return;
        CancelPress();
        settings.LoadAndShowSettings();
        settings.gameObject.SetActive(true);
        settings.transform.SetAsLastSibling();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshVisual()
    {
        bool fast = clock.IsFastForward;
        toggle.SetIsOnWithoutNotify(fast);
        speedText.text = fast ? "3×" : "1×";
        arrow.color = fast ? new Color(1f, .76f, .28f) : new Color(.94f, .94f, .87f);
        speedText.color = arrow.color;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus && clock != null) CancelPress();
    }

    private void OnDisable()
    {
        if (clock == null) return;
        CancelPress();
        clock.SetFastForward(false);
    }
}
