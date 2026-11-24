using System;using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class PieceDisplay : SerializedMonoBehaviour
{
    
    // 棋子图片控制脚本，有一个SpriteRenderer用于显示棋子图片
    public SpriteRenderer pieceSpriteRenderer;
    [LabelText("头顶 UI 锚点")] public Transform overheadAnchor; // 随棋子预制体保存，按不同身高手动调整。
    // 所有美术资源现已全部统一为 List<Sprite> 序列帧
    public List<Sprite> idleSprite;
    public List<Sprite> moveSprite;
    public List<Sprite> meleeSprites;
    public List<Sprite> rangeSprites;
    public List<Sprite> dodgeSprite;
    public List<Sprite> hitSprite;
    public List<Sprite> deathSprites;
    [OdinSerialize]
    public List<List<Sprite>> skillSpriteList = new();
    [LabelText("出手时机覆盖")] public List<ActionReleaseTiming> releaseTimings = new();

    // 每次播放独立持有进度和回调，旧播放不能完成新动作。
    public sealed class Playback
    {
        public bool IsDone { get; internal set; }
        public bool IsCancelled { get; internal set; }
        public bool HasReleased { get; internal set; }
        internal List<Sprite> Frames;
        internal float Elapsed, FrameDuration, Duration, ReleaseTime;
        internal PieceAnimationTimeline Timeline;
        internal bool Loop, ReturnToIdle;
        internal UnityAction Release, Finish;
    }

    public const float FramesPerSecond = 12f; // 正式动画与编辑器预览共用的序列帧率。
    public const float FrameDuration = 1f / FramesPerSecond;
    private Playback playback;
    private PieceDisplayState state;
    private Vector3 dodgeOrigin; // 闪避位移前的位置，结束或中断时必须恢复。
    private bool dodgeOffset;
    private bool pendingReaction;
    private PieceDisplayState pendingReactionState;
    private bool pendingReactionReturns;
    private bool capturedOpacity;
    private float initialOpacity; // 首次初始化时的预制体透明度，复用时保留原有美术配置。
    public bool BurstFrozen { get; private set; }
    public bool IsPlayingOneShot => !BurstFrozen && playback != null && !playback.Loop && !playback.IsDone;
    private bool IsAction => state == PieceDisplayState.Attack || state == PieceDisplayState.Shoot || state == PieceDisplayState.Skill;

    /// <summary>棋子初始化时停止上一生命的动画及淡出，恢复首次初始化的精灵透明度。</summary>
    public void ResetForReuse()
    {
        StopAnimation();
        if (pieceSpriteRenderer == null) return;
        var color = pieceSpriteRenderer.color;
        if (!capturedOpacity) { initialOpacity = color.a; capturedOpacity = true; }
        // 旧死亡淡出必须先取消，避免重新初始化后仍回写透明度或停用棋子。
        pieceSpriteRenderer.DOKill();
        color.a = initialOpacity;
        pieceSpriteRenderer.color = color;
    }

    /// <summary>
    /// 保留旧调用签名和序列帧数据。一次性动画的正 duration 仅表示播完回待机，
    /// 不再启动另一个计时器截断动画；循环动画仍可按 duration 结束。
    /// </summary>
    public void ChangeDisplayState(PieceDisplayState next, bool back = false, float duration = -1f,
        UnityAction finish = null, int index = 0)
    {
        if (IsPlayingOneShot)
        {
            // 移动/选择结束的 Idle 请求不得剪掉攻击、受击或死亡动画。
            if (next == PieceDisplayState.Idle || next == PieceDisplayState.Move) return;
            if ((next == PieceDisplayState.Hit || next == PieceDisplayState.Dodge) && IsAction)
            {
                pendingReaction = true;
                pendingReactionState = next;
                pendingReactionReturns = back || duration > 0f;
                return;
            }
            // 同一帧的多种伤害包只启动一次受击，不反复从第一帧播放。
            if ((next == PieceDisplayState.Hit || next == PieceDisplayState.Dodge) && state == next) return;
        }
        StartPlayback(next, GetFrames(next, index), back || duration > 0f, finish, null, duration, GetTimeline(next, index));
    }

    /// <summary>使用角色技能覆盖或动画默认出手点；未配置时保留序列中点，每次播放快照计时。</summary>
    public Playback PlayAction(PieceDisplayState next, UnityAction release, int index = 0, string skillId = null)
    {
        var current = StartPlayback(next, GetFrames(next, index), true, null, release, -1f, GetTimeline(next, index, skillId));
        current.ReleaseTime = GetReleaseTime(next, index, skillId);
        return current;
    }

    /// <summary>获取正式动作序列，供战斗与编辑器预览共用；返回列表只供读取。</summary>
    public List<Sprite> GetFrames(PieceDisplayState next, int index = 0)
    {
        switch (next)
        {
            case PieceDisplayState.Idle: return idleSprite;
            case PieceDisplayState.Move: return moveSprite;
            case PieceDisplayState.Attack: return meleeSprites;
            case PieceDisplayState.Shoot: return rangeSprites;
            case PieceDisplayState.Dodge:
                return dodgeSprite != null && dodgeSprite.Any(sprite => sprite != null) ? dodgeSprite : idleSprite;
            case PieceDisplayState.Hit: return hitSprite;
            case PieceDisplayState.Death: return deathSprites;
            case PieceDisplayState.Skill:
                return skillSpriteList != null && index >= 0 && index < skillSpriteList.Count
                    ? skillSpriteList[index] : null;
            default: return null;
        }
    }

    /// <summary>按角色技能覆盖、角色动画默认、中点的优先级解析出手秒数。</summary>
    public float GetReleaseTime(PieceDisplayState next, int index = 0, string skillId = null)
    {
        var timeline = GetTimeline(next, index, skillId);
        var timing = FindReleaseTiming(next, index, skillId);
        if (timing == null && !string.IsNullOrEmpty(skillId)) timing = FindReleaseTiming(next, index, null);
        return timing == null ? timeline.GetFrameStart(timeline.Count / 2) : timing.GetSeconds(timeline);
    }

    /// <summary>快照每张图的停留时长；技能未设置时继承动画默认，空项沿用12 FPS。</summary>
    public PieceAnimationTimeline GetTimeline(PieceDisplayState next, int index = 0, string skillId = null)
    {
        var timing = FindReleaseTiming(next, index, skillId);
        if (timing?.frameDurations == null || timing.frameDurations.Count == 0)
            timing = FindReleaseTiming(next, index, null);
        return new PieceAnimationTimeline(GetFrames(next, index)?.Count ?? 0, timing?.frameDurations);
    }

    /// <summary>读取指定作用域的配置，不自动回退；空技能 ID 表示该动画的默认配置。</summary>
    public ActionReleaseTiming FindReleaseTiming(PieceDisplayState next, int index, string skillId)
    {
        return releaseTimings?.Find(t => t != null && t.action == next &&
            (next != PieceDisplayState.Skill || t.animationIndex == index) &&
            string.Equals(t.skillId ?? "", skillId ?? "", StringComparison.Ordinal));
    }

    private Playback StartPlayback(PieceDisplayState next, List<Sprite> frames, bool back,
        UnityAction finish, UnityAction release, float duration, PieceAnimationTimeline timeline = null)
    {
        StopAnimation();
        state = next;
        playback = new Playback
        {
            Frames = frames, FrameDuration = FrameDuration,
            Timeline = timeline ?? new PieceAnimationTimeline(frames?.Count ?? 0),
            Loop = next == PieceDisplayState.Idle || next == PieceDisplayState.Move,
            ReturnToIdle = back, Finish = finish, Release = release, Duration = duration
        };
        if (next == PieceDisplayState.Dodge && pieceSpriteRenderer != null)
        {
            dodgeOrigin = pieceSpriteRenderer.transform.localPosition;
            dodgeOffset = true;
        }
        if (pieceSpriteRenderer != null && frames != null && frames.Count > 0)
            pieceSpriteRenderer.sprite = frames[0];
        return playback;
    }

    private void Update() => AdvancePlayback(Time.deltaTime);

    /// <summary>推进正式动画时钟；编辑器仅对未绑定战斗回调的独立预览对象调用。</summary>
    public void AdvancePlayback(float deltaTime)
    {
        if (BurstFrozen) return;
        var current = playback;
        if (current == null || current.IsDone || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        current.Elapsed += deltaTime;
        int count = pieceSpriteRenderer != null && current.Frames != null ? current.Frames.Count : 0;
        int frame = current.Timeline.GetFrame(current.Elapsed, current.Loop);
        if (dodgeOffset)
        {
            // 单张或缺失闪避素材时也用横移表达闪身，且不改变棋子的碰撞与导航位置。
            float progress = Math.Min(1f, current.Elapsed / Math.Max(FrameDuration, current.Timeline.Duration));
            float direction = pieceSpriteRenderer.transform.localScale.x < 0 ? 1f : -1f;
            pieceSpriteRenderer.transform.localPosition = dodgeOrigin + Vector3.right *
                ((float)Math.Sin(progress * Math.PI) * 0.3f * direction);
        }
        if (count > 0)
            pieceSpriteRenderer.sprite = current.Frames[current.Loop ? frame % count : Mathf.Min(frame, count - 1)];

        // 即使低帧率跳过出手帧，仍只发送一次出手事件。空动画在下一次有效更新正常出手。
        if (!current.HasReleased && (count == 0 || current.Elapsed >= current.ReleaseTime))
        {
            current.HasReleased = true;
            current.Release?.Invoke();
            if (playback != current) return;
        }
        bool finished = count == 0 || (!current.Loop && current.Elapsed >= current.Timeline.Duration) ||
            (current.Loop && current.Duration > 0f && current.Elapsed >= current.Duration);
        if (!finished) return;
        current.IsDone = true;
        RestoreDodgePosition();
        playback = null;
        bool reaction = pendingReaction;
        bool reactionReturns = pendingReactionReturns;
        pendingReaction = false;
        if (reaction) ChangeDisplayState(pendingReactionState, reactionReturns);
        else if (current.ReturnToIdle) ChangeDisplayState(PieceDisplayState.Idle);
        // 先提交完成状态，再执行外部回调，允许回调安全地开启另一段动画。
        current.Finish?.Invoke();
    }

    public void Dead() => pieceSpriteRenderer.DOFade(0f, 0.5f);

    public void PlayFrame(List<Sprite> sprites, UnityAction finish = null)
    {
        StartPlayback(PieceDisplayState.Skill, sprites, false, finish, null, -1f);
    }

    public void FaceRight(bool faceRight)
    {
        Vector3 scale = pieceSpriteRenderer.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1);
        pieceSpriteRenderer.transform.localScale = scale;
    }

    /// <summary>冻结受击/死亡首帧，不占用一次性播放锁；逻辑回调由聚能队列持有。</summary>
    public void FreezeBurstFirstFrame(PieceDisplayState next)
    {
        if (BurstFrozen && state == PieceDisplayState.Death && next != PieceDisplayState.Death) return;
        StartPlayback(next, GetFrames(next, 0), next != PieceDisplayState.Death, null, null, -1, GetTimeline(next));
        BurstFrozen = true;
    }

    /// <summary>清理取消的聚能首帧，避免对象池复用后继续冻结。</summary>
    public void CancelBurstFreeze()
    {
        if (!BurstFrozen) return;
        StopAnimation();
        ChangeDisplayState(PieceDisplayState.Idle);
    }

    public void StopAnimation()
    {
        BurstFrozen = false;
        RestoreDodgePosition();
        if (playback != null && !playback.IsDone)
        {
            playback.IsCancelled = true;
            playback.IsDone = true;
        }
        playback = null;
        pendingReaction = false;
    }

    private void RestoreDodgePosition()
    {
        if (dodgeOffset && pieceSpriteRenderer != null)
            pieceSpriteRenderer.transform.localPosition = dodgeOrigin;
        dodgeOffset = false;
    }

    private void OnDisable() => StopAnimation();


}

public enum ActionReleaseMode { Frame, Seconds }

[Serializable]
public class ActionReleaseTiming
{
    public PieceDisplayState action = PieceDisplayState.Skill;
    public int animationIndex;
    public string skillId; // 空值是动画默认，否则仅覆盖这个角色的指定技能。
    public ActionReleaseMode mode;
    public int frame = 1; // 从1开始，选定帧开始时出手。
    public float seconds;
    public List<float> frameDurations = new(); // 各帧秒数；空列表或非正数沿用1/12秒，不重复复制图片。

    /// <summary>将配置换算为秒；旧配置越界时限于末帧开始，空序列立即出手。</summary>
    public float GetSeconds(int frameCount)
    {
        return GetSeconds(new PieceAnimationTimeline(frameCount));
    }

    /// <summary>帧模式累加前面各图时长；旧秒模式保留绝对秒数，不随停留时长重映射。</summary>
    public float GetSeconds(PieceAnimationTimeline timeline)
    {
        float maximum = timeline.GetFrameStart(timeline.Count - 1);
        if (mode == ActionReleaseMode.Frame)
            return timeline.GetFrameStart(frame - 1);
        return float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0f : Mathf.Clamp(seconds, 0f, maximum);
    }
}

public sealed class PieceAnimationTimeline
{
    private readonly float[] ends; // 每次播放持有独立累计时间，修改配置不影响在途动作。
    public int Count => ends.Length;
    public float Duration => Count == 0 ? 0f : ends[Count - 1];

    public PieceAnimationTimeline(int count, IList<float> durations = null)
    {
        ends = new float[Math.Max(0, count)];
        for (int i = 0; i < ends.Length; i++)
        {
            float duration = durations != null && i < durations.Count ? durations[i] : 0f;
            if (duration <= 0f || float.IsNaN(duration) || float.IsInfinity(duration)) duration = PieceDisplay.FrameDuration;
            ends[i] = (i == 0 ? 0f : ends[i - 1]) + duration;
        }
    }

    /// <summary>返回从0开始的图片开始秒数，越界限定到首末帧；空序列返回0。</summary>
    public float GetFrameStart(int index) => Count == 0 || index <= 0 ? 0f : ends[Math.Min(index, Count - 1) - 1];

    /// <summary>正式播放与编辑器共用的累计时间取帧，末图会保持至其停留时间结束。</summary>
    public int GetFrame(float seconds, bool loop = false)
    {
        if (Count == 0) return 0;
        if (loop && Duration > 0f) seconds %= Duration;
        for (int i = 0; i < Count; i++) if (seconds < ends[i]) return i;
        return Count - 1;
    }
}

public enum PieceDisplayState
{
    Idle,
    Move,
    Attack,
    Shoot,
    Dodge,
    Hit,
    Death,
    TrueDeath,
    Skill
}
