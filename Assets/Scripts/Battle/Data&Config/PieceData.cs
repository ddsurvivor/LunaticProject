using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
using System;
using Sirenix.OdinInspector;
using SkillSystem;

[Serializable]
/// <summary>
/// 棋子数据
/// </summary>
public class PieceData
{
    [LabelText("模因身体类型")] public MemeBodyType memeBodyType;
    [LabelText("模因目标标签")] public List<string> memeTargetTags = new();
    [LabelText("固有模因编号")] public List<int> innateMemeIds = new();
    [FoldoutGroup("敌人等级成长"), OdinSerialize]
    [LabelText("每级成长"), DictionaryDrawerSettings(KeyLabel = "成长属性", ValueLabel = "每级增加")]
    public Dictionary<EnemyGrowthAttribute, float> levelGrowth = new();
    [OdinSerialize, LabelText("分类型基础攻击力")]
    public Dictionary<DamageType, int> attackDic = new();
    [Min(1), LabelText("回合速度")] public float speed = 10f; // 每次速度时钟推进时累加的行动条进度。
    [LabelText("行动条头像")] public Sprite turnPortrait; // 可留空，由速度条回退到立绘或棋子精灵。
    [LabelText("棋子编号")]public int pieceId;
    [LabelText("棋子名称")]public string pieceName;
    [LabelText("生命值")]public int maxHealth;
    [LabelText("行动力")]public int maxMovePoint;
    [LabelText("移动范围")]public float moveRange;
    [LabelText("技能库")] public SkillPackListSO skillLibrary;
    [LabelText("近战技能"), ValueDropdown(nameof(SkillChoices))] public string meleeSkillId;
    [LabelText("远程技能"), ValueDropdown(nameof(SkillChoices))] public string rangedSkillId;
    [LabelText("初始主动技能"), ValueDropdown(nameof(SkillChoices))] public List<string> initialSkillIds = new();
    private IEnumerable<ValueDropdownItem<string>> SkillChoices()
    {
        if (skillLibrary == null || skillLibrary.skillPacks == null) yield break;
        foreach (var skill in skillLibrary.skillPacks)
            if (skill != null && !string.IsNullOrEmpty(skill.skillId))
                yield return new ValueDropdownItem<string>($"{skill.skillName} [{skill.skillId}]", skill.skillId);
    }
    [HideInInspector] public SkillPack meleeAtk;
    [HideInInspector] public SkillPack rangedAtk;
    /// <summary>从技能库解析近战攻击；未迁移的历史资源回退到旧字段。</summary>
    public SkillPack MeleeAttack => skillLibrary != null ? skillLibrary.GetById(meleeSkillId) : meleeAtk;
    /// <summary>从技能库解析远程攻击；未迁移的历史资源回退到旧字段。</summary>
    public SkillPack RangedAttack => skillLibrary != null ? skillLibrary.GetById(rangedSkillId) : rangedAtk;
    [LabelText("弹药数量")]public int maxAmmoCount;
    [LabelText("闪避率")] public int evasionRate;
    [LabelText("暴击率")] public int critRate;
    [LabelText("暴击伤害倍率")] public int critDamageRate;
    [OdinSerialize]
    [LabelText("棋子音效")]public Dictionary<ActionType, AudioClip> actionSounds = new();
    [HideInInspector] public List<SkillPack> skillPacks = new();
    /// <summary>解析角色初始主动技能；玩家实际技能配置由存档状态决定。</summary>
    public List<SkillPack> DefaultSkills => skillLibrary != null
        ? (initialSkillIds ?? new List<string>()).ConvertAll(id => skillLibrary.GetById(id)).FindAll(s => s != null)
        : new List<SkillPack>(skillPacks ?? new List<SkillPack>());

    /// <summary>将旧内嵌技能迁入统一技能库；迁移后仅保存 ID，重复执行不会覆盖已有配置。</summary>
    public void MigrateSkills(SkillPackListSO library)
    {
        if (library == null) throw new ArgumentNullException(nameof(library));
        skillLibrary = library;
        if (meleeAtk != null) meleeSkillId = library.Register(meleeAtk);
        if (rangedAtk != null) rangedSkillId = library.Register(rangedAtk);
        initialSkillIds ??= new List<string>();
        if (skillPacks != null) foreach (var skill in skillPacks)
        {
            string id = library.Register(skill);
            if (!string.IsNullOrEmpty(id) && !initialSkillIds.Contains(id)) initialSkillIds.Add(id);
        }
        meleeAtk = rangedAtk = null;
        skillPacks = null;
    }
    [OdinSerialize]
    [LabelText("护甲值")]public Dictionary<DamageType, int> _armorDic = new();// 护甲值
    [LabelText("元素属性")]public PieceElementType elementType;
    [OdinSerialize]
    [LabelText("克制伤害")]public Dictionary<PieceElementType, int> _elementAddDamage = new();// 元素克制加成伤害
    [LabelText("初始能量值")] public int initialMana;
    [LabelText("最大能量值")] public int maxMana;
    [LabelText("掉落道具")] public List<ItemPack> dropItemList = new();
    [LabelText("掉落概率")]  public int dropRate;
    [LabelText("扩展属性")] public Dictionary<UnitAttrType, int> attrDic = new Dictionary<UnitAttrType, int>();// 其他属性
    //[LabelText("专长技能")] public List<PassiveSkillPack> passiveSkillPacks = new List<PassiveSkillPack>();
    [LabelText("专长类型")] public List<PassiveSkillType> passiveSkillTypes = new List<PassiveSkillType>();
    [LabelText("武器icon")] public Sprite weaponIcon;
    [LabelText("指令")] public List<OrderProfile> orderProfiles;
}

[SerializeField]
public class PassiveSkillPack
{
    public string skillName;
    public string description;
}
