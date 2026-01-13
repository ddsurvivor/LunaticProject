
    using UnityEngine;
using UnityEngine.SceneManagement;
    public class GM: MonoSingleton<GM>
    {
        // 战斗切换
        public string battleScene;
        public string endLog;
        
        
        public void StartBattle(string battleScene, string endLog)
        {
            this.battleScene = battleScene;
            this.endLog = endLog;
            SceneManager.LoadScene(battleScene);
            PlayingSystem.特殊剧情 = "";
        }
        public  void BattleEnd()
        {
            SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) =>
            {
                大地图System.instance.打开地图("TEST");
                if (endLog != "")
                {
                    大地图System.instance.开始剧情(endLog);
                }
            };
            SceneManager.LoadScene("Playing");
        }
    }
