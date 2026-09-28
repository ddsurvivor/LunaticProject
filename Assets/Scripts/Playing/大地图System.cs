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
    [Tooltip("新游戏的初始地图名称；留空使用场景配置的当前地图")]
    [SerializeField] private string initialMapName = "";
    private bool mapInitialized;
    private DaytimeSystem defaultDaytimeSystem;
    public CanvasGroup gameoverPanel;
    public GameObject mapRoot;

    [LabelText("调试模式显示所有关卡")]
    public bool isDebugMode;

    [LabelText("第一章剧情节点ID")]
    [SerializeField]
    private string firstNode = "PR0";

    public static bool 是可以点击地图事件;

    #region 进入剧情视觉反馈

    public float 点击后放大倍率;
    public float 点击后放大进行时间;
    public float 点击进入剧情编辑器等待时间 = 0.8f;

    #endregion

    public 任务节点[] NodeList; //任务节点列表
    [SerializeField]
    private DaytimeSystem daytimeSystem;

    // 二级地图
    public Dictionary<int, GameObject> SmallMapDict = new Dictionary<int, GameObject>();


    [Header("UI面板")]
    public InventoryPanel inventoryPanel;

    public ChapterPanel chapterPanel;
    
    public TutorialUI tutorial;
    public ItemGetPanel itemGetPanel;
    public MessagePanel messagePanel;
    public BattleStartUIPanel battleStartUIPanel;
    public NotificationManager notificationManager;
    public EndingCreditsPlayer endingCreditsPlayer;

    public GameObject blackFront;//黑幕
    public void 失败()
    {
        gameoverPanel.gameObject.SetActive(true);
        gameoverPanel.alpha = 0f;
        gameoverPanel.DOFade(1f, 1f);
    }

    private void OnEnable()
    {
        
    }

    public void StartFirstNode()
    {
        if (GM.Ins.PLAYERPROFILE.isNewGame)
        {
            Debug.Log("检测到新游戏,开始第一章剧情");
            大地图System.instance.开始剧情(firstNode);
            GM.Ins.PLAYERPROFILE.isNewGame = false;
            blackFront.SetActive(true);
            DOVirtual.DelayedCall(5.2f,()=>{
                blackFront.SetActive(false);
            });
        }
    }
    

    public void 开始剧情(string t)
    {
        if (!是可以点击地图事件)
        {
            return;
        }

        是可以点击地图事件 = false;

        当前地图.transform.DOScale(Vector3.one * 点击后放大倍率, 点击后放大进行时间).OnComplete(() =>
        {
            当前地图.transform.localScale = Vector3.one;
        });
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
        defaultDaytimeSystem = daytimeSystem;
    }

    private void Start()
    {
        InitializeMap();
        if (mapInitialized && !isDebugMode)
            StartFirstNode();
    }

    /// <summary>等待 GM 数据准备完毕后恢复地图；场景加载回调和 Start 均可安全调用。</summary>
    public void InitializeMap()
    {
        if (mapInitialized || GM.Ins == null || GM.Ins.PLAYERPROFILE == null) return;
        if (地图 == null || 地图.Length == 0)
        {
            Debug.LogError("大地图未配置可用地图。", this);
            return;
        }
        var profile = GM.Ins.PLAYERPROFILE;
        GameObject target = profile.isNewGame ? null : FindMap(profile.currentMap);
        if (target == null)
        {
            if (!profile.isNewGame)
                Debug.LogWarning($"存档地图“{profile.currentMap}”不存在，使用场景初始地图。", this);
            target = FindMap(initialMapName);
            if (target == null && 当前地图 != null && Array.IndexOf(地图, 当前地图) >= 0)
                target = 当前地图;
            if (target == null)
                target = Array.Find(地图, map => map != null && map.activeSelf);
            if (target == null)
                target = Array.Find(地图, map => map != null);
            profile.curSmallMapIndex = 0;
        }
        if (target == null)
        {
            Debug.LogError("大地图未配置可用地图。", this);
            return;
        }

        打开地图(target.name);
        mapInitialized = true;
    }

    private GameObject FindMap(string mapName)
    {
        return 地图 == null ? null : Array.Find(地图, map => map != null && map.name == mapName);
    }

    /// <summary>写入存档前同步实际选中的地图；战斗场景没有大地图时保留已有记录。</summary>
    public void CaptureMapState()
    {
        if (mapInitialized && 当前地图 != null)
            GM.Ins.PLAYERPROFILE.currentMap = 当前地图.name;
    }

    public void 剧情结束()
    {
        剧情.gameObject.SetActive(false);
        GM.Ins.AM.StopAll();
        //当前地图.transform.DOScale(Vector3.one, 点击后放大进行时间);
        foreach (var node in NodeList)
        {
            node.UpdateState();
        }
    }

    public void 打开地图(string t)
    {
        GameObject target = FindMap(t);
        if (target == null)
        {
            Debug.LogWarning($"找不到地图“{t}”，保留当前地图。", this);
            return;
        }

        var profile = GM.Ins.PLAYERPROFILE;
        if (profile.currentMap != t)
            profile.curSmallMapIndex = 0;
        当前地图 = target;
        profile.currentMap = t;
        当前地图.transform.localScale = Vector3.one;
        foreach (var map in 地图)
        {
            if (map != null) map.SetActive(map == target);
        }

        // 先关闭上次残留的二级地图，再恢复存档中选中的一张。
        foreach (var pair in SmallMapDict ?? new Dictionary<int, GameObject>())
        {
            if (pair.Value != null)
                pair.Value.SetActive(pair.Key == profile.curSmallMapIndex && pair.Key != 0);
        }

        // 地图切换后重新取时间系统，避免沿用上一张地图的组件。
        daytimeSystem = 当前地图.GetComponent<DaytimeSystem>() ?? defaultDaytimeSystem;
        daytimeSystem?.UpdateDaytimeImage();
        if (!isDebugMode)
            UpdateMission();
        else
            NodeList = mapRoot.GetComponentsInChildren<任务节点>(true);
    }

    private void UpdateMission()
    {
        NodeList = mapRoot.GetComponentsInChildren<任务节点>(true);
        foreach (var node in NodeList)
        {
            node.UpdateState();
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
            var profile = GM.Ins.PLAYERPROFILE;
            if (active)
                profile.curSmallMapIndex = mapID;
            else if (profile.curSmallMapIndex == mapID)
                profile.curSmallMapIndex = 0;
        }
    }


    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void OnClickQuit()
    {
        // 加载开始场景
        GM.Ins.LoadScene("StartScene");
    }

    public void BlackSceneChapter(string endLog)
    {
        if (endLog != "")
        {
            blackFront.SetActive(true);
            大地图System.instance.开始剧情(endLog);
            DOVirtual.DelayedCall(5.2f,()=>{
                blackFront.SetActive(false);
            });
        }
    }
    
    public DaytimeSystem GetDaytimeSystem()
    {
        if (daytimeSystem == null)
            daytimeSystem = 当前地图.GetComponent<DaytimeSystem>() ?? defaultDaytimeSystem;
        return daytimeSystem;
    }
}