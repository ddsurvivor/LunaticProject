
[System.Serializable]
    public class AttackPack
    {
        public int damage;
        public DamageType damageType;

        public AttackPack(int damage, DamageType damageType)
        {
            this.damage = damage;
            this.damageType = damageType;
        }
    }
