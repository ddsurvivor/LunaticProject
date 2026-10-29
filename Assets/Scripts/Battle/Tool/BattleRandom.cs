using UnityEngine;

/// <summary>战斗规则共用的随机入口；保留Unity的范围语义，状态包含整个Unity全局随机序列。</summary>
public static class BattleRandom
{
    /// <summary>保存随机状态，不消费随机数；恢复时也会回滚共用Unity序列的表现或剧情调用。</summary>
    public static Random.State CaptureState() => Random.state;
    /// <summary>恢复已保存的全局随机状态；应在无在途动作的边界调用。</summary>
    public static void RestoreState(Random.State state) => Random.state = state;
    /// <summary>返回包含下界、不包含上界的整数，沿用Unity范围语义。</summary>
    public static int Range(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    /// <summary>返回包含两端的浮点随机数，沿用Unity范围语义。</summary>
    public static float Range(float minInclusive, float maxInclusive) => Random.Range(minInclusive, maxInclusive);
    /// <summary>返回0至1的浮点随机数，不替换调用方现有概率比较符。</summary>
    public static float Value => Random.value;
}
