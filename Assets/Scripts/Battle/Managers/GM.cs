
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
            // 重复 GM 已由基类销毁，不能继续初始化并影响正在使用的存档。
            if (Ins != this) return;
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
                // 捕获本次调试场景的管理器，不在延迟回调中查找届时的新场景单例。
                var manager = FindObjectOfType<BattleManager>();
                if (manager != null) StartCoroutine(DebugBattleStartRoutine(manager, SceneManager.GetActiveScene()));
            }
        }

        private IEnumerator DebugBattleStartRoutine(BattleManager manager, Scene scene)
        {
            yield return new WaitForSecondsRealtime(0.5f);
            if (manager != null && manager.isActiveAndEnabled && !IsTransitioning &&
                manager.gameObject.scene == scene && SceneManager.GetActiveScene() == scene)
                manager.StartBattle();
        }

        /// <summary>统一普通切场景入口，可直接绑定 UnityEvent(string)。</summary>
        public void LoadScene(string sceneName)
        {
            LoadPlayingScene(sceneName);
        }

        public bool IsTransitioning { get; private set; }

        [Header("普通场景过渡")]
        [Min(0f)] [SerializeField] private float sceneFadeDuration = 0.5f;

        public bool CanLoadScene(string sceneName)
        {
            if (IsTransitioning) return false;
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"场景“{sceneName}”不可加载，请检查 Build Settings。", this);
                return false;
            }
            return true;
        }

        public void StartBattle(string sceneName, string storyAfterBattle, int setting = 0)
        {
            if (!CanLoadScene(sceneName)) return;
            if (大地图System.instance != null && 大地图System.instance.剧情 != null)
            {
                大地图System.instance.剧情.EndReadingStory();
                // 战斗入场已有独立动画，阅读器立即关闭，不等待普通关闭淡出。
                大地图System.instance.剧情.HideReaderImmediately();
            }
            battleScene = sceneName;
            endLog = storyAfterBattle;
            battleSetting = setting;
            AM.StopAll();
            PlayingSystem.特殊剧情 = "";
            bool hasIntro = 大地图System.instance != null && 大地图System.instance.battleStartUIPanel != null;
            // 在播放入场动画前锁定，防止延迟期间反复点击创建多个切换请求。
            IsTransitioning = true;
            if (hasIntro) 大地图System.instance.battleStartUIPanel.PlayBattleStartAnimation(1f);
            StartCoroutine(TransitionRoutine(sceneName, true, "", hasIntro ? 0.7f : 0f));
        }

        public void StartBattle(string sceneName)
        {
            StartBattle(sceneName, "", 0);
        }

        public void BattleEnd()
        {
            ReturnFromBattle("Playing", endLog);
        }

        public void ReturnFromBattle(string sceneName, string storyAfterBattle)
        {
            if (!CanLoadScene(sceneName)) return;
            IsTransitioning = true;
            StartCoroutine(TransitionRoutine(sceneName, false, storyAfterBattle));
        }

        public void LoadPlayingScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) sceneName = "Playing";
            if (!CanLoadScene(sceneName)) return;
            endLog = "";
            battleScene = "";
            IsTransitioning = true;
            bool isBattle = sceneName.IndexOf("BATTLE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            sceneName.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
            StartCoroutine(TransitionRoutine(sceneName, isBattle, ""));
        }

        public void ReloadCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (!CanLoadScene(sceneName)) return;
            IsTransitioning = true;
            StartCoroutine(TransitionRoutine(sceneName, FindObjectOfType<BattleManager>() != null, ""));
        }

        private IEnumerator TransitionRoutine(string sceneName, bool showLoading, string storyAfterBattle, float delay = 0f)
        {
            try
            {
                if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
                if (battleTransitionPanel == null)
                    battleTransitionPanel = gameObject.AddComponent<BattleTransitionPanel>();
                // 已有转场 UI 应随 GM 常驻，保留它的 Canvas 层级。
                if (!battleTransitionPanel.transform.IsChildOf(transform))
                {
                    var root = battleTransitionPanel.transform.root.gameObject;
                    DontDestroyOnLoad(root);
                }

                bool loaded = false;
                yield return battleTransitionPanel.RunTransition(sceneName, showLoading, sceneFadeDuration, () =>
                {
                    loaded = true;
                    // 暂停/慢速状态属于上一场景；淡出前恢复新场景的正常时间。
                    Time.timeScale = 1f;
                    if (大地图System.instance != null)
                    {
                        大地图System.instance.InitializeMap();
                    }
                });
                if (loaded)
                {
                    var manager = FindObjectOfType<BattleManager>();
                    if (manager != null) manager.StartBattle();
                    if (大地图System.instance != null)
                        大地图System.instance.StartStoryAfterLoad(storyAfterBattle);
                }
            }
            finally
            {
                IsTransitioning = false;
            }
        }

        public void BackToMainMapFinish()
        {
            if (大地图System.instance != null)
            {
                大地图System.instance.InitializeMap();
                大地图System.instance.StartStoryAfterLoad(endLog);
            }
        }
    }
