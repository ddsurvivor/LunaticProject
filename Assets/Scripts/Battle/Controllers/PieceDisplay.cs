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

    // 每次播放独立持有进度和回调，旧播放不能完成新动作。
    public sealed class Playback
    {
        public bool IsDone { get; internal set; }
        public bool IsCancelled { get; internal set; }
        public bool HasReleased { get; internal set; }
        internal List<Sprite> Frames;
        internal float Elapsed, FrameDuration, Duration;
        internal bool Loop, ReturnToIdle;
        internal UnityAction Release, Finish;
    }

    private const float frameDuration = 1f / 6f;
    private const float dodgeDuration = 1f; // 闪避统一播放一秒，不随素材帧数变化。
    private Playback playback;
    private PieceDisplayState state;
    private Vector3 dodgeOrigin; // 闪避位移前的位置，结束或中断时必须恢复。
    private bool dodgeOffset;
    private bool pendingReaction;
    private PieceDisplayState pendingReactionState;
    private bool pendingReactionReturns;
    public bool BurstFrozen { get; private set; }
    public bool IsPlayingOneShot => !BurstFrozen && playback != null && !playback.Loop && !playback.IsDone;
    private bool IsAction => state == PieceDisplayState.Attack || state == PieceDisplayState.Shoot || state == PieceDisplayState.Skill;

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
        StartPlayback(next, GetFrames(next, index), back || duration > 0f, finish, null, duration);
    }

    /// <summary>自动以序列中点作为出手帧，不需要逐个角色增加动画事件。</summary>
    public Playback PlayAction(PieceDisplayState next, UnityAction release, int index = 0)
    {
        return StartPlayback(next, GetFrames(next, index), true, null, release, -1f);
    }

    private List<Sprite> GetFrames(PieceDisplayState next, int index)
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

    private Playback StartPlayback(PieceDisplayState next, List<Sprite> frames, bool back,
        UnityAction finish, UnityAction release, float duration)
    {
        StopAnimation();
        state = next;
        playback = new Playback
        {
            Frames = frames, FrameDuration = next == PieceDisplayState.Dodge
                ? dodgeDuration / Math.Max(1, frames?.Count ?? 0)
                : next == PieceDisplayState.Move ? frameDuration / 2f : frameDuration,
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

    private void Update()
    {
        if (BurstFrozen) return;
        var current = playback;
        if (current == null || current.IsDone || Time.deltaTime <= 0f) return;
        current.Elapsed += Time.deltaTime;
        int count = pieceSpriteRenderer != null && current.Frames != null ? current.Frames.Count : 0;
        int frame = Mathf.FloorToInt(current.Elapsed / current.FrameDuration);
        if (dodgeOffset)
        {
            // 单张或缺失闪避素材时也用横移表达闪身，且不改变棋子的碰撞与导航位置。
            float progress = Math.Min(1f, current.Elapsed / (current.FrameDuration * Math.Max(1, count)));
            float direction = pieceSpriteRenderer.transform.localScale.x < 0 ? 1f : -1f;
            pieceSpriteRenderer.transform.localPosition = dodgeOrigin + Vector3.right *
                ((float)Math.Sin(progress * Math.PI) * 0.3f * direction);
        }
        if (count > 0)
            pieceSpriteRenderer.sprite = current.Frames[current.Loop ? frame % count : Mathf.Min(frame, count - 1)];

        // 即使低帧率跳过出手帧，仍只发送一次出手事件。空动画在下一次有效更新正常出手。
        if (!current.HasReleased && (count == 0 || frame >= count / 2))
        {
            current.HasReleased = true;
            current.Release?.Invoke();
            if (playback != current) return;
        }
        bool finished = count == 0 || (!current.Loop && frame >= count) ||
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
        StartPlayback(next, GetFrames(next, 0), next != PieceDisplayState.Death, null, null, -1);
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
