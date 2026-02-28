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
    public bool deadNotDelete = false; // 死亡后不删除，用于剧情需要
    //public bool ableFakeDeath = false; // 是否具有假死能力
    //private bool isFakeDead = false; // 是否处于假死状态
    //public bool FakeDead => isFakeDead;

    public Dictionary<PieceController, int> damageDic = new(); // 记录各个单位造成的伤害

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
        Debug.Log($"{this.name} 死亡");
        OnDead?.Invoke();
        isActived = false;
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

        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            Debug.Log($"技能命中数量{targets.Count}");
            BattleScene.Ins.BM.PieceSkill(this, targets, skill, targets[0].transform.position);
            rangeUI?.CloseRange();
        }, false);
        // 技能聚能充能
    }

    public void CastAttackOnTarget(PieceController targetPc)
    {
        if (_curAttackPack == null || targetPc == null) return;
        Debug.Log($"{this.name} 对 {targetPc.name} 施放攻击 {_curAttackPack.skillName}");

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
            PlayAudio(ActionType.近战攻击);
        }
        else if (_curAtkType == ActionType.远程攻击)
        {
            pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
            // 消耗弹药
            unitAttrCenter.CostAmmo();
            PlayAudio(ActionType.远程攻击);
        }

        // 延迟0.3f
        DOVirtual.DelayedCall(0.3f, () =>
        {
            Debug.Log($"攻击命中数量{targets.Count}");
            BattleScene.Ins.BM.PieceSkill(this, targets, _curAttackPack);
            rangeUI?.CloseRange();
        }, false);
        // 技能聚能充能
    }

    #endregion

    private void CheckDrop()
    {
        if (pieceData.dropItemList!=null && pieceData.dropItemList.Count > 0)
        {
            // 按照概率随机
            if (GameConst.CheckRate(pieceData.dropRate))
            {
                foreach (var dropItem in pieceData.dropItemList)
                {
                    // 添加道具到存档里
                    GM.Ins.PLAYERPROFILE.AddItem(dropItem.itemName, dropItem.itemNum);
                }
            }
        }
    }
}