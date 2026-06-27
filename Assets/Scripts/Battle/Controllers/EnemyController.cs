using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敌人单位棋子控制器
/// </summary>
public class EnemyController : PieceController
{
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
        if (!isActived) return;
        base.TurnStart();
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
                });
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
        if (skill == null || targetPc == null) return;
        Debug.Log($"{this.name} 对 {targetPc.name} 施放技能 {skill.skillName}");

        // 根据范围获取所有棋子
        List<PieceController> targets = BattleScene.Ins.BM.skillManager
            .GetTargets(this, targetPc.transform, skill);
        Transform atkPos = targetPc.transform;
        if (atkPos != null && skill.skillVFXType != 0)
        {
            ObjectPool.Ins.GenerateObject(
                skill.skillVFXType,
                atkPos.position + Vector3.up * 3f,
                atkPos.localRotation);
        }

        CheckFace(targetPc.transform.position - transform.position);
        // 播放技能动画
        pieceDisplay.ChangeDisplayState(PieceDisplayState.Skill, false, 1f);
        PlayAudio(skill);
        if (targets.Count > 0)
        {
            ShootBolt(targets[0].transform.position, skill.bulletVFXType);
        }

        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            if (targets == null || targets.Count == 0) return;
            Debug.Log($"技能命中数量{targets.Count}");
            BattleScene.Ins.BM.PieceSkill(this, targets, skill, targets[0].transform.position);
            enemyCanvas.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
            rangeUI?.CloseRange();
        }, false);
        // 技能聚能充能
    }

    public void CastAttackOnTarget(PieceController targetPc)
    {
        if (_curAttackPack == null || targetPc == null) return;
        Debug.Log($"{this.name} 对 {targetPc.name} 施放攻击{_curAtkType} - {_curAttackPack.skillName}");
        //_curAtkType = range ? ActionType.远程攻击 : ActionType.近战攻击; 
        // 根据范围获取所有棋子
        List<PieceController> targets = BattleScene.Ins.BM.skillManager
            .GetTargets(this, targetPc.transform, _curAttackPack);
        Transform atkPos = targetPc.transform;
        if (atkPos != null && _curAttackPack.skillVFXType != 0)
        {
            ObjectPool.Ins.GenerateObject(
                _curAttackPack.skillVFXType,
                atkPos.position + Vector3.up * 3f,
                atkPos.localRotation);
        }

        CheckFace(targetPc.transform.position - transform.position);
        if (_curAtkType == ActionType.近战攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Attack, false, 1f);
            PlayAudio(_curAttackPack);
        }
        else if (_curAtkType == ActionType.远程攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
            PlayAudio(_curAttackPack);
            if (targets.Count > 0)
            {
                ShootBolt(targets[0].transform.position, _curAttackPack.bulletVFXType);
            }
        }

        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            if (targets.Count == 0) return;
            Debug.Log($"攻击命中数量{targets.Count}");
            BattleScene.Ins.BM.PieceSkill(this, targets, _curAttackPack
                , targets[0].transform.position, _curAtkType);
            enemyCanvas.hpBarUI.UpdateMpIcons(unitAttrCenter.CurMovePoint);
            rangeUI?.CloseRange();
        }, false);
        // 技能聚能充能
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

    public override void ShowOutline(bool option)
    {
        base.ShowOutline(option);
        //Debug.Log($"ShowOutline {option} - {_curTargetPc?.name}");
        if (!option)
        {
            tagetLine.enabled = false;
            return;
        }
        /*// 显示攻击目标指示线
        if (player is AIController aiController)
        {
            _curTargetPc = aiController.CheckEnemyTarget(this);
        }
        UpdateTargetLine();*/
    }

    public void ShowTargetLine()
    {
        // 显示攻击目标指示线
        if (player is AIController aiController)
        {
            _curTargetPc = aiController.CheckEnemyTarget(this);
        }

        UpdateTargetLine();
    }

    // 在 EnemyController.cs 中添加
    private void UpdateTargetLine()
    {
        if (_curTargetPc != null && tagetLine != null)
        {
            Vector3 start = transform.position + Vector3.up * 1.5f; // 本棋子顶部
            Vector3 end = _curTargetPc.transform.position + Vector3.up * 1.5f; // 目标棋子顶部
            Vector3 control = (start + end) / 2 + Vector3.up * 2.5f; // 控制点：中点上移

            int segmentCount = 20;
            Vector3[] positions = new Vector3[segmentCount + 1];
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                // 二次贝塞尔曲线公式
                positions[i] = Mathf.Pow(1 - t, 2) * start +
                               2 * (1 - t) * t * control +
                               Mathf.Pow(t, 2) * end;
            }

            tagetLine.positionCount = positions.Length;
            tagetLine.SetPositions(positions);
            tagetLine.enabled = true;
        }
        else if (tagetLine != null)
        {
            tagetLine.positionCount = 0;
            tagetLine.enabled = false;
        }
    }
}