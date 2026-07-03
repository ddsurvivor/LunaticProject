using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 技能特殊效果基类，所有技能特殊效果都继承自这个类
/// </summary>
public class SkillEffectBase
{
    
}

/// <summary>
/// 击退效果，造成伤害的同时将目标击退一定距离
/// </summary>
public class HitBackEffect : SkillEffectBase
{
    [LabelText("击退距离")]
    public float dis;// 击退距离
    [LabelText("碰撞伤害")]
    public int hitBackDamage;// 击退伤害
}

// 生成设计特效的特殊技能
public class ShootFxEffect : SkillEffectBase
{
    public ItemType shootFxType;// 射击特效类型
    public bool isRotate;
    
    public void ApplyEffect(PieceController attacker, Vector3 targetPos)
    {
        // 在射击点生成特效
        GameObject fx = ObjectPool.Ins.GenerateObject(shootFxType, targetPos, Quaternion.identity);
        // 根据设置决定是否旋转特效
        
        
        if (isRotate)
        {
            //z轴旋转，从attacker 指向targetPos
            Vector3 direction = targetPos - attacker.transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            fx.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Debug.Log($"生成射击特效 {shootFxType} at {targetPos}, rotate: {angle}");
        }
        else
        {
            fx.transform.rotation = Quaternion.identity;
        }
    }
}

// 自爆技能
public class SelfExplosionEffect : SkillEffectBase
{
    public void ApplyEffect(PieceController attacker)
    {
        // 对自身造成伤害
        int selfDamage = 100; // 示例伤害值，可以根据需要调整
        attacker.unitAttrCenter.TakeDamage(new AttackPack(selfDamage,DamageType.Ranged));
    }
}

/// <summary>
/// 召唤扩展效、
/// </summary>
public class SummonEffect : SkillEffectBase
{
    [LabelText("召唤棋子编号")]
    public int summonPieceId;
    public ItemType summonPieceObj;

    public void ApplyEffect(Vector3 pos, PlayerController summoner)
    {
        // 在指定地点生成召唤棋子，并且初始化
        PieceController summonPiece = ObjectPool.Ins.GenerateObject(summonPieceObj, pos, Quaternion.identity)
            ?.GetComponent<PieceController>();
        if(summonPiece== null) return;
        PieceData pieceData = BattleScene.Ins.BM.pieceDataListSO.GetPieceData(summonPieceId);
        summonPiece.Init(summoner, pieceData);
        summoner.pieces.Add(summonPiece);// 将召唤的棋子加入召唤者的棋子列表
    }
}

public class ReviveEffect: SkillEffectBase
{
    [LabelText("复活回血比例【%】")]
    public int reviveHealthPercent = 50;

    public void ApplyEffect(PieceController taget)
    {
        // 将棋子复活，恢复一定量的生命值，显示动画为Idle状态
        int healAmount = (int)(taget.unitAttrCenter.MaxHealth * reviveHealthPercent / 100f);
        taget.unitAttrCenter.Heal(healAmount);
        taget.pieceDisplay.ChangeDisplayState(PieceDisplayState.Idle);
    }
}