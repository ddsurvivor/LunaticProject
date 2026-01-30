
    using System.Collections;
    using UnityEngine;
using UnityEngine.SceneManagement;
    using Sirenix.OdinInspector;
    public class GM: MonoSingleton<GM>
    {
        // 战斗切换
        [ReadOnly]
        public string battleScene;
        [ReadOnly]
        public string endLog;
        public PLAYERPROFILE PLAYERPROFILE;
        
        
        public void StartBattle(string battleScene, string endLog)
        {
            this.battleScene = battleScene;
            this.endLog = endLog;
            SceneManager.LoadScene(battleScene);
            PlayingSystem.特殊剧情 = "";
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
            大地图System.instance.打开地图("TEST");
            if (endLog != "")
            {
                大地图System.instance.开始剧情(endLog);
            }
        }
    }
