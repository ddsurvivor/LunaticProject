using UnityEngine;

public static class GameConst
{
    /// <summary>AVG 与战斗 UI 共用的 sRGB 色板；白色素材通过 Image.color 或材质染色，透明度由使用方控制。</summary>
    public static class UIColors
    {
        public static readonly Color32 Background = new Color32(17, 24, 30, 255); // #11181E，对话及存档底板。
        public static readonly Color32 Surface = new Color32(36, 48, 66, 255); // #243042，设置侧栏和标题栏。
        public static readonly Color32 SurfaceInset = new Color32(32, 44, 61, 255); // #202C3D，对话框内层。
        public static readonly Color32 Border = new Color32(165, 145, 119, 255); // #A59177，对话及存档外框。
        public static readonly Color32 Accent = new Color32(231, 165, 51, 255); // #E7A533，选项与设置选中态。
        public static readonly Color32 Text = new Color32(238, 230, 207, 255); // #EEE6CF，对话操作文字。
        public static readonly Color32 TextHeading = new Color32(229, 224, 207, 255); // #E5E0CF，设置标题。
        public static readonly Color32 TextMuted = new Color32(163, 171, 175, 255); // #A3ABAF，设置分类计数。
        public static readonly Color32 Divider = new Color32(104, 122, 133, 255); // #687A85，设置侧栏分隔线。
        public static readonly Color32 Disabled = new Color32(87, 103, 116, 255); // #576774，设置未选中控件。
        public static readonly Color32 Ally = new Color32(74, 195, 213, 255); // #4AC3D5，战斗扩展色：我方、移动范围。
        public static readonly Color32 Enemy = new Color32(234, 105, 99, 255); // #EA6963，战斗扩展色：敌方、危险范围。
        public static readonly Color32 Positive = new Color32(125, 197, 158, 255); // #7DC59E，战斗扩展色：增益、治疗。
        public static readonly Color32 Health = new Color32(125, 197, 158, 255); // #7DC59E，血量；阵营另用色条和图标标识。
        public static readonly Color32 DamagePreview = new Color32(234, 105, 99, 255); // #EA6963，预计损血段。
        public static readonly Color32 SkillRange = new Color32(231, 165, 51, 255); // #E7A533，技能范围与当前瞄准。
    }

    public const int attackBurstCharge = 10;
    //public const int skillBurstCharge = 15;
    public const int hurtBurstCharge = 5;
    public const float burstDamageRate = 1.2f; // 聚能发动时的伤害加成比例
    public const float burstAddDamageRate = 0.2f; // 聚能发动后增加的伤害比例
    public const int enemySkillRate = 35;// 敌人使用技能的概率，百分比
    public const int initialCoins = 3000; // 初始金币数量
    
    public static readonly Quaternion spriteRotation = Quaternion.Euler(45,  -45, 0f);
    
    public static bool CheckRate(int rate)
    {
        int roll = UnityEngine.Random.Range(1, 101);
        return roll <= rate;
    }
}
