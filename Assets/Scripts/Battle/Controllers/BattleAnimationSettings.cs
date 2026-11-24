using UnityEngine;

/// <summary>全局棋子动画默认帧率；资源缺失或数值无效时使用内置默认值。</summary>
public sealed class BattleAnimationSettings : ScriptableObject
{
    [Min(0.01f)] public float normalFramesPerSecond = 6f; // 待机、移动、攻击、射击、闪避、受击和死亡。
    [Min(0.01f)] public float skillFramesPerSecond = 12f; // Skill 状态，不包含普通攻击和射击。
    private static BattleAnimationSettings cached;

    /// <summary>获取当前动作的默认每帧秒数；显式逐帧停留配置优先。</summary>
    public static float FrameDuration(PieceDisplayState state)
    {
        if (cached == null) cached = Resources.Load<BattleAnimationSettings>("BattleAnimationSettings");
        float fallback = state == PieceDisplayState.Skill ? 12f : 6f;
        float fps = cached == null ? fallback : state == PieceDisplayState.Skill ? cached.skillFramesPerSecond : cached.normalFramesPerSecond;
        if (fps <= 0f || float.IsNaN(fps) || float.IsInfinity(fps)) fps = fallback;
        return 1f / fps;
    }
}
