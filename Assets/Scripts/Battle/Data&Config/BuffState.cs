[System.Serializable]
public class BuffState
{
    public BuffType buffType;

    // 层数
    public int stacks;
    public int barrierHealth;
    public int retaliationTurns;
    // 仅结算本回合开始时已存在的持续层数；回合中新增状态不提前扣层。
    [System.NonSerialized] public bool durationPending;
    [System.NonSerialized] public System.Collections.Generic.HashSet<object> sources = new();
    public BuffState(BuffType buffType, int stacks)
    {
        this.buffType = buffType;
        this.stacks = stacks;
    }
}
