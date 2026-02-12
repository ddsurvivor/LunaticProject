using System;
using UnityEngine;

public class HealArea : MonoBehaviour
{
    public BuffPack buffPack;
    public float range;
    public int turnsDuration;
    public void SetData(BuffPack buffPack, int turnsDuration)
    {
        this.buffPack = buffPack;
        this.turnsDuration = turnsDuration;
        BattleScene.Ins.BM.areaList.Add(this);
        transform.localScale = Vector3.one * range;
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

    private void OnDrawGizmosSelected()
    {
        // 显示治疗范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}