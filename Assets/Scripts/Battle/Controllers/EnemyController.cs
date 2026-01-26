using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敌人单位棋子控制器
/// </summary>
public class EnemyController : PieceController
{
    public EnemyAIType enemyAIType;
    public bool isActived = false;// 是否被激活
    public bool deadNotDelete = false;// 死亡后不删除，用于剧情需要
   
   public Dictionary<PieceController, int> damageDic = new();// 记录各个单位造成的伤害

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
       pieceDisplay.ChangeDisplayState(PieceDisplayState.Death, false, -1, () =>
       {
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
   
   public void CastSkillOnTarget(PieceController targetPc, SkillPack skill)
   {
       if (skill == null || targetPc == null) return;
       Debug.Log($"{this.name} 对 {targetPc.name} 施放技能 {skill.skillName}");
       
       // 根据范围获取所有棋子
       List<PieceController> targets = rangeUI.GetCurTargets;
       Transform atkPos = rangeUI.GetSkillTransform();
       if (atkPos != null && skill.skillVFXType != 0)
       {
           ObjectPool.Ins.GenerateObject(
               skill.skillVFXType,
               atkPos.position+Vector3.up*3f,
               atkPos.localRotation);
       }
       // 播放技能动画
       pieceDisplay.ChangeDisplayState(PieceDisplayState.Shoot, false, 1f);
       PlayAudio(skill);
       
       // 延迟0.3f
       DOVirtual.DelayedCall(0.3f, () =>
       {
           BattleScene.Ins.BM.PieceSkill(this, targets, skill);
       }, false);
       rangeUI.CloseRange();
       // 技能聚能充能
   }
}