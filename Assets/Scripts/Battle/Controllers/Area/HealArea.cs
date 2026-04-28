using System;
using UnityEngine;

public class HealArea : MonoBehaviour
{
    public BuffPack buffPack;
    public float range;
    public int turnsDuration;
    public void SetData(BuffPack buffPack, int turnsDuration, float range)
    {
        this.buffPack = buffPack;
        this.turnsDuration = turnsDuration;
        BattleScene.Ins.BM.areaList.Add(this);
        this.range = range;
        transform.localScale = Vector3.one * range;
        Heal();
    }
    public void AddBuff()
    {
        // 给范围内的友方单位添加治疗buff
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        foreach (var collider in colliders)
        {
            PieceController pc = collider.GetComponent<PieceController>();
            if (pc != null && pc.isPlayerPiece && !pc.isDead)
            {
                if (GameConst.CheckRate(buffPack.rate))
                {
                    BattleScene.Ins.BM.buffManager.AddBuff(pc.unitAttrCenter, buffPack.buffType
                        , buffPack.stacks);
                }
            }
        }
    }
    public void AddBuffToUnit(PieceController pc)
    {
        if (pc != null && pc.isPlayerPiece && !pc.isDead)
        {
            if (GameConst.CheckRate(buffPack.rate))
            {
                BattleScene.Ins.BM.buffManager.AddBuff(pc.unitAttrCenter, buffPack.buffType
                    , buffPack.stacks);
            }
        }
    }

    public void Heal()
    {
        // 治疗范围内的所有友军
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        foreach (var collider in colliders)
        {
            PieceController pc = collider.GetComponent<PieceController>();
            if (pc != null && pc.isPlayerPiece && !pc.isDead)
            {
                pc.unitAttrCenter.Heal(10);// 治疗10点生命值
                if (GameConst.CheckRate(buffPack.rate))// 给范围内的友方单位添加治疗buff
                {
                    BattleScene.Ins.BM.buffManager.AddBuff(pc.unitAttrCenter, buffPack.buffType
                        , buffPack.stacks);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 显示治疗范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}