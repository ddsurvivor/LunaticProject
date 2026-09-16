[System.Serializable]
    public class BuffPack
    {
        public BuffType buffType;
        public int stacks;
        public int rate;// 触发概率百分比
        public SkillTarget target;// 作用目标
        public int barrierHealth; // 阻速场耐久，0使用BuffManager默认值
        public int retaliationTurns; // 认知反噬回合数，0使用默认值
    }
