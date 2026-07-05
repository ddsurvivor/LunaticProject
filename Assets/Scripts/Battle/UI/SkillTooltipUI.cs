using UnityEngine;
using UnityEngine.UI; // 使用旧版 UI 命名空间

public class SkillTooltipUI : MonoBehaviour
{

    [Header("UI 组件引用")]
    //[SerializeField] private GameObject tooltipPanel; // 提示窗口的根节点面板
    [SerializeField] private Text nameText;           // 专门显示技能名字的 Text
    [SerializeField] private Text infoText;           // 显示消耗、范围、描述的 Text

    
    [SerializeField]
    private Image tooltipImage; // 提示窗口的背景图片组件
    /// <summary>
    /// 显示并更新技能提示信息
    /// </summary>
    /// <param name="skill">传入的技能数据包</param>
    public void ShowTooltip(SkillPack skill)
    {
        if (skill == null) return;

        // 1. 设置技能名字
        nameText.text = skill.skillName;

        // 2. 拼接技能详细信息（消耗、范围、描述等）
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"能量消耗: {skill.mpCost}");
        sb.AppendLine($"作用目标: {(skill.target.ToChinese())}");
        sb.Append($"作用范围: {skill.rangeValue}");
        
        // 根据技能范围类型，动态追加特有属性
        if (skill.rangeType == RangeType.Fan)
        {
            sb.Append($" (角度: {skill.rangeAgle}°)");
        }
        else if (skill.rangeType == RangeType.Grenade)
        {
            sb.Append($" (爆炸半径: {skill.explodeRadius})");
        }
        sb.AppendLine(); // 换行
        
        sb.AppendLine("---------------------------");
        sb.AppendLine($"技能描述: {skill.description}");

        // 3. 新增：在描述末尾动态追加技能伤害信息
        if (skill.attackPacks != null && skill.attackPacks.Count > 0)
        {
            sb.AppendLine(); // 与描述隔开一行
            sb.AppendLine("【技能伤害】");

            // 如果只有一段伤害，直接整行输出
            if (skill.attackPacks.Count == 1)
            {
                AttackPack atk = skill.attackPacks[0];
                string critLabel = atk.isCritical ? " (必定暴击)" : "";
                sb.AppendLine($"造成 <b>{atk.damage}</b> 点 {(atk.damageType.ToChinese())}伤害{critLabel}");
            }
            // 如果有多段伤害（比如复合属性或连击），循环输出每段细节
            else
            {
                for (int i = 0; i < skill.attackPacks.Count; i++)
                {
                    AttackPack atk = skill.attackPacks[i];
                    string critLabel = atk.isCritical ? " (必定暴击)" : "";
                    sb.AppendLine($"  • 第 {i + 1} 段: <b>{atk.damage}</b> 点 {(atk.damageType.ToChinese())}伤害{critLabel}");
                }
            }

            // 如果攻击次数大于 1，可以额外提示总连击数
            if (skill.atkTimes > 1)
            {
                sb.AppendLine($"总计攻击次数: {skill.atkTimes} 次");
            }
        }
        
        // 3. 赋值给详细信息 Text
        infoText.text = sb.ToString();

        
        tooltipImage.gameObject.SetActive(false);
        // 4. 显示面板
        this.gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏技能提示信息
    /// </summary>
    public void HideTooltip()
    {
        this.gameObject.SetActive(false);
    }

    public void ShowItemTip(ItemData itemData)
    {
        if (itemData == null) return;

        // 1. 设置物品名字
        nameText.text = itemData.itemName.ToString();

        // 2. 拼接物品详细信息（描述等）
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"物品标签: {itemData.itemTag.ToChinese()}");
        sb.AppendLine($"使用类别: {itemData.useType.ToChinese()}");
        sb.AppendLine($"物品描述: {itemData.itemDescription}");
        
        // 设置icon
        tooltipImage.sprite = itemData.itemIcon;
        tooltipImage.gameObject.SetActive(true);
        
        // 3. 赋值给详细信息 Text
        infoText.text = sb.ToString();

        // 4. 显示面板
        this.gameObject.SetActive(true);
    }
    
}