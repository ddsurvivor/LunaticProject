using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum SceneDeviceKind { MobileCommandCenter, SentryTower, MedicalStation }

/// <summary>可直接摆放的独立装置；可被正式技能选中，但不加入行动、胜负或掉落名单。</summary>
public sealed class SceneBattleDevice : PieceController
{
    public SceneDeviceKind kind;
    [Min(0)] public float radius = 10f;
    [Min(1)] public int health = 65;
    [Min(0)] public int kineticArmor = 10, thermalArmor = 10, sparkArmor;
    public string displayName = "移动指挥中心";
    [Header("指挥中心")]
    [Min(0)] public float speedBonus = 1f;
    [Min(1)] public float hitMultiplier = 1.2f;
    [Range(0, 100)] public int criticalBonus = 5;
    [Min(1)] public int focusRounds = 1;
    [Header("医疗站")]
    [Range(0, 1)] public float healFraction = .25f;
    [Min(1)] public int frailStacks = 3;
    public bool healInitialOccupants = true;
    [Header("警戒塔")]
    public bool alarmOnInitialOccupants;
    public bool restartBattleOnAlarm = true;
    public UnityEvent onAlarm = new();
    public bool IsInitialized { get; private set; }
    public bool AlarmRaised { get; private set; }
    public bool IsOperational => IsInitialized && isActiveAndEnabled && !isDead &&
        (BattleScene.Ins?.BM?.AppleTraining?.Owns(this) == true ||
         unitAttrCenter.GetBuffStacks(BuffType.ElectronicInterference) == 0 &&
         unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0 && !MemeState.Paralyzed);
    public static readonly HashSet<SceneBattleDevice> Active = new();
    private readonly Dictionary<PieceController, Visit> visits = new();
    private static readonly Dictionary<PieceController, HealBudget> heals = new();
    private PieceController focusTarget;
    private int focusLife, focusStart, focusEnd, focusSequence;
    private static int sequence;
    private static readonly HashSet<BattleManager> alarmBattles = new();
    private bool destroying;

    private sealed class Visit { public int Life; public bool Inside; public Vector3 Position; }
    private sealed class HealBudget { public int Life, Epoch, LastEpoch = -1; }

    private IEnumerator Start()
    {
        while (BattleScene.Ins?.BM == null) yield return null;
        InitializeDevice();
    }

    /// <summary>从本预制体配置初始化耐久与机械身体，不读取玩家存档或改写共享棋子库。</summary>
    public void InitializeDevice()
    {
        if (IsInitialized) return;
        bool faction = isPlayerPiece;
        isPlayerPiece = false;
        Init(null, new PieceData
        {
            pieceName = displayName, maxHealth = Mathf.Max(1, health), speed = 0,
            memeBodyType = MemeBodyType.Mechanical,
            _armorDic = new Dictionary<DamageType, int> { [DamageType.Melee] = kineticArmor,
                [DamageType.Ranged] = thermalArmor, [DamageType.Electric] = sparkArmor }
        });
        isPlayerPiece = faction;
        cantControl = true;
        ableMove = false;
        IsInitialized = true;
        visits.Clear(); focusTarget = null; AlarmRaised = false;
        Active.Add(this);
    }

    private void LateUpdate()
    {
        var battle = BattleScene.Ins?.BM;
        if (!IsInitialized || isDead || battle == null || !battle.SpeedTurnsActive ||
            BattleScene.Ins.TM.IsPaused || GM.Ins.IsTransitioning) return;
        Active.Add(this);
        var pieces = battle.SpeedParticipants();
        foreach (var piece in pieces)
        {
            if (piece is SceneBattleDevice || piece == null) continue;
            bool inside = Contains(piece.transform.position);
            bool first = !visits.TryGetValue(piece, out var visit) || visit.Life != piece.BattleLifeVersion;
            bool entered = first ? inside : !visit.Inside &&
                (inside || CrossesRange(visit.Position, piece.transform.position));
            visits[piece] = new Visit { Life = piece.BattleLifeVersion, Inside = inside, Position = piece.transform.position };
            bool trainingGuard = battle.AppleTraining != null && battle.AppleTraining.Owns(this);
            if (!entered || (!trainingGuard && !IsOperational)) continue;
            if (kind == SceneDeviceKind.SentryTower && BattleRelations.CanAttack(this, piece) &&
                (!first || alarmOnInitialOccupants)) RaiseAlarm();
            if (kind == SceneDeviceKind.MedicalStation)
            {
                if (BattleRelations.CanSupport(this, piece) && (!first || healInitialOccupants)) HealOnce(piece, true);
                else if (BattleRelations.CanAttack(this, piece)) ApplyFrail(piece, battle);
            }
        }
        foreach (var stale in visits.Keys.Where(p => p == null || !pieces.Contains(p)).ToArray()) visits.Remove(stale);
    }

    public bool Contains(Vector3 point) => radius >= 0 && !float.IsNaN(radius) && !float.IsInfinity(radius) &&
        (point - transform.position).sqrMagnitude <= radius * radius;

    /// <summary>变更装置阵营并清除旧集火和范围记录；返回是否实际变更。</summary>
    public bool SetFaction(bool playerFaction)
    {
        if (!IsInitialized || !isActiveAndEnabled || isDead || isPlayerPiece == playerFaction) return false;
        isPlayerPiece = playerFaction;
        focusTarget = null;
        visits.Clear();
        return true;
    }

    private bool CrossesRange(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float t = delta.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector3.Dot(transform.position - from, delta) / delta.sqrMagnitude) : 0;
        return Contains(from + delta * t);
    }

    private bool Supports(PieceController target) => IsOperational && target != null && !target.isDead &&
        gameObject.scene == target.gameObject.scene && BattleRelations.CanSupport(this, target) && Contains(target.transform.position);

    /// <summary>同类装置不叠加，实时读取最强来源；离开、摧毁或电子干扰后不残留属性写入。</summary>
    public static float SpeedBonus(PieceController target) => Commands(target).Select(d => d.speedBonus).DefaultIfEmpty(0).Max();
    public static float HitMultiplier(PieceController target) => Commands(target).Select(d => d.hitMultiplier).DefaultIfEmpty(1).Max();
    public static int CriticalBonus(PieceController target) => Commands(target).Select(d => d.criticalBonus).DefaultIfEmpty(0).Max();
    private static IEnumerable<SceneBattleDevice> Commands(PieceController target) => Active.Where(d => d != null &&
        d.kind == SceneDeviceKind.MobileCommandCenter && d.Supports(target));

    /// <summary>正式技能效果完成后通知一次；电子干扰先落地，摧毁指挥中心不产生集火。</summary>
    public void NotifySkillHit(PieceController attacker, SkillPack skill)
    {
        var battle = BattleScene.Ins?.BM;
        if (!IsInitialized || attacker == null || battle == null || skill?.attackPacks?.Count == 0 ||
            !BattleRelations.CanAttack(attacker, this)) return;
        if (battle.AppleTraining != null && battle.AppleTraining.Owns(this))
        { RaiseAlarm(); return; }
        if (kind == SceneDeviceKind.MobileCommandCenter && IsOperational)
        {
            focusTarget = attacker; focusLife = attacker.BattleLifeVersion;
            focusStart = battle.TunrNumber + 1; focusEnd = focusStart + Mathf.Max(1, focusRounds);
            focusSequence = ++sequence;
        }
        if (kind == SceneDeviceKind.SentryTower && unitAttrCenter.GetBuffStacks(BuffType.ElectronicInterference) == 0 &&
            unitAttrCenter.GetBuffStacks(BuffType.Stun) == 0 && !MemeState.Paralyzed) RaiseAlarm();
    }

    /// <summary>覆盖内同阵营敌人下个标准轮优先选择最近一次攻击者；死亡、复用、停机或轮次到期失效。</summary>
    public static PieceController FocusTarget(EnemyController enemy)
    {
        var battle = BattleScene.Ins?.BM;
        if (enemy == null || battle == null) return null;
        return Commands(enemy).Where(d => d.focusTarget != null && !d.focusTarget.isDead &&
            d.focusTarget.gameObject.activeInHierarchy && d.focusLife == d.focusTarget.BattleLifeVersion &&
            battle.TunrNumber >= d.focusStart && battle.TunrNumber < d.focusEnd && BattleRelations.CanAttack(enemy, d.focusTarget))
            .OrderByDescending(d => d.focusSequence).Select(d => d.focusTarget).FirstOrDefault();
    }

    private static HealBudget Budget(PieceController target)
    {
        if (!heals.TryGetValue(target, out var budget) || budget.Life != target.BattleLifeVersion)
            heals[target] = budget = new HealBudget { Life = target.BattleLifeVersion };
        return budget;
    }

    private void HealOnce(PieceController target, bool passedRange = false)
    {
        var source = Active.Where(d => d != null && d.kind == SceneDeviceKind.MedicalStation &&
            (d.Supports(target) || passedRange && d == this && d.IsOperational && !target.isDead &&
             d.gameObject.scene == target.gameObject.scene && BattleRelations.CanSupport(d, target)))
            .OrderByDescending(d => d.healFraction).FirstOrDefault();
        if (source == null) return;
        var budget = Budget(target);
        if (budget.LastEpoch == budget.Epoch) return;
        budget.LastEpoch = budget.Epoch;
        target.unitAttrCenter.Heal(Mathf.CeilToInt(target.unitAttrCenter.MaxHealth * Mathf.Clamp01(source.healFraction)));
    }

    private void ApplyFrail(PieceController target, BattleManager battle)
    {
        int current = target.unitAttrCenter.GetBuffStacks(BuffType.Frail);
        if (current >= 0 && current < frailStacks) battle.buffManager.AddBuff(target.unitAttrCenter, BuffType.Frail, frailStacks - current);
    }

    /// <summary>仅普通棋子回合调用；同一角色每回合各医疗站共享一次治疗额度，聚能不另算回合。</summary>
    public static void OnNormalTurnStart(PieceController target, BattleManager battle)
    {
        if (Active.Count == 0) return;
        Budget(target).Epoch++;
        foreach (var device in Active.ToArray())
        {
            if (device == null || !device.IsOperational || device.kind != SceneDeviceKind.MedicalStation ||
                device.gameObject.scene != target.gameObject.scene || !device.Contains(target.transform.position)) continue;
            if (BattleRelations.CanSupport(device, target)) device.HealOnce(target);
            else if (BattleRelations.CanAttack(device, target)) device.ApplyFrail(target, battle);
        }
    }

    /// <summary>装置不进入行动队列，其状态在标准轮更替时沿用正式Buff结算和递减。</summary>
    public static void OnWorldRound(BattleManager battle)
    {
        foreach (var device in Active.ToArray())
        {
            if (device == null || !device.IsInitialized || device.isDead || device.gameObject.scene != battle.gameObject.scene) continue;
            battle.buffManager.ProcessBuffs(device.unitAttrCenter);
            battle.buffManager.ProcessBuffDurations(device.unitAttrCenter);
        }
    }

    private void RaiseAlarm()
    {
        var battle = BattleScene.Ins?.BM;
        if (battle != null && battle.AppleTraining != null && battle.AppleTraining.Owns(this))
        {
            if (battle.AppleTraining.TryRaiseAlarm(this)) { AlarmRaised = true; onAlarm.Invoke(); }
            return;
        }
        if (AlarmRaised || battle == null || battle.PlayerController.isBursting || GM.Ins.IsTransitioning ||
            unitAttrCenter.GetBuffStacks(BuffType.ElectronicInterference) != 0 ||
            !alarmBattles.Add(battle)) return;
        AlarmRaised = true;
        onAlarm.Invoke();
        if (restartBattleOnAlarm) StartCoroutine(RestartAfterAction(battle));
        else alarmBattles.Remove(battle);
    }

    private IEnumerator RestartAfterAction(BattleManager battle)
    {
        yield return null;
        while (battle != null && battle.SpeedTurnsActive && (battle.HasCombatPresentation || battle.moveManager.IsMoving)) yield return null;
        if (battle != null && battle.SpeedTurnsActive && !GM.Ins.IsTransitioning && !GM.Ins.RestartBattleFromDeviceAlarm())
        { AlarmRaised = false; alarmBattles.Remove(battle); Debug.LogWarning("警戒塔无法重置：当前没有有效战斗会话。", this); }
    }

    /// <summary>保存本关装置访问与报警状态，恢复不重发进入、治疗或报警事件。</summary>
    internal Action CaptureDeviceTurnState()
    {
        bool raised = AlarmRaised, dying = destroying;
        var target = focusTarget; int life = focusLife, start = focusStart, end = focusEnd, order = focusSequence;
        var savedVisits = visits.ToDictionary(p => p.Key, p => new Visit { Life = p.Value.Life, Inside = p.Value.Inside, Position = p.Value.Position });
        return () =>
        {
            AlarmRaised = raised; destroying = dying; focusTarget = target; focusLife = life;
            focusStart = start; focusEnd = end; focusSequence = order; visits.Clear();
            foreach (var pair in savedVisits) visits.Add(pair.Key, new Visit { Life = pair.Value.Life, Inside = pair.Value.Inside, Position = pair.Value.Position });
            if (isActiveAndEnabled) Active.Add(this);
        };
    }

    public override void Hurt() => pieceDisplay?.ChangeDisplayState(PieceDisplayState.Hit, true);

    /// <summary>装置立即失去覆盖与碰撞，不触发棋子胜负、奖励或掉落；保留对象以完成报警回调。</summary>
    public override void Dead()
    {
        if (destroying) return;
        destroying = true; focusTarget = null;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        // 停止装置作用后保留精灵渲染，死亡序列播完再隐藏。
        if (pieceDisplay != null && pieceDisplay.pieceSpriteRenderer != null &&
            pieceDisplay.deathSprites != null && pieceDisplay.deathSprites.Count > 0)
        {
            var spriteRenderer = pieceDisplay.pieceSpriteRenderer;
            spriteRenderer.enabled = true;
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, finish: () =>
            {
                if (spriteRenderer != null) spriteRenderer.enabled = false;
            });
        }
        OnDead?.Invoke();
    }

    protected override void OnDisable()
    {
        base.OnDisable(); Active.Remove(this); focusTarget = null; visits.Clear();
        if (Active.Count == 0) { heals.Clear(); alarmBattles.Clear(); }
    }

    private void OnDrawGizmosSelected()
    { Gizmos.color = kind == SceneDeviceKind.MedicalStation ? Color.green : kind == SceneDeviceKind.SentryTower ? Color.red : Color.cyan; Gizmos.DrawWireSphere(transform.position, Mathf.Max(0, radius)); }
}
