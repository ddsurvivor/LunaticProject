using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public string startLevelName;

    public SavePanel savePanel;

    public UIPanel settingPanel;

    [Header("开始菜单 CG")]
    [Tooltip("未通关时显示的第一张 CG 图片物体")]
    [SerializeField] private GameObject normalCG;
    [Tooltip("本地记录已触发 GAMEFINISH 后显示的第二张 CG 图片物体")]
    [SerializeField] private GameObject finishedCG;

    private void OnEnable()
    {
        RefreshCG();
    }

    public void RefreshCG()
    {
        bool finished = LocalGameProgress.HasFinishedGame;
        if (normalCG != null) normalCG.SetActive(!finished);
        if (finishedCG != null) finishedCG.SetActive(finished);
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

    [Button("清除通关标记")]
    private void ClearMark()
    {
        PlayerPrefs.DeleteKey("LunaticProject.GameFinished");
        PlayerPrefs.Save();
    }
}
