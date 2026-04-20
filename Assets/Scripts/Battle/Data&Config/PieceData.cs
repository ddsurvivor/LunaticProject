using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEngine;
using System;
using Sirenix.OdinInspector;

[Serializable]
/// <summary>
/// 棋子数据
/// </summary>
public class PieceData
{
    [LabelText("棋子编号")]public int pieceId;
    [LabelText("棋子名称")]public string pieceName;
    [LabelText("生命值")]public int maxHealth;
    [LabelText("行动力")]public int maxMovePoint;
    [LabelText("移动范围")]public float moveRange;
    [LabelText("近战攻击")]public SkillPack meleeAtk;
    [LabelText("远程攻击")]public SkillPack rangedAtk;
    [LabelText("弹药数量")]public int maxAmmoCount;
    [LabelText("闪避率")] public int evasionRate;
    [LabelText("暴击率")] public int critRate;
    [LabelText("暴击伤害倍率")] public int critDamageRate;
    [OdinSerialize]
    [LabelText("棋子音效")]public Dictionary<ActionType, AudioClip> actionSounds = new();
    [LabelText("技能列表")]public List<SkillPack> skillPacks = new();
    [OdinSerialize]
    [LabelText("护甲值")]public Dictionary<DamageType, int> _armorDic = new();// 护甲值
    [LabelText("元素属性")]public PieceElementType elementType;
    [OdinSerialize]
    [LabelText("克制伤害")]public Dictionary<PieceElementType, int> _elementAddDamage = new();// 元素克制加成伤害
    [LabelText("初始能量值")] public int initialMana;
    [LabelText("最大能量值")] public int maxMana;
    [LabelText("掉落道具")] public List<ItemPack> dropItemList = new();
    [LabelText("掉落概率")]  public int dropRate;
}