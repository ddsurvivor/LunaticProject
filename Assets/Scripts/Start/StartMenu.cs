using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public string startLevelName;

    public SavePanel savePanel;

    public UIPanel settingPanel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickStart()
    {
        // 返回标题后再开始游戏也必须创建全新档案，不能沿用上一次读档的数据。
        GM.Ins.PLAYERPROFILE = new PLAYERPROFILE();
        GM.Ins.PLAYERPROFILE.新游戏初始化数值();
        GM.Ins.LoadPlayingScene(startLevelName);
    }

    public void OnClickSet()
    {
        settingPanel.Open();
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    
    public void OnClickContinue()
    {
        savePanel.gameObject.SetActive(true);
    }
}
