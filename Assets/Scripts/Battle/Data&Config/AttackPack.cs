
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
