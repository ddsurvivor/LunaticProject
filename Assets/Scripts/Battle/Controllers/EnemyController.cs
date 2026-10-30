using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敌人单位棋子控制器
/// </summary>
public partial class EnemyController : PieceController
{
    [Sirenix.OdinInspector.LabelText("敌人等级"), Range(1, 100)]
    public int level = 1;
    public int Level => Mathf.Clamp(level, EnemyLevelGrowth.MinLevel, EnemyLevelGrowth.MaxLevel);

    [Sirenix.OdinInspector.LabelText("基础行动策略")]
    public EnemyAIType enemyAIType;
    public bool isActived = false; // 是否被激活

    public bool navigate;

    /// 是否正在导航中
    //public bool ableFakeDeath = false; // 是否具有假死能力
    //private bool isFakeDead = false; // 是否处于假死状态
    //public bool FakeDead => isFakeDead;
    public EnemyCanvas enemyCanvas; // 敌人专用UI画布，包含血条、buff等显示组件

    public Dictionary<PieceController, int> damageDic = new(); // 记录各个单位造成的伤害

    public LineRenderer tagetLine; // 目标指示线
    private PieceController _curTargetPc; // 当前攻击目标


    public override void TurnStart()
    {
        base.TurnStart();
        if (enemyCanvas != null) enemyCanvas.UpdateBuffs(unitAttrCenter.buffStates);
        if (!isActived) return;
        if (enemyCanvas != null) enemyCanvas.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
    }

    // 添加伤害记录
    public void AddDamageRecord(PieceController pc, int damage)
    {
        if (damageDic.ContainsKey(pc))
        {
            damageDic[pc] += damage;
        }
        else
        {
            damageDic[pc] = damage;
        }
    }

    public override void Dead()
    {
        if (HasQueuedBurstDeath) return;
        MemeState.ClearNetworkLinks();
        BattleScene.Ins?.BM?.timedMemeStatuses.ClearForPiece(this);
        CancelCombatAction();
        StopHitShake();
        isActived = false;
        if (DeferBurstDeath(CompleteEnemyDeath)) return;
        CompleteEnemyDeath();
    }

    private void CompleteEnemyDeath()
    {
        BattleScene.Ins.TM.RequestHitStop();
        Debug.Log($"{this.name} 死亡");
        OnDead?.Invoke();
        isActived = false;
        enemyCanvas?.gameObject.SetActive(false);
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, false, -1, () =>
        {
            CheckDrop();
            if (!deadNotDelete)
            {
                BattleScene.Ins.BM.FadeDeadPiece(this);
            }
            else
            {
                BattleScene.Ins.BM.PlayerCheckWin();
            }
        });
    }

    #region 敌人攻击

    public void CastSkillOnTarget(PieceController targetPc, SkillPack skill)
    {
        if (IsPerformingAction || isDead || !BattleRelations.CanAttack(this, targetPc) || !TryGetAttackPosition(targetPc, skill, out var position)) return;
        var targets = BattleScene.Ins.BM.skillManager.GetTargets(this, position, skill);
        if (targets.Count == 0) return;
        BeginCombatAction(skill, targets, position, ActionType.Skill, PieceDisplayState.Skill);
        NotifySkillUsed(skill);
        rangeUI?.CloseRange();
    }

    /// <summary>回复计划复用正式技能动画与结算，区域回复允许没有直接命中目标。</summary>
    public void CastSupportSkill(PieceController target, SkillPack skill)
    {
        if (IsPerformingAction || isDead || target == null || skill == null) return;
        Vector3 position = target.transform.position;
        var targets = SkillTargeting.Query(this, skill, position);
        if (skill.target != SkillTarget.Area && !targets.Contains(target)) return;
        BeginCombatAction(skill, targets, position, ActionType.Skill, PieceDisplayState.Skill);
        NotifySkillUsed(skill);
        rangeUI?.CloseRange();
    }

    /// <summary>结算已选定的普通攻击，保留原单参数入口供场景事件和 AI 调用。</summary>
    public void CastAttackOnTarget(PieceController targetPc) => CastAttackOnTarget(targetPc, false);

    /// <summary>夹击和警戒标记反应来源，沿用原模因限制与范围查询。</summary>
    public void CastAttackOnTarget(PieceController targetPc, bool isMemeReaction) =>
        CastAttackOnTarget(targetPc, isMemeReaction, false);

    /// <summary>反应须标记isMemeReaction；警戒捕获的单体目标保留经过事实，范围攻击仍查询实际范围。</summary>
    public void CastAttackOnTarget(PieceController targetPc, bool isMemeReaction, bool capturedOrderTarget)
    {
        if (IsPerformingAction || isDead || !BattleRelations.CanAttack(this, targetPc) || !TryGetAttackPosition(targetPc, _curAttackPack, out var position)) return;
        var skill = _curAttackPack;
        var action = _curAtkType;
        var targets = capturedOrderTarget && skill.target == SkillTarget.Enemy
            ? new List<PieceController> { targetPc }
            : BattleScene.Ins.BM.skillManager.GetTargets(this, position, skill);
        if (targets.Count == 0) return;
        bool ranged = action == ActionType.RangedAttack;
        if (ranged && unitAttrCenter.AmmoCount <= 0) return;
        if (ranged) unitAttrCenter.CostAmmo();
        BeginCombatAction(skill, targets, position, action,
            ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack, isMemeReaction: isMemeReaction);
        rangeUI?.CloseRange();
    }

    #endregion

    public void CastOnExplosive(IEnemyExplosiveTarget explosive, SkillPack skill, ActionType action)
    {
        if (IsPerformingAction || !EnemyExplosiveTargets.CanAttack(this, explosive, skill)) return;
        var attack = skill.WithTarget(SkillTarget.All);
        var position = explosive.Piece.transform.position;
        var targets = SkillTargeting.Query(this, attack, position);
        // 仅允许本次选中的环境目标成为友方命中对象，其他目标沿用原技能阵营规则。
        targets.RemoveAll(target => target != explosive.Piece && !SkillTargeting.IsValid(this, target, skill));
        if (!EnemyAttackPosition.IsAOE(skill)) targets.RemoveAll(target => target != explosive.Piece);
        if (!targets.Contains(explosive.Piece)) return;
        bool ranged = action == ActionType.RangedAttack;
        if (ranged && unitAttrCenter.AmmoCount <= 0) return;
        if (ranged) unitAttrCenter.CostAmmo();
        BeginCombatAction(attack, targets, position, action, action == ActionType.Skill ? PieceDisplayState.Skill :
            ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack);
        if (action == ActionType.Skill) NotifySkillUsed(skill);
        rangeUI?.CloseRange();
    }

    private bool TryGetAttackPosition(PieceController target, SkillPack skill, out Vector3 position)
    {
        position = transform.position;
        if (maximizeAOETargets && EnemyAttackPosition.IsAOE(skill))
            return EnemyAttackPosition.TryFind(this, target, skill, out position);
        if (!SkillTargeting.IsValid(this, target, skill)) return false;
        position = target.transform.position;
        return true;
    }

    private void CheckDrop()
    {
        if (pieceData.dropItemList != null && pieceData.dropItemList.Count > 0)
        {
            // 按照概率随机
            if (GameConst.CheckRate(pieceData.dropRate))
            {
                foreach (var dropItem in pieceData.dropItemList)
                {
                    // 添加道具到存档里
                    GM.Ins.PLAYERPROFILE.AddItem(dropItem.itemName, dropItem.itemNum);
                    // 显示提示
                    BattleScene.Ins.UM.ShowItemGet(dropItem);
                }
            }
        }
    }

    public override void OnBeTarget(PieceController attacker, SkillPack skillPack)
    {
        if (isActived)
        {
            if (enemyCanvas != null)
            {
                enemyCanvas.hitInfoPanel.UpdateDisplay(attacker, skillPack, this);
                enemyCanvas.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
            }
        }
    }

    public override void ShowHighlight(bool option)
    {
        base.ShowHighlight(option);
        if (!option && enemyCanvas != null)
        {
            enemyCanvas.hitInfoPanel.gameObject.SetActive(false);
        }
    }

    public void UpdateHpBar(float hpPercent)
    {
        if (enemyCanvas != null)
        {
            enemyCanvas.hpBarUI.UpdateHpBar(hpPercent);
            enemyCanvas.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
        }
    }


}
