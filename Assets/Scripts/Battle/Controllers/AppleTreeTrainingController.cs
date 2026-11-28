using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>苹果树训练副本的警戒回退及聚能击杀规则；不改变其他关卡或苹果树Prefab。</summary>
public sealed class AppleTreeTrainingController : MonoBehaviour
{
    public EnemyController appleTree;
    public PieceController green;
    public List<SceneBattleDevice> towers = new();
    public string alarmLog;
    public bool AlarmPending { get; private set; } // 从首次报警到确认恢复始终阻止新行动和交棒。
    private int alarmVersion;
    private int treeDeathVersion; // 回退或停用时使旧死亡动画回调失效。
    private bool treeDeathPending, treeDeathCompleted; // 死亡帧播放完成前不提交死亡事件或胜利。

    /// <summary>本关只在绑定树的死亡动画完成后开放胜利，普通致死或外部提前判胜不越过此门控。</summary>
    internal bool HoldVictory => isActiveAndEnabled && !treeDeathCompleted;

    /// <summary>仅本关绑定树接受聚能攻击命中即清零；其他伤害最多降至1，不提前通关。</summary>
    internal int AdjustTreeDamage(PieceController target, BattleDamageSource source, int damage)
    {
        if (!isActiveAndEnabled || target != appleTree || BattleScene.Ins?.BM?.AppleTraining != this) return damage;
        var battle = BattleScene.Ins.BM;
        bool burstHit = !AlarmPending && source.Kind == BattleDamageKind.Skill &&
            source.Action.Actor != null && source.Action.Actor.isPlayerPiece && battle.IsBurstActor(source.Action.Actor);
        return burstHit ? appleTree.unitAttrCenter.CurHealth : Mathf.Min(damage, Mathf.Max(0, appleTree.unitAttrCenter.CurHealth - 1));
    }

    /// <summary>生命归零后立即播放本树死亡帧，完成后才沿正式敌人死亡收尾及胜利入口提交。</summary>
    internal bool TryPlayTreeDeath(PieceController target)
    {
        if (!isActiveAndEnabled || target != appleTree || !appleTree.isDead) return false;
        if (treeDeathPending || treeDeathCompleted) return true;
        treeDeathPending = true;
        int version = ++treeDeathVersion, life = appleTree.BattleLifeVersion;
        var battle = BattleScene.Ins.BM;
        appleTree.pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, false, -1, () =>
        {
            if (!isActiveAndEnabled || version != treeDeathVersion || appleTree == null ||
                life != appleTree.BattleLifeVersion || BattleScene.Ins?.BM != battle || GM.Ins.IsTransitioning) return;
            treeDeathPending = false;
            treeDeathCompleted = true;
            appleTree.CompleteDeathAfterAnimation();
            battle.PlayerWin();
        });
        return true;
    }

    /// <summary>每名角色普通回合结束回满存活的苹果树；聚能切换不经过此入口。</summary>
    internal void OnNormalTurnEnded()
    {
        if (appleTree != null && !appleTree.isDead && !treeDeathPending && !AlarmPending)
            appleTree.unitAttrCenter.SetHealth(100f);
    }

    /// <summary>报警恢复快照时撤销已放弃分支的树死亡标记与旧回调。</summary>
    internal void ResetTreeDeath()
    {
        treeDeathVersion++;
        treeDeathPending = treeDeathCompleted = false;
    }

    public bool Owns(SceneBattleDevice device) => device != null && towers.Contains(device);

    /// <summary>仅训练中的绿临时获得现有骇入，不学习、不配置永久技能，也不改共享技能资源。</summary>
    internal void AddTrainingSkills(PieceController actor)
    {
        if (actor != green) return;
        var skill = actor.pieceData?.skillLibrary?.GetById(SkillPack.AdultGreenHackId);
        if (skill != null && !actor.availableSkills.Contains(skill)) actor.availableSkills.Add(skill);
    }

    /// <summary>原关卡禁用了技能，仅为本关的绿临时开放技能菜单。</summary>
    internal void ConfigureActions()
    {
        if (green != null && !green.availableActions.Contains(ActionType.Skill)) green.availableActions.Add(ActionType.Skill);
    }

    /// <summary>未骇入的塔在本关必报警，聚能及临时停机不绕过；同次多塔报警只接管一次。</summary>
    internal bool TryRaiseAlarm(SceneBattleDevice tower)
    {
        var battle = BattleScene.Ins?.BM;
        if (!Owns(tower) || tower.isPlayerPiece || AlarmPending || battle == null ||
            !battle.SpeedTurnsActive || GM.Ins.IsTransitioning) return false;
        AlarmPending = true;
        battle.PlayerController.isInTurn = false;
        battle.AIController.isInTurn = false;
        StartCoroutine(PresentAlarm(battle, ++alarmVersion));
        return true;
    }

    private IEnumerator PresentAlarm(BattleManager battle, int version)
    {
        yield return null;
        // 先完成已提交动作，再播放剧情；确认前不取消在途命中或让调度器交棒。
        while (battle != null && (battle.HasCombatPresentation || battle.moveManager.IsMoving)) yield return null;
        if (!Current(battle, version)) yield break;
        battle.battleDialogueManager.StartLog(alarmLog, () =>
        {
            if (!Current(battle, version)) return;
            BattleScene.Ins.UM.ShowDeviceAlarmConfirmation(() =>
            {
                if (!Current(battle, version) || !battle.RestorePreviousNormalTurn()) return false;
                AlarmPending = false; alarmVersion++;
                battle.PlayerController.RefreshBurstUI();
                return true;
            });
        });
    }

    private bool Current(BattleManager battle, int version) => isActiveAndEnabled && AlarmPending &&
        version == alarmVersion && BattleScene.Ins?.BM == battle && battle.SpeedTurnsActive && !GM.Ins.IsTransitioning;

    private void OnDisable() { AlarmPending = false; alarmVersion++; ResetTreeDeath(); StopAllCoroutines(); }
}
