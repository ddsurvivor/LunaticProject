using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class 大地图System : SerializedMonoBehaviour
{
    public static 大地图System instance;
    public 剧本System 剧情;
    public GameObject[] 地图;
    public GameObject 当前地图;
    public GameObject 失败Obj;

    public static bool 是可以点击地图事件;

    #region 进入剧情视觉反馈

    public float 点击后放大倍率;
    public float 点击后放大进行时间;
    public float 点击进入剧情编辑器等待时间 = 0.8f;

    #endregion

    public 任务节点[] NodeList; //任务节点列表
    public DaytimeSystem daytimeSystem;

    // 二级地图
    public Dictionary<int, GameObject> SmallMapDict = new Dictionary<int, GameObject>();


    [Header("UI面板")]
    public InventoryPanel inventoryPanel;

    public ChapterPanel chapterPanel;
    
    public TutorialUI tutorial;
    public ItemGetPanel itemGetPanel;
    public MessagePanel messagePanel;
    public void 失败()
    {
        失败Obj.SetActive(true);
    }

    private void OnEnable()
    {
        进入大地图调用特殊剧情();
    }

    public void 进入大地图调用特殊剧情()
    {
        if (GM.Ins.PLAYERPROFILE.isNewGame)
        {
            大地图System.instance.开始剧情("PR1");
        }
    }

    public void 开始剧情(string t)
    {
        if (!是可以点击地图事件)
        {
            return;
        }

        是可以点击地图事件 = false;

        当前地图.transform.DOScale(Vector3.one * 点击后放大倍率, 点击后放大进行时间);
        StartCoroutine(wait());

        IEnumerator wait()
        {
            yield return new WaitForSeconds(点击进入剧情编辑器等待时间);
            是可以点击地图事件 = true;
            剧情.gameObject.SetActive(true);
            剧情.设置新剧本(t);
            剧情.Next();
        }
    }

    private void Awake()
    {
        是可以点击地图事件 = true;
        instance = this;
        Debug.Log("测试中,打开第一张地图");
        // TODO:根据存档打开地图
        打开地图("TEST");
    }

    public void 剧情结束()
    {
        剧情.gameObject.SetActive(false);
        //当前地图.transform.DOScale(Vector3.one, 点击后放大进行时间);
        foreach (var node in NodeList)
        {
            node.UpdateState();
        }
    }

    public void 打开地图(string t)
    {
        foreach (var VARIABLE in 地图)
        {
            if (VARIABLE.name == t)
            {
                当前地图 = VARIABLE;
                当前地图.transform.localScale = Vector3.one;
                VARIABLE.SetActive(true);

                //NodeList = VARIABLE.GetComponentsInChildren<任务节点>(true);

                // if (endLog != "")
                // {
                //     大地图System.instance.开始剧情(endLog);
                //     endLog = "";
                // }
                foreach (var node in NodeList)
                {
                    node.UpdateState();
                }
                GM.Ins.PLAYERPROFILE.currentMap = t;
                // 是否打开小地图
                if (GM.Ins.PLAYERPROFILE.curSmallMapIndex != 0)
                {
                    SmallMapActive(GM.Ins.PLAYERPROFILE.curSmallMapIndex, true);
                }
            }
            else
            {
                VARIABLE.SetActive(false);
            }
        }
    }

    public void RefreshAllNodes()
    {
        foreach (var node in NodeList)
        {
            node.UpdateState();
        }
    }

    /// <summary>
    /// 点击进入二级地图
    /// </summary>
    /// <param name="mapID"></param>
    public void SmallMapActive(int mapID, bool active)
    {
        if (SmallMapDict.ContainsKey(mapID))
        {
            SmallMapDict[mapID].SetActive(active);
        }
    }


    public void OnClickQuit()
    {
        // 加载开始场景
        UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
    }
}