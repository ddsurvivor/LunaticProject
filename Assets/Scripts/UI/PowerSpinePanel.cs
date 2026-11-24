using System.Linq;
using UnityEngine;

/// <summary>战外设施共享面板；使用配置报价、二次确认和既有存档槽界面，不自动覆盖存档。</summary>
public sealed class PowerSpinePanel : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text details, feedback;
    [SerializeField] private UnityEngine.UI.Button upgradeButton, closeButton, saveButton;
    private PowerSpineUpgradeRules rules;
    private PLAYERPROFILE quotedProfile;
    private int quotedLevel;
    private bool confirming; // 一次报价只允许提交一次，刷新或取消即失效。

    /// <summary>检查共享模板的必要文字与按钮引用。</summary>
    public bool HasTemplateBindings => details != null && feedback != null && upgradeButton != null && closeButton != null && saveButton != null;
    /// <summary>设施只在持有玩家档案的战外稳定场景开放。</summary>
    public static bool CanOpen => GM.Ins != null && GM.Ins.PLAYERPROFILE != null && !GM.Ins.HasBattleSession && !GM.Ins.IsTransitioning && BattleScene.Ins == null;

    /// <summary>首次创建模板时写入引用；运行时不重建布局，允许 prefab 及 Variant 手动调整。</summary>
    public void Configure(UnityEngine.UI.Text body, UnityEngine.UI.Text message, UnityEngine.UI.Button upgrade, UnityEngine.UI.Button close, UnityEngine.UI.Button save)
    { details = body; feedback = message; upgradeButton = upgrade; closeButton = close; saveButton = save; }

    private void Awake()
    {
        if (!HasTemplateBindings) return;
        upgradeButton.onClick.AddListener(Upgrade);
        closeButton.onClick.AddListener(CancelOrClose);
        saveButton.onClick.AddListener(OpenSave);
    }

    private void OnEnable()
    {
        if (!CanOpen || !HasTemplateBindings) { gameObject.SetActive(false); return; }
        var resource = Resources.Load<TextAsset>("Battle/PowerSpineUpgrades");
        rules = resource != null ? JsonUtility.FromJson<PowerSpineUpgradeRules>(resource.text) : null;
        confirming = false;
        feedback.text = "升级后请通过存档界面选择存档位保存。";
        Refresh();
    }

    private void Update()
    {
        if (!CanOpen) { gameObject.SetActive(false); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) CancelOrClose();
    }

    private void Refresh()
    {
        var profile = GM.Ins.PLAYERPROFILE;
        int level = profile.powerSpineProgress?.Level ?? 0;
        var current = new BurstGauge(); current.Reset(level, false);
        string next = "已达到最高等级";
        if (level < 3)
        {
            var target = new BurstGauge(); target.Reset(level + 1, false);
            var step = rules?.steps != null && level < rules.steps.Length ? rules.steps[level] : null;
            next = step == null ? "尚未配置下一等级" : $"下一级：Lv.{level + 1}　容量 {target.Capacity:0}　充能 ×{target.ChargeMultiplier:0.##}\n费用：{step.coinCost} 金币　需要章节：{step.minimumChapter}";
        }
        details.text = $"当前等级：Lv.{level}　持有金币：{profile.coins}\n聚能容量：{current.Capacity:0}　充能倍率：×{current.ChargeMultiplier:0.##}\n\n{next}\n\n每名角色入场消耗 50；序章与教学使用基础能力。";
        string error = "尚未配置动力脊升级规则";
        upgradeButton.interactable = rules != null && rules.CanUpgrade(profile, out error);
        if (!upgradeButton.interactable) feedback.text = error;
        upgradeButton.GetComponentInChildren<UnityEngine.UI.Text>(true).text = confirming ? "确认升级" : "升级动力脊";
        closeButton.GetComponentInChildren<UnityEngine.UI.Text>(true).text = confirming ? "取消确认" : "关闭";
        saveButton.interactable = FindSavePanel() != null;
    }

    private void Upgrade()
    {
        if (!CanOpen) return;
        if (!confirming)
        {
            quotedProfile = GM.Ins.PLAYERPROFILE; quotedLevel = quotedProfile.powerSpineProgress?.Level ?? 0;
            confirming = true; Refresh();
            if (upgradeButton.interactable) feedback.text = $"确认花费 {rules.steps[quotedLevel].coinCost} 金币升级？";
            return;
        }
        confirming = false;
        if (!ReferenceEquals(quotedProfile, GM.Ins.PLAYERPROFILE)) { feedback.text = "玩家档案已变化，请重新确认"; Refresh(); return; }
        bool success = GM.Ins.TryUpgradePowerSpine(rules, quotedLevel, out string error);
        Refresh();
        feedback.text = success ? "升级成功。请点击「打开存档」选择存档位保存。" : error;
    }

    private void CancelOrClose()
    {
        if (confirming) { confirming = false; feedback.text = "已取消升级，未扣除金币。"; Refresh(); }
        else gameObject.SetActive(false);
    }

    private SavePanel FindSavePanel() => Resources.FindObjectsOfTypeAll<SavePanel>().FirstOrDefault(p => p.GetType() == typeof(SavePanel) && p.gameObject.scene == gameObject.scene);
    private void OpenSave()
    {
        if (!CanOpen) return;
        var panel = FindSavePanel(); if (panel == null) return;
        gameObject.SetActive(false);
        // 旧存档页嵌套在队伍窗口内，需要同时打开其页面容器，沿用原关闭与槽位事件。
        for (var node = panel.transform.parent; node != null && node.GetComponent<Canvas>() == null; node = node.parent)
        { node.gameObject.SetActive(true); node.SetAsLastSibling(); }
        panel.ShowPanel(); panel.transform.SetAsLastSibling();
    }
}
