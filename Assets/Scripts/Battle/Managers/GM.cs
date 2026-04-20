
    using System;
    using System.Collections;
    using UnityEngine;
using UnityEngine.SceneManagement;
    using Sirenix.OdinInspector;
    using Sirenix.Serialization;

    public class GM: MonoSingleton<GM>
    {
        // 战斗切换
        [ReadOnly]
        public string battleScene;
        [ReadOnly]
        public string endLog;
        [OdinSerialize]
        public PLAYERPROFILE PLAYERPROFILE;
        
        // 系统
        public DataManager DM;
        public MarketSystem marketSystem;
        
        [LabelText("棋子血量继承")]
        public bool pieceHPInherit = false;

        private void Awake()
        {
            DM.Init();
            // 临时使用全新游戏存档
            PLAYERPROFILE = new PLAYERPROFILE();
            PLAYERPROFILE.新游戏初始化数值();
        }

        // public void StartBattle(string battleScene, string endLog)
        // {
        //     this.battleScene = battleScene;
        //     this.endLog = endLog;
        //     SceneManager.LoadScene(battleScene);
        //     PlayingSystem.特殊剧情 = "";
        // }
        
        public void StartBattle(string battleScene, string endLog, int setting = 0)
        {
            this.battleScene = battleScene;
            this.endLog = endLog;
            StartCoroutine(StartBattleCoroutine(battleScene, 
                ()=>BattleScene.Ins.BM.ApplySetting(setting)));
        }

        private IEnumerator StartBattleCoroutine(string sceneName, Action onComplete = null)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            while (!asyncLoad.isDone)
            {
                // 可选：显示加载进度
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log($"加载进度: {progress * 100}%");
                yield return null;
            }

            // 场景加载完成后执行
            PlayingSystem.特殊剧情 = "";
            onComplete?.Invoke();
        }

        public void StartBattle(string battleScene)
        {
            this.battleScene = battleScene;
            this.endLog = "";
            SceneManager.LoadScene(battleScene);
            PlayingSystem.特殊剧情 = "";
        }
        public  void BattleEnd()
        {
            StartCoroutine(LoadSceneCoroutine());
        }

        public void LoadPlayingScene()
        {
            endLog = "";
            battleScene = "";
            StartCoroutine(LoadSceneCoroutine());
        }
        
        
        IEnumerator LoadSceneCoroutine()
        {
            // 异步加载场景
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Playing");
        
            // 等待场景加载完成
            while (!asyncLoad.isDone)
            {
                // 可以在这里显示加载进度
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                Debug.Log($"加载进度: {progress * 100}%");
                yield return null;
            }
        
            // 场景加载完成后执行
            大地图System.instance.打开地图(PLAYERPROFILE.currentMap);
            if (endLog != "")
            {
                大地图System.instance.开始剧情(endLog);
            }
        }
    }
