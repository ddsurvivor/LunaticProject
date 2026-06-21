
    using System;
    using System.Collections;
    using DG.Tweening;
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
        [ReadOnly]
        public int battleSetting = 0;// 战斗设置，默认为0，特殊战斗会有不同的设置
        [OdinSerialize]
        public PLAYERPROFILE PLAYERPROFILE;
        
        // 系统
        public DataManager DM;
        public AudioManager AM;
        public MarketSystem marketSystem;
        
        [LabelText("棋子血量继承")]
        public bool pieceHPInherit = false;
        
        [LabelText("战斗转场面板")]
        public BattleTransitionPanel battleTransitionPanel;

        protected  void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this);
            DM.Init();
            AM.初始化();
            // 临时使用全新游戏存档
            PLAYERPROFILE = new PLAYERPROFILE();
            PLAYERPROFILE.新游戏初始化数值();
            DebugBattleScene();
        }

        private void DebugBattleScene()
        {
            // 仅供测试用
            // 如果当前场景名称包含battle，则调用战斗
            if (SceneManager.GetActiveScene().name.Contains("BATTLE") || SceneManager.GetActiveScene().name.Contains("Boss"))
            {
                DOVirtual.DelayedCall(0.5f, () => { BattleScene.Ins.BM.StartBattle(); });
            }
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
            this.battleSetting = setting;
            大地图System.instance.battleStartUIPanel.PlayBattleStartAnimation(1f);
            DOVirtual.DelayedCall(0.7f, () =>
            {
                battleTransitionPanel.TransitionToBattle(battleScene);
            });
            // 场景加载完成后执行
            PlayingSystem.特殊剧情 = "";
            //StartCoroutine(StartBattleCoroutine(battleScene));
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
            /*DOVirtual.DelayedCall(0.7f, () =>
            {
                battleTransitionPanel.TransitionEndBattle("Playing", endLog);
            });*/
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
            // 执行黑屏加载动画
            大地图System.instance.BlackSceneChapter(endLog);
            大地图System.instance.打开地图(PLAYERPROFILE.currentMap);
        }
        public void BackToMainMapFinish()
        {
            //大地图System.instance.blackFront.SetActive(true);
            大地图System.instance.BlackSceneChapter(endLog);
            大地图System.instance.打开地图(PLAYERPROFILE.currentMap);
        }
    }
