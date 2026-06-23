using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    //public static TutorialManager Instance { get; private set; }

    [Header("配置")]
    [SerializeField] private TutorialDatabaseSO database;
    [SerializeField] public TutorialUI tutorialPanel;

    private void OnEnable()
    {
        // 组件被启用时（场景加载后）立即检查当前场景
        //CheckAndShowTutorial();
    }

    public bool CheckAndShowTutorial()
    {
        string levelName = SceneManager.GetActiveScene().name;

        // 已看过则跳过
        if (GM.Ins.PLAYERPROFILE.seenTutorials.Contains(levelName))
            return false;

        // 使用接口获取教程数据
        TutorialData data = database.GetTutorial(levelName);
        if (data != null)
        {
            tutorialPanel.Show(data, levelName, true);
            return true;
        }
        return false;
    }

    
}