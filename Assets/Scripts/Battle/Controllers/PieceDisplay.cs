using System;using System.Collections;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
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

    private UnityAction finishAction;


    private float frameDuration = 1 / 6f; // 12帧 //0.2f; // 每帧持续时间，默认为0.2秒
    /// <summary>
    /// 更改显示状态脚本，传入一个状态和一个持续时间。
    /// 如果是-1则表示永久更改（或等待动画播放完毕），否则持续时间结束后恢复到idle状态。
    /// </summary>
    public void ChangeDisplayState(PieceDisplayState state, bool back = false, float duration = -1f, UnityAction finish = null, int index = 0)
    {
        if (pieceSpriteRenderer == null) return;
        finishAction = finish;
        StopAllCoroutines();

        switch (state)
        {
            case PieceDisplayState.Idle:
                StartCoroutine(PlaySpriteAnimation(idleSprite, frameDuration, true));
                break;
            case PieceDisplayState.Move:
                // 移动通常也是循环播放
                StartCoroutine(PlaySpriteAnimation(moveSprite, frameDuration/2f, true));
                break;
            case PieceDisplayState.Attack:
                StartCoroutine(PlaySpriteAnimation(meleeSprites, frameDuration));
                break;
            case PieceDisplayState.Shoot:
                StartCoroutine(PlaySpriteAnimation(rangeSprites, frameDuration));
                break;
            case PieceDisplayState.Dodge:
                StartCoroutine(PlaySpriteAnimation(dodgeSprite, frameDuration));
                break;
            case PieceDisplayState.Hit:
                StartCoroutine(PlaySpriteAnimation(hitSprite, frameDuration));
                break;
            case PieceDisplayState.Death:
                StartCoroutine(PlaySpriteAnimation(deathSprites, frameDuration));
                break;
            case PieceDisplayState.Skill:
                if (index >= 0 && index < skillSpriteList.Count)
                {
                    StartCoroutine(PlaySpriteAnimation(skillSpriteList[index], frameDuration));
                }
                else
                {
                    Debug.LogWarning($"[PieceDisplay] 技能索引 {index} 超出范围。");
                    finishAction?.Invoke();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        if (duration > 0)
        {
            StartCoroutine(RevertToIdleAfterDelay(duration));
        }
    }
    
    private IEnumerator RevertToIdleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        //pieceSpriteRenderer.sprite = idleSprite;
        // 进入idle动画状态
        StartCoroutine(PlaySpriteAnimation(idleSprite, frameDuration,true));
        finishAction?.Invoke();
    }
    
    public void Dead()
    {
        // 死亡后隐藏棋子
        pieceSpriteRenderer.DOFade(0f, 0.5f);
    }

    private IEnumerator PlaySpriteAnimation(List<Sprite> sprites, float frameDuration)
    {
        if (sprites == null || sprites.Count == 0)
        {
            finishAction?.Invoke();
            yield break;
        }
        foreach (var sprite in sprites)
        {
            pieceSpriteRenderer.sprite = sprite;
            yield return new WaitForSeconds(frameDuration);
        }
        finishAction?.Invoke();
    }
    private IEnumerator PlaySpriteAnimation(List<Sprite> sprites, float frameDuration, bool loop = false)
    {
        // 1. 防御性检查：没图片直接闪人，安全第一
        if (sprites == null || sprites.Count == 0)
        {
            finishAction?.Invoke();
            yield break;
        }

        int currentFrame = 0;

        while (true)
        {
            // 2. 渲染当前帧
            pieceSpriteRenderer.sprite = sprites[currentFrame];
            yield return new WaitForSeconds(frameDuration);

            // 3. 准备切到下一帧
            currentFrame++;

            // 4. 当一轮动画播放完毕时的核心逻辑判定
            if (currentFrame >= sprites.Count)
            {
                if (loop)
                {
                    currentFrame = 0; // 如果是循环，索引归零，继续跑 while
                }
                else
                {
                    break; // 如果不循环，直接跳出整个 while
                }
            }
        }

        // 5. 只有跳出了 while 循环（即 loop = false 且播完）才会走到这里
        finishAction?.Invoke();
    }
    
    public void PlayFrame(List<Sprite> sprites, UnityAction finish = null)
    {
        finishAction = finish;
        StartCoroutine(PlaySpriteAnimation(sprites, frameDuration));
    }
    
    public void FaceRight(bool faceRight)
    {
        Vector3 scale = pieceSpriteRenderer.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1);
        pieceSpriteRenderer.transform.localScale = scale;
    }
    
    /// <summary>
    /// 停止当前播放的序列帧动画，图片保持在当前帧不动
    /// </summary>
    public void StopAnimation()
    {
        StopAllCoroutines();
        // 如果需要，这里也可以选择性地触发 finishAction?.Invoke(); 
        // 视你的底层逻辑（比如是否有连招、动作锁）而定
    }

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
