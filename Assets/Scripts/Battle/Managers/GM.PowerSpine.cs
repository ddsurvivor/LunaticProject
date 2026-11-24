public partial class GM
{
    /// <summary>设施确认入口；重新校验会话、报价等级与配置后同时提交扣费和升级，不自动覆盖存档槽。</summary>
    public bool TryUpgradePowerSpine(PowerSpineUpgradeRules rules, int expectedLevel, out string error)
    {
        if (PLAYERPROFILE == null) { error = "没有可用的玩家档案"; return false; }
        if (HasBattleSession || IsTransitioning ||
            (BattleScene.Ins != null && BattleScene.Ins.BM != null && BattleScene.Ins.BM.IsBattleRunning))
        { error = "战斗或转场期间不能升级设施"; return false; }
        if (rules == null) { error = "尚未配置动力脊升级规则"; return false; }
        return rules.TryUpgrade(PLAYERPROFILE, expectedLevel, out error);
    }

    /// <summary>按正式资源配置提交当前等级升级；同样校验费用与解锁，不提供免费旁路。</summary>
    public bool TryUpgradePowerSpine()
    {
        var resource = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Battle/PowerSpineUpgrades");
        var rules = resource != null ? UnityEngine.JsonUtility.FromJson<PowerSpineUpgradeRules>(resource.text) : null;
        return TryUpgradePowerSpine(rules, PLAYERPROFILE?.powerSpineProgress?.Level ?? 0, out _);
    }
}
