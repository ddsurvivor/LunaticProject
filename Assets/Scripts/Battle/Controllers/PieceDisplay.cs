using System;using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Reflection;
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
    private Playback playback;
    private PieceDisplayState state;
    private bool pendingReaction;
    private PieceDisplayState pendingReactionState;
    private bool pendingReactionReturns;
    public bool IsPlayingOneShot => playback != null && !playback.Loop && !playback.IsDone;
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
            case PieceDisplayState.Dodge: return dodgeSprite;
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
            Frames = frames, FrameDuration = next == PieceDisplayState.Move ? frameDuration / 2f : frameDuration,
            Loop = next == PieceDisplayState.Idle || next == PieceDisplayState.Move,
            ReturnToIdle = back, Finish = finish, Release = release, Duration = duration
        };
        if (pieceSpriteRenderer != null && frames != null && frames.Count > 0)
            pieceSpriteRenderer.sprite = frames[0];
        return playback;
    }

    private void Update()
    {
        var current = playback;
        if (current == null || current.IsDone || Time.deltaTime <= 0f) return;
        current.Elapsed += Time.deltaTime;
        int count = pieceSpriteRenderer != null && current.Frames != null ? current.Frames.Count : 0;
        int frame = Mathf.FloorToInt(current.Elapsed / current.FrameDuration);
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

    public void StopAnimation()
    {
        if (playback != null && !playback.IsDone)
        {
            playback.IsCancelled = true;
            playback.IsDone = true;
        }
        playback = null;
        pendingReaction = false;
    }

    private void OnDisable() => StopAnimation();

    #region 加载图片

    #if UNITY_EDITOR
   
    

    [Header(("加载图片"))]
    
    [SerializeField] private string path = "Assets/A美术/BattleSprites/马赛成年动作";
    [SerializeField] private string pieceName = "PC03A";
    [Button("测试加载图片")]
    private void TestLoadSprite()
    {
        if (!Directory.Exists(path))
        {
            Debug.LogError($"<color=red>【路径不存在】</color> 请检查: {path}");
            return;
        }

        // 获取当前脚本所有公开字段
        FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            // --- 排除特定不需要自动绑定的字段 ---
            if (field.Name == "enabled" || field.Name == "tag" || field.Name == "name") continue;

            // 处理 List<List<Sprite>> (特定命名的技能组)
            if (field.FieldType == typeof(List<List<Sprite>>))
            {
                // 这里为了通用性，判断一下变量名是否包含 skill。你也可以直接针对 skillSpriteList 处理。
                if (field.Name.ToLower().Contains("skill"))
                {
                    LoadSkillSpritesNested(field, path, pieceName);
                }
                continue; // 处理完嵌套列表直接跳过后面逻辑
            }

            // --- 通用命名处理逻辑 (提取后缀) ---
            // "idleSprite" -> "idle", "meleeSprites" -> "melee", "deathSprites" -> "death"
            string suffix = field.Name.ToLower().Replace("sprite", "").Replace("list", "");
            // 移除可能存在的复数 's' (如 meleeSprites -> melee)
            if (suffix.EndsWith("s")) suffix = suffix.Substring(0, suffix.Length - 1);
            
            // 处理 List<Sprite> (序列)
            if (field.FieldType == typeof(List<Sprite>))
            {
                field.SetValue(this, LoadSpriteListSequential(path, pieceName, suffix));
            }
            // 处理 Sprite (单张)
            else if (field.FieldType == typeof(Sprite))
            {
                field.SetValue(this, LoadSingleSprite(path, pieceName, suffix));
            }
        }

        // 核心：标记对象已改变，否则引用不会保存！
        EditorUtility.SetDirty(this);
        // 可选：强制保存未保存的资源（更保险）
        // AssetDatabase.SaveAssets();

        Debug.Log($"<color=green>【自动绑定完成】</color> 已尝试匹配 {pieceName} 的所有资源。");
        Debug.Log($"技能组数量: <color=yellow>{skillSpriteList.Count}</color>");
    }

    // --- 加载逻辑函数 ---

    private Sprite LoadSingleSprite(string folder, string prefix, string suffix)
    {
        // 格式: PC01Aidle.png
        string fullPath = $"{folder}/{prefix}{suffix}.png";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        if (s == null) Debug.LogWarning($"[单图] 未找到: {fullPath}");
        return s;
    }

    private List<Sprite> LoadSpriteListSequential(string folder, string prefix, string suffix)
    {
        // 格式: PC01Amelee1.png, PC01Amelee2.png ...
        List<Sprite> sprites = new List<Sprite>();
        int index = 1;
        
        while (true)
        {
            string fullPath = $"{folder}/{prefix}{suffix}{index}.png";
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            
            if (s == null) break; // 只要断了一次序号（比如有1,2,4），就停止。这是通常的做法。
            
            sprites.Add(s);
            index++;
        }

        if (sprites.Count == 0) Debug.LogWarning($"[序列] 未在路径找到以 '{prefix}{suffix}' 开头的图片帧。");
        return sprites;
    }

    // 针对 PC01AskillX-Y 格式的特定处理
    private void LoadSkillSpritesNested(FieldInfo field, string folder, string prefix)
    {
        // 清空旧数据
        List<List<Sprite>> allSkills = new List<List<Sprite>>();
        
        int skillGroupIndex = 1;

        // 外层循环：遍历技能组 (skill1, skill2, ...)
        while (true)
        {
            List<Sprite> currentSkillFrames = new List<Sprite>();
            int frameIndex = 1;

            // 内层循环：遍历该技能组内的帧 (1-1, 1-2, ...)
            while (true)
            {
                // 格式拼接: PC01Askill + 1 + - + 1 + .png
                string fullPath = $"{folder}/{prefix}skill{skillGroupIndex}-{frameIndex}.png";
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);

                if (s == null)
                {
                    // 这里有一个细节：如果 skill1-1 存在，但 skill1-2 不存在，我们应该跳出内层。
                    // 但此时我们不能确定 skill2-1 是否存在。
                    break; 
                }

                currentSkillFrames.Add(s);
                frameIndex++;
            }

            // 如果这一组搜集到了图片，添加到总列表中
            if (currentSkillFrames.Count > 0)
            {
                allSkills.Add(currentSkillFrames);
                skillGroupIndex++; // 继续尝试寻找下一个技能组
            }
            else
            {
                // 如果 skill{skillGroupIndex}-1 连第一帧都没找到，说明没有更多技能组了，跳出外层
                break;
            }
        }

        if (allSkills.Count == 0) Debug.LogWarning($"[技能组] 未能在 '{folder}' 下找到匹配 '{prefix}skillX-Y.png' 格式的图片。");
        
        // 将反射获取到的列表赋值回脚本变量
        field.SetValue(this, allSkills);
    }
#endif
    #endregion
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
