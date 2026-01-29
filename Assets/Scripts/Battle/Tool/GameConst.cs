public static class GameConst
{
    public const int attackBurstCharge = 20;
    public const int skillBurstCharge = 40;
    public const int hurtBurstCharge = 10;
    public const float burstDamageRate = 1.5f; // 聚能发动时的伤害加成比例
    public const float burstAddDamageRate = 0.3f; // 聚能发动后增加的伤害比例
    public const int enemySkillRate = 35;// 敌人使用技能的概率，百分比
    
    public static bool CheckRate(int rate)
    {
        int roll = UnityEngine.Random.Range(1, 101);
        return roll <= rate;
    }
}