using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI 配置")]
    public GameObject panelPrefab;       // 挂载了 ItemGetPanel 和 NotificationItem 的预制体
    public Transform container;          // 左下角的 UI 容器（带 Vertical Layout Group）

    [Header("平衡参数")]
    public float spawnInterval = 0.25f;  // 多个道具同时获取时，每个面板蹦出来的间隔时间

    // 对象池
    private List<NotificationItem> pool = new List<NotificationItem>();
    
    // 缓冲数据队列
    private Queue<NotificationData> dataQueue = new Queue<NotificationData>();
    private bool isProcessingQueue = false;

    // 统一的数据包装结构
    private class NotificationData
    {
        public ItemPack itemPack;
        public ComponentData componentData;
        public bool isComponent;

        public NotificationData(ItemPack pack) { itemPack = pack; isComponent = false; }
        public NotificationData(ComponentData comp) { componentData = comp; isComponent = true; }
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
    }

    // 从对象池获取一个空闲面板
    private NotificationItem GetPooledItem()
    {
        foreach (var item in pool)
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
    
    //[Button("测试接口")]
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
            NotificationItem uiItem = GetPooledItem();

            if (uiItem != null)
            {
                // 将新生成的 UI 移动到 Layout Group 的最下方
                uiItem.transform.SetAsLastSibling();

                if (data.isComponent)
                {
                    uiItem.Initialize(data.componentData, ReturnToPool);
                }
                else
                {
                    uiItem.Initialize(data.itemPack, ReturnToPool);
                }
            }

            // 等待一小段时间再弹出下一个，避免瞬间重叠显得突兀
            yield return new WaitForSeconds(spawnInterval);
        }

        isProcessingQueue = false;
    }
}