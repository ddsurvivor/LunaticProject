using System.Collections.Generic;

public partial class PieceController
{
    private SkillLoadoutState skillLoadout; // 玩家引用存档状态，敌人持有本场独立配置。

    private void EnsureSkillLoadout()
    {
        skillLoadout ??= isPlayerPiece && playerData != null ? playerData.skills ??= new SkillLoadoutState() : new SkillLoadoutState();
        skillLoadout.Initialize(_pieceData);
    }

    /// <summary>汇总存档配置和装备附加技能；重复刷新不会还原玩家配置或修改 SO。</summary>
    public void RefreshConfiguredSkills()
    {
        EnsureSkillLoadout();
        availableSkills = _pieceData?.skillLibrary != null ? skillLoadout.Resolve(_pieceData.skillLibrary)
            : _pieceData?.DefaultSkills ?? new List<SkillPack>();
        foreach (var data in ComponentEquipment.Equipped(playerData))
            if (data.effectType == ComponentEffect.ActiveSkill && data.skillPack != null &&
                !string.IsNullOrWhiteSpace(data.skillPack.skillName) && !availableSkills.Contains(data.skillPack))
                availableSkills.Add(data.skillPack);
    }

    /// <summary>学习技能；立即配置时须等待动作结束，学习费用和等级条件由调用方控制。</summary>
    public bool LearnSkill(string skillId, bool equip = false)
    {
        if (equip && IsPerformingAction) return false;
        EnsureSkillLoadout();
        if (!skillLoadout.Learn(skillId, _pieceData?.skillLibrary, equip)) return false;
        if (equip) OnSkillConfigurationChanged();
        return true;
    }

    /// <summary>应用整组已学习技能配置；动作执行中拒绝更改，失败保留原配置。</summary>
    public bool ConfigureSkills(IEnumerable<string> skillIds)
    {
        if (IsPerformingAction) return false;
        EnsureSkillLoadout();
        if (!skillLoadout.Configure(skillIds, _pieceData?.skillLibrary)) return false;
        OnSkillConfigurationChanged();
        return true;
    }

    private void OnSkillConfigurationChanged()
    {
        RefreshConfiguredSkills();
        if (BattleScene.Ins?.UM != null) BattleScene.Ins.UM.OnPieceStateChance(this);
    }
}
