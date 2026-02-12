using UnityEngine;

/// <summary>
/// 假死技能
/// </summary>
public class FakeDeath : MonoBehaviour
{
    
    public void OnDead()
    {
        // 死亡时保留1点生命值并不受普通伤害
    }

    public void OnTurnStart()
    {
        // 回合开始时恢复一定比例生命值
    }
}