using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人单位棋子控制器
/// </summary>
public class EnemyController : PieceController
{
   public bool isActived = false;// 是否被激活
   
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
}