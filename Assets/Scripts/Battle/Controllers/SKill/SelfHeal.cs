using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfHeal : MonoBehaviour
{
    
    public void FullHp()
    {
        var piece = GetComponent<EnemyController>();
        if (piece != null)
        {
            piece.unitAttrCenter.FullHealth();
            GM.Ins.AM.PlayAudio(AudioCueType.Heal);
        }
    }
}
