
using System;

/// <summary>
/// 战斗场景中的唯一单例
/// 负责引用所有管理器
/// </summary>

    public class BattleScene: MonoSingleton<BattleScene>
    {
        public BattleManager BM;
        public UIManager UM;

        public void Start()
        {
            BM.Init();
        }
    }
