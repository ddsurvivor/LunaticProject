using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>空间站训练场景专用流程；按真实行动完成教学，普通关卡不挂载此组件。</summary>
[DisallowMultipleComponent]
public sealed class SpaceStationTrainingController : MonoBehaviour
{
    public enum TrainingStep { Waiting, Move, Attack, Drones, SecondArrival, SecondBattle, ThirdArrival, WaitingForMemeTurn, EquipMeme, FinalBattle, Cancelled }
    public PieceController qiuWu;
    public Transform movementTarget;
    public EnemyController firstDrone;
    public List<EnemyController> drones = new();
    public List<EnemyController> secondWave = new();
    public List<EnemyController> thirdWave = new();
    public string arrivalLog = "BTPR0-Training-Arrival";
    public string burstReadyLog = "BTPR0-2";
    public int dragonMemeId = 1006; // 使用稳定词条编号，不依赖显示名称。
    [Min(.1f)] public float arrivalTolerance = .8f;
    /// <summary>本场只读教学步骤；重开重新创建，永久已读记录不跳过强制操作。</summary>
    public TrainingStep Step { get; private set; } = TrainingStep.Waiting;
    /// <summary>强制步骤禁止切换查看对象、聚能和撤销；装备AP不足时仅放行等待。</summary>
    public bool RestrictsCommands => pendingWave != 0 || presentingReady || Step != TrainingStep.Drones && Step != TrainingStep.SecondBattle &&
        Step != TrainingStep.FinalBattle && Step != TrainingStep.Cancelled;
    /// <summary>阶段交接和装备后的提示期间保持原行动者，不能提前让下一单位行动。</summary>
    public bool BlocksTurnAdvance => pendingWave != 0 || presentingReady || finishingEquipment ||
        Step == TrainingStep.SecondArrival || Step == TrainingStep.ThirdArrival;
    private BattleManager battle;
    private PieceController memeActor; // 装备步骤绑定真实获得控制权的玩家单位。
    private bool openingTaken; // 首个教学行动只消费一次。
    private bool initialized; // 只有本场初始化后才接管流程。
    private bool readyPending; // 满槽提示等待当前动作和入场剧情结束。
    private bool readyShown; // 每场只显示一次满槽提示。
    private bool finishingEquipment; // 装备后先等待提示完成，再释放当前角色。
    private int pendingWave; // 全灭时先占用下一波，防止多次死亡回调重复刷怪。
    private int lifetime; // 旧剧情及教程关闭回调不能推进重开后的流程。
    private Coroutine presentation; // 增援与聚能提示共用唯一串行流程。
    private BattleActionContext teachingAttack; // 仅保存首次有效普攻的身份，弹道到达后仍有效。
    private bool teachingImpact; // 真实命中结算后才允许完成首攻教学。
    private bool presentingReady; // 满槽剧情与教程交接期间阻止新指令。
    private Material markerMaterial;
    private Transform deviceIcon;
    private Renderer deviceBody;
    private Renderer[] deviceRenderers;
    private readonly Dictionary<Player, int[]> originalSlots = new(); // 离场清理临时词条时恢复原槽位。
    private SpaceStationTrainingHint hint;

    /// <summary>棋子初始化前隐藏待增援单位；只从首波开始参战。</summary>
    public void PrepareBattle(BattleManager owner)
    {
        battle = owner;
        lifetime++;
        Step = TrainingStep.Waiting;
        foreach (var enemy in secondWave.Concat(thirdWave))
            if (enemy != null) { enemy.isActived = false; enemy.gameObject.SetActive(false); }
        foreach (var enemy in drones)
            if (enemy != null) { enemy.isActived = true; enemy.gameObject.SetActive(true); }
        battle.AIController.pieces.RemoveAll(p => secondWave.Contains(p as EnemyController) || thirdWave.Contains(p as EnemyController));
    }

    /// <summary>记录本场教学前装配；待增援单位留到激活后再初始化，避免未唤醒组件重载序列化值。</summary>
    public void InitializeBattle()
    {
        foreach (var actor in battle.PlayerController.pieces)
            if (actor != null && actor.playerData != null) originalSlots[actor.playerData] = MemeEquipment.CopySlots(actor.playerData.memeSlots);
        initialized = true;
    }

    /// <summary>本关首个行动固定交给邱悟一次，不修改其永久速度或其他单位的普通队列进度。</summary>
    public PieceController TakeOpeningActor()
    {
        if (!initialized || openingTaken || !BattleManager.IsSpeedParticipant(qiuWu)) return null;
        openingTaken = true;
        return qiuWu;
    }

    /// <summary>真实角色回合开始或聚能交还控制时启用教学；装备步骤需要足够AP。</summary>
    public void OnPieceTurnStarted(PieceController actor)
    {
        if (!initialized) return;
        if (Step == TrainingStep.Waiting && actor == qiuWu)
        {
            ShowMovementMarker();
            SetStep(TrainingStep.Move, "移动教学：选择“移动”，前往无人机面前的金色标记。");
        }
        else if (Step == TrainingStep.WaitingForMemeTurn && actor != null && actor.isPlayerPiece &&
                 actor.unitAttrCenter.CurMovePoint >= MemeEquipment.BattleEquipCost)
        {
            memeActor = actor;
            SetStep(TrainingStep.EquipMeme, "模因教学：打开“模因装配”，选择槽位和“屠龙术”，再确认装备（2 AP）。");
            if (actor.playerData?.memeSlots?.Contains(dragonMemeId) == true) OnMemeEquipped(actor);
        }
        else if (Step == TrainingStep.WaitingForMemeTurn && actor != null && actor.isPlayerPiece)
            SetStep(TrainingStep.WaitingForMemeTurn, "模因装配需要2 AP。当前行动点不足，请选择“等待”，由下一名角色进行教学。");
    }

    /// <summary>只限制本关强制步骤，不借用外部控制权锁，以免同时挡住所需操作。</summary>
    public bool AllowsActor(PieceController actor)
    {
        if (!initialized || Step == TrainingStep.Cancelled) return true;
        if (Step == TrainingStep.Move || Step == TrainingStep.Attack) return actor == qiuWu;
        if (Step == TrainingStep.EquipMeme) return !finishingEquipment && actor == memeActor;
        if (Step == TrainingStep.WaitingForMemeTurn) return actor != null && actor.isPlayerPiece && actor == battle.ActivePiece;
        return !RestrictsCommands;
    }

    /// <summary>菜单展示与费用提交共用步骤白名单，避免快捷键或其他入口绕过教学。</summary>
    public bool AllowsAction(PieceController actor, ActionType action)
    {
        if (!RestrictsCommands) return true;
        if (!AllowsActor(actor)) return false;
        return Step == TrainingStep.WaitingForMemeTurn && action == ActionType.Wait || Step == TrainingStep.Move && action == ActionType.Move ||
            Step == TrainingStep.Attack && (action == ActionType.MeleeAttack || action == ActionType.RangedAttack);
    }

    /// <summary>移动教学只接受标记附近的实际导航落点，未确认和不可达移动不会推进。</summary>
    public bool AllowsMoveDestination(PieceController actor, Vector3 position) => !RestrictsCommands ||
        Step == TrainingStep.Move && actor == qiuWu && movementTarget != null &&
        Vector3.Distance(position, movementTarget.position) <= arrivalTolerance;

    /// <summary>只有正常到达目标才解锁攻击；失败的导航保持移动教学。</summary>
    public void OnMovementFinished(PieceController actor, bool reached)
    {
        if (Step != TrainingStep.Move || actor != qiuWu || !reached || !AllowsMoveDestination(actor, actor.transform.position)) return;
        battle.ClearMoveUndo();
        SetStep(TrainingStep.Attack, "攻击教学：选择近战或远程攻击前方无人机；本次攻击必定命中。");
    }

    /// <summary>确认普攻前验证指定教学目标；取消瞄准不占用必定命中的机会。</summary>
    public bool AllowsAttack(PieceController actor, ActionType action, IReadOnlyList<PieceController> targets)
    {
        if (!RestrictsCommands) return true;
        return Step == TrainingStep.Attack && teachingAttack == null && AllowsAction(actor, action) &&
            targets != null && targets.Count == 1 && targets[0] == firstDrone && firstDrone != null && !firstDrone.isDead;
    }

    /// <summary>有效提交后保存独立行动身份，反应和其他攻击不继承本次命中覆盖。</summary>
    public void OnAttackCommitted(BattleActionContext action, ActionType type, IReadOnlyList<PieceController> targets)
    {
        if (action != null && !action.IsReaction && AllowsAttack(action.Actor, type, targets)) teachingAttack = action;
    }

    /// <summary>预览尚未提交时按教学目标显示100%；实战必须匹配已提交的同一次行动。</summary>
    public bool GuaranteesHit(PieceController actor, PieceController target, SkillPack skill, BattleActionContext action, bool preview)
    {
        if (Step != TrainingStep.Attack || actor != qiuWu || target != firstDrone || actor.pieceData == null ||
            skill != actor.pieceData.MeleeAttack && skill != actor.pieceData.RangedAttack) return false;
        return preview ? teachingAttack == null : action != null && ReferenceEquals(action, teachingAttack) && !action.IsReaction;
    }

    /// <summary>结算入口通知实际命中，不能用动画或按钮点击冒充已完成攻击。</summary>
    public void OnAttackImpact(BattleActionContext action)
    {
        if (ReferenceEquals(action, teachingAttack) && action != null) teachingImpact = true;
    }

    /// <summary>等待本次表现全部结束后解除教学；取消动作保留重试机会。</summary>
    public void OnAttackFinished(BattleActionContext action)
    {
        if (action == null || !ReferenceEquals(action, teachingAttack)) return;
        teachingAttack = null;
        if (teachingImpact)
        {
            SetStep(TrainingStep.Drones, "消灭场内全部无人机。");
            battle.PlayerCheckWin();
        }
        teachingImpact = false;
    }

    /// <summary>敌方全灭时接管前两波胜利；失败判定仍由BattleManager执行。</summary>
    public bool HoldVictory()
    {
        if (!initialized || Step == TrainingStep.Cancelled) return false;
        if (Step == TrainingStep.FinalBattle) return false;
        if (pendingWave == 0)
        {
            if (Step == TrainingStep.Drones) pendingWave = 2;
            else if (Step == TrainingStep.SecondBattle) pendingWave = 3;
        }
        if (pendingWave != 0 && presentation == null) presentation = StartCoroutine(AdvanceWave(lifetime));
        return true;
    }

    private bool Current(int version) => initialized && version == lifetime && isActiveAndEnabled && battle != null && battle.IsBattleRunning;
    private bool Stable => !BattleScene.Ins.TM.IsPaused && !battle.HasCombatPresentation && !battle.moveManager.IsMoving &&
        !battle.BurstEffectsDeferred && !MemeLoadoutPanel.IsOpen && !battle.tutorialManager.tutorialPanel.gameObject.activeInHierarchy;

    private IEnumerator AdvanceWave(int version)
    {
        yield return null;
        while (Current(version) && !Stable) yield return null;
        if (!Current(version)) yield break;
        int next = pendingWave;
        SetStep(next == 2 ? TrainingStep.SecondArrival : TrainingStep.ThirdArrival, "敌方增援抵达。");
        BattleScene.Ins.CM.ClearTurnSelection();
        if (next == 3 && readyPending && !readyShown) yield return PresentBurstReady(version);
        if (!Current(version)) yield break;
        foreach (var enemy in next == 2 ? secondWave : thirdWave)
        {
            if (enemy == null) continue;
            enemy.gameObject.SetActive(true);
            enemy.Init(battle.AIController, battle.pieceDataListSO.GetPieceData(enemy.pieceID));
            enemy.isActived = true;
            var icon = enemy.transform.Find("TrainingDevicePlaceholderIcon");
            if (icon != null && enemy.pieceDisplay?.pieceSpriteRenderer != null)
            {
                var body = enemy.pieceDisplay.pieceSpriteRenderer;
                icon.gameObject.layer = body.gameObject.layer;
                deviceIcon = icon;
                deviceBody = body;
                deviceRenderers = enemy.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(icon)).ToArray();
                UpdateDeviceIcon();
            }
            if (!battle.AIController.pieces.Contains(enemy)) battle.AIController.pieces.Add(enemy);
        }
        pendingWave = 0;
        if (next == 2)
        {
            yield return PlayLog(arrivalLog, version);
            if (!Current(version)) yield break;
            SetStep(TrainingStep.SecondBattle, "击败两名黑衣人；积攒聚能后可发动聚能进行攻击。");
            readyPending |= battle.PlayerController.burstCharge >= battle.PlayerController.maxBurstCharge;
            if (readyPending && !readyShown) yield return PresentBurstReady(version);
        }
        else
        {
            SetStep(TrainingStep.WaitingForMemeTurn, "第三波抵达。模因装配需要2 AP；当前不足时请选择“等待”，由下一名角色进行教学。");
            var actor = battle.ActivePiece;
            if (actor != null && battle.CanControlPiece(actor)) OnPieceTurnStarted(actor);
        }
        if (!Current(version)) yield break;
        presentation = null;
        RestoreActorMenu();
    }

    /// <summary>按容量满槽而非50点发动门槛触发本关剧情，并与增援剧情串行播放。</summary>
    public void OnBurstChargeChanged()
    {
        if (!initialized || readyShown || battle.PlayerController.burstCharge < battle.PlayerController.maxBurstCharge) return;
        readyPending = true;
        if (Step == TrainingStep.SecondBattle && presentation == null) presentation = StartCoroutine(BurstReadyRoutine(lifetime));
    }

    private IEnumerator BurstReadyRoutine(int version)
    {
        yield return null;
        while (Current(version) && !Stable) yield return null;
        if (!Current(version)) yield break;
        if (Step == TrainingStep.SecondBattle) yield return PresentBurstReady(version);
        if (!Current(version)) yield break;
        presentation = null;
        RestoreActorMenu();
        if (battle.AIController.pieces.All(p => p == null || p.isDead)) HoldVictory();
    }

    private IEnumerator PresentBurstReady(int version)
    {
        readyShown = true;
        presentingReady = true;
        yield return PlayLog(burstReadyLog, version);
        if (!Current(version)) yield break;
        yield return ShowPrompt("聚能", "聚能槽已满。点击聚能按钮发动；一次消耗50点，聚能期间可切换三名主角行动。利用聚能击败黑衣人。", version);
        presentingReady = false;
    }

    private IEnumerator PlayLog(string log, int version)
    {
        bool closed = false;
        if (hint != null) hint.gameObject.SetActive(false);
        battle.battleDialogueManager.StartLog(log, () => { if (Current(version)) closed = true; });
        while (Current(version) && !closed) yield return null;
        if (Current(version) && hint != null) hint.gameObject.SetActive(true);
    }

    private IEnumerator ShowPrompt(string title, string body, int version)
    {
        bool closed = false;
        if (hint != null) hint.gameObject.SetActive(false);
        var panel = battle.tutorialManager.tutorialPanel;
        var illustration = title == "聚能" ? panel.database?.GetTutorial("PR0BATTLE-1")?.pages.Find(p => p.title == "聚能")?.image : null;
        panel.ShowTransient(title, body, () => { if (Current(version)) closed = true; }, illustration);
        while (Current(version) && !closed) yield return null;
        if (Current(version) && hint != null) hint.gameObject.SetActive(true);
    }

    /// <summary>仅本场提供教学词条访问，不改永久解锁记录。</summary>
    public MemeProgress EquipmentProgress(MemeProgress original)
    {
        if (!initialized || Step != TrainingStep.EquipMeme && Step != TrainingStep.FinalBattle) return original;
        var progress = new MemeProgress { unlockedIds = original?.unlockedIds != null ? new List<int>(original.unlockedIds) : new List<int>() };
        if (!progress.IsUnlocked(dragonMemeId)) progress.unlockedIds.Add(dragonMemeId);
        return progress;
    }

    /// <summary>装配服务与词条按钮共用限制；其他词条和清空槽位在强制教学期间不可提交。</summary>
    public bool AllowsMeme(PieceController actor, int id) => !RestrictsCommands ||
        Step == TrainingStep.EquipMeme && actor == memeActor && id == dragonMemeId && !finishingEquipment;

    /// <summary>装备提交必须包含目标词条，并保持其他槽位；不接受外部入口替换整套无关配置。</summary>
    public bool AllowsEquipment(PieceController actor, IReadOnlyList<int> slots)
    {
        if (!RestrictsCommands) return true;
        if (!AllowsMeme(actor, dragonMemeId) || slots == null || !slots.Contains(dragonMemeId)) return false;
        var previous = MemeEquipment.CopySlots(actor.playerData.memeSlots);
        for (int i = 0; i < slots.Count; i++) if (slots[i] != dragonMemeId && (i >= previous.Length || slots[i] != previous[i])) return false;
        return true;
    }

    /// <summary>正式装配成功后显示装置攻击提示；不要求概率词条实际触发才解除限制。</summary>
    public void OnMemeEquipped(PieceController actor)
    {
        if (Step != TrainingStep.EquipMeme || actor != memeActor || finishingEquipment || actor.playerData?.memeSlots?.Contains(dragonMemeId) != true) return;
        finishingEquipment = true;
        StartCoroutine(CompleteMemeTeaching(lifetime));
    }

    private IEnumerator CompleteMemeTeaching(int version)
    {
        yield return null;
        while (Current(version) && !Stable) yield return null;
        if (!Current(version)) yield break;
        yield return ShowPrompt("模因技能与装置", "屠龙术已装备。使用携带模因的攻击命中方尖碑装置；屠龙术成功触发时可摧毁方尖碑并向附近黑衣人传播。", version);
        if (!Current(version)) yield break;
        finishingEquipment = false;
        SetStep(TrainingStep.FinalBattle, "消灭剩余敌人和方尖碑。");
        RestoreActorMenu();
        battle.PlayerCheckWin();
    }

    private void SetStep(TrainingStep next, string text)
    {
        Step = next;
        if (movementTarget != null) movementTarget.gameObject.SetActive(next == TrainingStep.Move);
        if (hint == null) hint = SpaceStationTrainingHint.Create(battle.tutorialManager.tutorialPanel.inforText);
        hint.SetInstruction(text);
        battle.PlayerController.RefreshBurstUI();
    }

    private void ShowMovementMarker()
    {
        if (movementTarget == null) return;
        var line = movementTarget.gameObject.AddComponent<LineRenderer>();
        markerMaterial = new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial = markerMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 40;
        line.widthMultiplier = .09f;
        line.startColor = line.endColor = new Color(1f, .8f, .3f);
        line.sortingLayerName = "Piece";
        line.sortingOrder = 105;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2 / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * .65f, .08f, Mathf.Sin(angle) * .65f));
        }
    }

    private void RestoreActorMenu()
    {
        if (battle != null && battle.CanControlPiece(battle.ActivePiece) && !battle.ActivePiece.IsUsingSkill)
            BattleScene.Ins.CM.SelectActivePiece(battle.ActivePiece);
    }

    private void LateUpdate()
    {
        if (initialized && deviceIcon != null && deviceIcon.gameObject.activeInHierarchy) UpdateDeviceIcon();
    }

    private void UpdateDeviceIcon()
    {
        if (deviceBody == null || deviceRenderers == null) return;
        // 逐帧素材加载和姿势可能改变边界，图标须跟随当前完整模型的投影顶部。
        var bounds = deviceBody.bounds;
        foreach (var renderer in deviceRenderers)
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy) bounds.Encapsulate(renderer.bounds);
        var up = Camera.main != null ? Camera.main.transform.up : Vector3.up;
        float extent = Mathf.Abs(up.x) * bounds.extents.x + Mathf.Abs(up.y) * bounds.extents.y + Mathf.Abs(up.z) * bounds.extents.z;
        deviceIcon.position = bounds.center + up * (extent + .6f);
    }

    private void OnDisable()
    {
        lifetime++;
        initialized = false;
        Step = TrainingStep.Cancelled;
        StopAllCoroutines();
        presentation = null;
        teachingAttack = null;
        pendingWave = 0;
        presentingReady = false;
        finishingEquipment = false;
        teachingImpact = false;
        deviceIcon = null;
        deviceBody = null;
        deviceRenderers = null;
        // 临时解锁不进入战外存档；原本已永久拥有的词条及正常装配保持原规则。
        if (GM.Ins?.PLAYERPROFILE?.memeProgress?.IsUnlocked(dragonMemeId) != true)
            foreach (var pair in originalSlots)
                for (int i = 0; i < (pair.Key.memeSlots?.Length ?? 0); i++)
                    if (pair.Key.memeSlots[i] == dragonMemeId) pair.Key.memeSlots[i] = i < pair.Value.Length ? pair.Value[i] : 0;
        originalSlots.Clear();
        if (hint != null) Destroy(hint.gameObject);
        if (markerMaterial != null) Destroy(markerMaterial);
    }
}
