public class BuffState
{
    public BuffType buffType;

    // 层数
    public int stacks;
    public BuffState(BuffType buffType, int stacks)
    {
        this.buffType = buffType;
        this.stacks = stacks;
    }
}