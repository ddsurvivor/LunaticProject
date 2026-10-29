using UnityEngine;

/// <summary>动作悬停使用的只读费用快照，不执行扣费或触发战斗事件。</summary>
public readonly struct ActionResourceCost
{
    public readonly int AP;
    public readonly int Mana;
    public readonly int Ammo;

    public ActionResourceCost(int ap, int mana = 0, int ammo = 0)
    {
        AP = Mathf.Max(0, ap);
        Mana = Mathf.Max(0, mana);
        Ammo = Mathf.Max(0, ammo);
    }

    public bool CanPay(UnitAttrCenter attr) => attr != null && attr.CurMovePoint >= AP
        && attr.ManaPoint >= Mana && attr.AmmoCount >= Ammo;

    /// <summary>复用正式流程的 AP 配置、最终技能能量费和远程普通攻击单发弹药费。</summary>
    public static ActionResourceCost Query(PieceController piece, ActionType action, SkillPack skill = null)
    {
        var attr = piece.unitAttrCenter;
        return new ActionResourceCost(action == ActionType.Wait ? attr.CurMovePoint
            : GM.Ins.DM.gameConstSO.GetActionPointCost(action),
            action == ActionType.Skill && skill != null ? attr.GetSkillManaCost(skill) : 0,
            action == ActionType.RangedAttack ? 1 : 0);
    }
}
