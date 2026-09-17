using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>仅在 Editor/Development Build 中由 BattleScene 自动挂载的战斗测试入口。</summary>
public class BattleTestTools : MonoBehaviour
{
    [LabelText("我方棋子序号（从0开始）"), Min(0)] public int playerPieceIndex;
    [LabelText("要授予的测试技能")] public TestSkillChoice selectedSkill;

    public enum TestSkillChoice
    {
        CognitiveProtection,
        SlowingField,
        OffensiveCognitiveProtection,
        SpontaneousMemeticAttack
    }

    private PieceController GetPlayerPiece()
    {
        var battle = BattleScene.Ins != null ? BattleScene.Ins.BM : null;
        var pieces = battle != null && battle.PlayerController != null
            ? battle.PlayerController.pieces : null;
        if (pieces == null || playerPieceIndex < 0 || playerPieceIndex >= pieces.Count ||
            pieces[playerPieceIndex] == null || pieces[playerPieceIndex].isDead)
        {
            Debug.LogWarning("战斗测试：请指定有效且存活的我方棋子序号。");
            return null;
        }
        return pieces[playerPieceIndex];
    }

    private static string GetName(TestSkillChoice choice)
    {
        switch (choice)
        {
            case TestSkillChoice.CognitiveProtection: return BattleTestSkillFactory.CognitiveProtection;
            case TestSkillChoice.SlowingField: return BattleTestSkillFactory.SlowingField;
            case TestSkillChoice.OffensiveCognitiveProtection: return BattleTestSkillFactory.OffensiveProtection;
            default: return BattleTestSkillFactory.SpontaneousAttack;
        }
    }

    [Button("给我方棋子添加选中技能")]
    public void GrantSelectedSkill() => GrantSkill(GetName(selectedSkill));

    [Button("给我方棋子添加全部测试技能")]
    public void GrantAllTestSkills()
    {
        foreach (var name in BattleTestSkillFactory.SkillNames) GrantSkill(name);
    }

    private void GrantSkill(string skillName)
    {
        var piece = GetPlayerPiece();
        if (piece == null || GM.Ins == null || GM.Ins.DM == null ||
            GM.Ins.DM.skillPackListSO == null) return;
        var skill = GM.Ins.DM.skillPackListSO.GetSkillPack(skillName);
        if (skill == null) return;

        // Init 原本直接引用 PieceData.skillPacks；复制列表以免污染角色配置。
        if (piece.availableSkills == null) piece.availableSkills = new List<SkillPack>();
        else piece.availableSkills = new List<SkillPack>(piece.availableSkills);
        if (piece.availableSkills.Exists(s => s != null && s.skillName == skillName)) return;
        piece.availableSkills.Add(skill);
        Debug.Log($"战斗测试：{piece.name} 获得技能 {skillName}");
    }

    [Button("回满我方棋子生命")]
    public void RefillHealth()
    {
        var piece = GetPlayerPiece();
        if (piece != null) piece.unitAttrCenter.FullHealth();
    }

    [Button("补满我方棋子行动点")]
    public void RefillMovePoints()
    {
        var piece = GetPlayerPiece();
        if (piece == null) return;
        var unit = piece.unitAttrCenter;
        unit.AddMP(unit.MaxMovePoint - unit.CurMovePoint);
    }

    [Button("补满我方棋子能量")]
    public void RefillMana()
    {
        var piece = GetPlayerPiece();
        if (piece == null) return;
        var unit = piece.unitAttrCenter;
        unit.AddMana(unit.MaxManaPoint - unit.ManaPoint);
    }
}
