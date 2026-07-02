using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI 配置")]
    public GameObject panelPrefab;       // 挂载了 ItemGetPanel 和 NotificationItem 的预制体
    public GameObject expPanelPrefab;    // 挂载了 ExpGetPanel 和 NotificationItem 的预制体
    public Transform container;          // 左下角的 UI 容器（带 Vertical Layout Group）
    

    [Header("平衡参数")]
    public float spawnInterval = 0.25f;  // 多个道具同时获取时，每个面板蹦出来的间隔时间

    // 对象池
    private List<NotificationItem> pool = new List<NotificationItem>();
    private List<NotificationItem> expPool = new List<NotificationItem>();

    // 缓冲数据队列
    private Queue<NotificationData> dataQueue = new Queue<NotificationData>();
    private bool isProcessingQueue = false;

    // 统一的数据包装结构类型枚举
    private enum NotificationType
    {
        Item,
        Component,
        Exp
    }
    // 统一的数据包装结构
    private class NotificationData
    {
        public NotificationType type;
        public ItemPack itemPack;
        public ComponentData componentData;

        // 经验值专属数据
        public string characterName;
        public string expAmount;

        public NotificationData(ItemPack pack)
        {
            itemPack = pack;
            type = NotificationType.Item;
        }

        public NotificationData(ComponentData comp)
        {
            componentData = comp;
            type = NotificationType.Component;
        }

        public NotificationData(string charName, string exp)
        {
            characterName = charName;
            expAmount = exp;
            type = NotificationType.Exp;
        }
    }

    private void Awake()
    {
        Instance = this;
        InitPool();
    }

    // 初始化 10 个面板放入对象池
    private void InitPool()
    {
        for (int i = 0; i < 10; i++)
        {
            GameObject go = Instantiate(panelPrefab, container);
            go.SetActive(false);
            NotificationItem item = go.GetComponent<NotificationItem>();
            
            // 确保预制体上有 CanvasGroup
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            
            pool.Add(item);
        }
        for (int i = 0; i < 10; i++)
        {
            GameObject go = Instantiate(expPanelPrefab, container);
            go.SetActive(false);
            NotificationItem item = go.GetComponent<NotificationItem>();
            
            // 确保预制体上有 CanvasGroup
            if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
            
            expPool.Add(item);
        }
    }

    // 从对象池获取一个空闲面板
    private NotificationItem GetPooledItem(bool exp)
    {
        List<NotificationItem> targetPool = exp ? expPool : pool;
        foreach (var item in targetPool)
        {
            if (!item.gameObject.activeSelf) return item;
        }
        
        // 如果 10 个不够用（比如瞬间获得极多道具），动态扩容
        GameObject go = Instantiate(panelPrefab, container);
        NotificationItem newItem = go.GetComponent<NotificationItem>();
        pool.Add(newItem);
        return newItem;
    }

    // 回收时的回调
    private void ReturnToPool(NotificationItem item)
    {
        // 可以在这里做一些重置操作
    }

    #region 公开调用接口

    public void PushNotification(ItemPack itemPack)
    {
        dataQueue.Enqueue(new NotificationData(itemPack));
        TryStartQueue();
    }

    public void PushNotification(ComponentData componentData)
    {
        dataQueue.Enqueue(new NotificationData(componentData));
        TryStartQueue();
    }

    
    public void ShowModifyMessage(int index,int attr,int opIndex,int value)
    {
        //棋子名字 0邱悟、1绿、2马赛
        string pieceName = GM.Ins.PLAYERPROFILE.player[index].NAME;
        
        string attrName = GM.Ins.PLAYERPROFILE.player[index].GetAttrName(attr);
        string opText = "";
        switch (opIndex)
        {
            // opIndex: 0读取，1增加，2赋值，3减小
            case 0: opText = "当前"; break;
            case 1: opText = "增加"; break;
            case 2: opText = "设置为"; break;
            case 3: opText = "减少"; break;
        }
        
        string text = $"{attrName}{opText}{value}";
        //ShowMessage(text);
        dataQueue.Enqueue(new NotificationData(pieceName, text));
        TryStartQueue();
    }
    
    [Button("测试物品接口")]
    public void TestPushNotification()
    {
        // 测试 ItemPack
        ItemPack testPack = new ItemPack(
            ItemName.礼盒,1
        ); // 这里需要根据你的实际构造方式创建 ItemPack
        PushNotification(testPack);

        // 测试 ComponentData
        ComponentData compData = GM.Ins.DM.componentConfig.GetData(5);
        PushNotification(compData);
    }
    
    [Button("测试经验接口")]
    public void TestPushExpNotification()
    {
        ShowModifyMessage(0, 22, 1, 10); // 测试经验获取
        ShowModifyMessage(1, 22, 1, 20); // 测试经验获取
    }

    #endregion

    private void TryStartQueue()
    {
        if (!isProcessingQueue && dataQueue.Count > 0)
        {
            StartCoroutine(ProcessQueueRoutine());
        }
    }

    private IEnumerator ProcessQueueRoutine()
    {
        isProcessingQueue = true;

        while (dataQueue.Count > 0)
        {
            NotificationData data = dataQueue.Dequeue();
            NotificationItem uiItem = GetPooledItem(data.type == NotificationType.Exp);

            if (uiItem != null)
            {
                // 将新生成的 UI 移动到 Layout Group 的最下方
                uiItem.transform.SetAsLastSibling();

                // 根据数据类型调用对应的初始化函数
                switch (data.type)
                {
                    case NotificationType.Item:
                        uiItem.Initialize(data.itemPack, ReturnToPool);
                        break;
                    case NotificationType.Component:
                        uiItem.Initialize(data.componentData, ReturnToPool);
                        break;
                    case NotificationType.Exp:
                        uiItem.Initialize(data.characterName, data.expAmount, ReturnToPool);
                        break;
                }
            }

            // 等待一小段时间再弹出下一个，避免瞬间重叠显得突兀
            yield return new WaitForSeconds(spawnInterval);
        }

        isProcessingQueue = false;
    }
}