/// <summary>
/// 复用的伤害配置、传输方法
/// </summary>
[System.Serializable]
public class AttackPack
{
    public int damage;
    public DamageType damageType;
    public bool isCritical;

    public AttackPack(int damage, DamageType damageType, bool isCritical = false)
    {
        this.damage = damage;
        this.damageType = damageType;
        this.isCritical = isCritical;
    }
}