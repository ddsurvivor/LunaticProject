using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum ComponentType { [LabelText("普通插件")]Normal, [LabelText("武器插件")]Weapon }

public enum ComponentEffect
{
    // 属性加成
    [LabelText("属性加成")]
    AttrBonus = 0,
    // 被动效果
    [LabelText("被动效果")]
    PassiveEffect = 100,
    // 主动技能
    [LabelText("主动技能")]
    ActiveSkill = 200
}

[System.Serializable]
public class ComponentData
{
    public int id; 
    public string itemName;
    public ComponentType type;
    public Sprite icon;
    [TextArea] public string description;
    
    public ComponentEffect effectType;
    [ShowIf("@this.effectType == ComponentEffect.AttrBonus")][LabelText("属性编号")]
    public int attrId;
    [ShowIf("@this.effectType == ComponentEffect.AttrBonus")][LabelText("属性加成值")]
    public int attrValue;
    public List<ComponentAttributeBonus> battleBonuses = new();
    [Min(0)] public int extraSkillMana;
    [ShowIf("@this.effectType == ComponentEffect.PassiveEffect")][LabelText("被动技能id")]
    public PassiveType passiveType; // 被动效果ID
    [ShowIf("@this.effectType == ComponentEffect.ActiveSkill")][LabelText("主动技能")]
    public SkillPack skillPack; // 主动技能数据包
}

[CreateAssetMenu(fileName = "ComponentConfig", menuName = "Game/ComponentConfig")]
public class ComponentConfig : ScriptableObject
{
    
    public List<ComponentData> componentList = new List<ComponentData>();

    public ComponentData GetData(int id)
    {
        // 如果 ID 为 0 或未找到，返回 null
        if (id <= 0) return null;
        return componentList?.Find(c => c != null && c.id == id);
    }

    public List<string> ValidateConfiguration()
    {
        var issues = new List<string>();
        var ids = new HashSet<int>();
        if (componentList == null) return issues;
        foreach (var data in componentList)
        {
            if (data == null) { issues.Add("插件配置包含空条目"); continue; }
            if (data.id <= 0 || !ids.Add(data.id)) issues.Add($"插件 {data.itemName} 的编号无效或重复：{data.id}");
            if (data.type != ComponentType.Normal && data.type != ComponentType.Weapon)
                issues.Add($"插件 {data.id} 的槽位类型无效");
            if (data.effectType == ComponentEffect.AttrBonus && (data.attrId < 0 || data.attrId > 4))
                issues.Add($"插件 {data.id} 的角色属性编号应为0至4");
            if (data.effectType == ComponentEffect.ActiveSkill &&
                (data.skillPack == null || string.IsNullOrWhiteSpace(data.skillPack.skillName)))
                issues.Add($"插件 {data.id} 缺少主动技能");
            if (data.effectType == ComponentEffect.PassiveEffect &&
                (data.passiveType == PassiveType.None || !System.Enum.IsDefined(typeof(PassiveType), data.passiveType)))
                issues.Add($"插件 {data.id} 缺少有效被动效果");
            if (data.battleBonuses != null)
                foreach (var bonus in data.battleBonuses)
                    if (bonus == null || bonus.attribute == BuffAttrType.None ||
                        !System.Enum.IsDefined(typeof(BuffAttrType), bonus.attribute))
                        issues.Add($"插件 {data.id} 的战斗属性加成无效");
        }
        return issues;
    }

    private void OnValidate()
    {
        foreach (string issue in ValidateConfiguration()) Debug.LogWarning(issue);
    }
}

[System.Serializable]
public class ComponentAttributeBonus
{
    public BuffAttrType attribute;
    public float value;
}

public static class ComponentEquipment
{
    public static IEnumerable<ComponentData> Equipped(Player player)
    {
        var config = GM.Ins?.DM?.componentConfig;
        if (player == null || config == null) yield break;
        var seen = new HashSet<int>();
        foreach (var slots in new[] { player.normalSlots, player.weaponSlots })
        {
            if (slots == null) continue;
            foreach (int id in slots)
            {
                var data = config.GetData(id);
                if (data == null || (data.type != ComponentType.Normal && data.type != ComponentType.Weapon) ||
                    (slots == player.normalSlots) != (data.type == ComponentType.Normal) || !seen.Add(id)) continue;
                yield return data;
            }
        }
    }

    public static int AttributeBonus(Player player, int attribute)
    {
        int result = 0;
        foreach (var data in Equipped(player))
            if (data.effectType == ComponentEffect.AttrBonus && data.attrId == attribute)
                result += data.attrValue;
        return result;
    }

    public static int ManaCost(Player player, SkillPack skill)
    {
        if (skill == null || skill.mpCost < 0) return -1;
        long cost = skill.mpCost;
        foreach (var data in Equipped(player)) cost += Mathf.Max(0, data.extraSkillMana);
        return cost > int.MaxValue ? -1 : (int)cost;
    }

    public static void RefreshBattle(Player player)
    {
        var battle = BattleScene.Ins?.BM;
        if (battle == null) return;
        var seen = new HashSet<PieceController>();
        foreach (var controller in new[] { battle.PlayerController, battle.AIController })
        {
            if (controller == null) continue;
            foreach (var piece in controller.pieces)
                if (piece != null && piece.playerData == player && seen.Add(piece)) piece.RefreshComponents();
        }
    }

    public static void OnAttackResolved(PieceController attacker, List<PieceController> hits,
        SkillPack skill, ActionType action, Vector3 targetPosition)
    {
        if (attacker == null || attacker.isDead || hits.Count == 0) return;
        var battle = BattleScene.Ins.BM;
        if (action == ActionType.近战攻击)
        {
            if (attacker.availablePassives.Contains(PassiveType.Lash)) attacker.unitAttrCenter.AddMana(3);
            if (attacker.availablePassives.Contains(PassiveType.Implosion))
            {
                var explosion = GM.Ins.DM.skillPackListSO.GetSkillPack("内爆");
                if (explosion != null)
                {
                    var targets = battle.orderManager.IsInsideSector(attacker.transform.position,
                        hits[0].transform.position - attacker.transform.position, explosion.rangeValue, explosion.rangeAgle);
                    targets.RemoveAll(t => t == null || t.isPlayerPiece == attacker.isPlayerPiece);
                    battle.PieceSkill(attacker, targets, explosion, hits[0].transform.position);
                }
            }
        }
        if (action != ActionType.技能 || !attacker.availablePassives.Contains(PassiveType.SpiderSilk)) return;
        var affected = new HashSet<PieceController>(hits);
        var candidates = new List<PieceController>();
        var enemies = attacker.isPlayerPiece ? battle.AIController.pieces : battle.PlayerController.pieces;
        foreach (var enemy in enemies)
            if (enemy != null && !enemy.isDead && enemy.gameObject.activeInHierarchy &&
                enemy.isPlayerPiece != attacker.isPlayerPiece && BuffManager.CanTarget(attacker, enemy) &&
                !affected.Contains(enemy) && !candidates.Contains(enemy)) candidates.Add(enemy);
        for (int i = 0; i < 2 && candidates.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            affected.Add(candidates[index]); candidates.RemoveAt(index);
        }
        foreach (var target in affected)
        {
            if (target == null || target.isDead) continue;
            battle.buffManager.AddBuff(target.unitAttrCenter, BuffType.Stun, 2);
            target.unitAttrCenter.TakeDamage(new AttackPack(15, DamageType.Electric));
            battle.characterSkillManager.NotifyTakeDamage(target.gameObject, attacker.gameObject);
            if (target.isDead) battle.characterSkillManager.NotifyKillEnemy(attacker.gameObject, target.gameObject);
        }
    }

    // 仅在成功支付费用并确认施法后调用；延迟技能落地不再次返还。
    public static void OnSkillUsed(PieceController caster, SkillPack skill)
    {
        if (caster == null || caster.isDead || skill == null ||
            !caster.availablePassives.Contains(PassiveType.LongTermInterests)) return;
        if (UnityEngine.Random.Range(0, 100) < 35)
            caster.unitAttrCenter.AddMana(caster.unitAttrCenter.GetSkillManaCost(skill));
    }
}
