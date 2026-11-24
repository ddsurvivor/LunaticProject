using System;
using UnityEngine;

/// <summary>设施升级的逐级配置；未启用的等级不能升级，零费用必须显式配置。</summary>
[Serializable]
public sealed class PowerSpineUpgradeStep
{
    public bool enabled;
    [Min(0)] public int coinCost;
    [Min(0)] public int minimumChapter;
    public string requiredProgressKey;
    [Min(1)] public int requiredProgressValue = 1;
}

/// <summary>战外设施使用的升级规则；索引 0–2 分别对应升至 1–3 级。</summary>
[Serializable]
public sealed class PowerSpineUpgradeRules
{
    public PowerSpineUpgradeStep[] steps = new PowerSpineUpgradeStep[3];

    /// <summary>只读校验当前等级的费用与进度要求；缺失配置时拒绝，不推断免费升级。</summary>
    public bool CanUpgrade(PLAYERPROFILE profile, out string error)
    {
        error = null;
        if (profile == null) { error = "没有可用的玩家档案"; return false; }
        int level = profile.powerSpineProgress?.Level ?? 0;
        if (level >= 3) { error = "动力脊已满级"; return false; }
        var step = steps != null && level < steps.Length ? steps[level] : null;
        if (step == null || !step.enabled) { error = "该等级尚未开放升级"; return false; }
        if (step.coinCost < 0 || step.minimumChapter < 0 || step.requiredProgressValue < 1)
        { error = "动力脊升级配置无效"; return false; }
        if (profile.chapterNumber < step.minimumChapter) { error = $"需要到达第 {step.minimumChapter} 章"; return false; }
        if (!string.IsNullOrWhiteSpace(step.requiredProgressKey) &&
            (profile.finishNodeDic == null || !profile.finishNodeDic.TryGetValue(step.requiredProgressKey, out int progress) || progress < step.requiredProgressValue))
        { error = "尚未满足设施解锁进度"; return false; }
        if (profile.coins < step.coinCost) { error = $"金币不足，需要 {step.coinCost}"; return false; }
        return true;
    }

    /// <summary>校验后一次提交扣费与等级；等级与界面报价不符时拒绝，调用方另走正式保存流程。</summary>
    public bool TryUpgrade(PLAYERPROFILE profile, int expectedLevel, out string error)
    {
        if (!CanUpgrade(profile, out error)) return false;
        int level = profile.powerSpineProgress?.Level ?? 0;
        if (level != expectedLevel) { error = "设施等级已变化，请重新确认"; return false; }
        int cost = steps[level].coinCost;
        // 付款前已完成全部校验；无回调的同步提交保证拒绝时不扣费、不创建旧档进度。
        profile.powerSpineProgress ??= new PowerSpineProgress();
        if (!profile.powerSpineProgress.TryUpgrade()) { error = "动力脊已满级"; return false; }
        profile.coins -= cost;
        return true;
    }
}
