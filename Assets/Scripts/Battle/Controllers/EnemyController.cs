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
        CancelCombatAction();
        StopHitShake();
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
                pieceDisplay.pieceSpriteRenderer.DOFade(0f, 0.8f).OnComplete(() =>
                {
                    this.gameObject.SetActive(false);
                    BattleScene.Ins.BM.PlayerCheckWin();
                }).SetUpdate(UpdateType.Normal);
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
        if (IsPerformingAction || isDead || !SkillTargeting.IsValid(this, targetPc, skill)) return;
        var targets = BattleScene.Ins.BM.skillManager.GetTargets(this, targetPc.transform, skill);
        if (targets.Count == 0) return;
        BeginCombatAction(skill, targets, targetPc.transform.position, ActionType.技能, PieceDisplayState.Skill);
        NotifySkillUsed(skill);
        rangeUI?.CloseRange();
    }

    public void CastAttackOnTarget(PieceController targetPc)
    {
        if (IsPerformingAction || isDead || !SkillTargeting.IsValid(this, targetPc, _curAttackPack)) return;
        var skill = _curAttackPack;
        var action = _curAtkType;
        var targets = BattleScene.Ins.BM.skillManager.GetTargets(this, targetPc.transform, skill);
        if (targets.Count == 0) return;
        bool ranged = action == ActionType.远程攻击;
        if (ranged && unitAttrCenter.AmmoCount <= 0) return;
        if (ranged) unitAttrCenter.CostAmmo();
        BeginCombatAction(skill, targets, targetPc.transform.position, action,
            ranged ? PieceDisplayState.Shoot : PieceDisplayState.Attack);
        rangeUI?.CloseRange();
    }

    #endregion

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
