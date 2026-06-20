using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 飘字提示管理器
/// 内置对象池自回收机制，负责在指定目标位置动态分配并显示文本提示。
/// </summary>
public class TipTextManager : MonoBehaviour
{
    /// <summary>
    /// 提示文本的预设文案数据结构
    /// </summary>
    //[System.Serializable]
    public class TipPresetData
    {
        [Tooltip("能量不足时的提示文本")]
        public string energyInsufficient = "能量不足";
        
        [Tooltip("弹药不足时的提示文本")]
        public string ammoInsufficient = "弹药不足";
        
        public string reloadAmmo = "重新装填弹药";
        
        [Tooltip("无法使用时的提示文本")]
        public string cannotUse = "无法使用";
        
        [Tooltip("没有有效目标时的提示文本")]
        public string noValidTarget = "无有效目标";
        
        [Tooltip("道具不足时的格式化提示文本，{0} 会被替换为道具名称")]
        public string itemInsufficientFormat = "{0}不足";
        
        // 添加buff，n层
        public string buffAddedFormat = "+{0}{1}";
        
        public string missText = "未命中";
    }

    [Header("文本内容配置")]
    [SerializeField] private TipPresetData presetData; 

    [Header("基础设施配置")]
    [SerializeField] private GameObject tipTextPrefab;     // TipText 预制体
    [SerializeField] private RectTransform canvasRect;      // UI Canvas 的 RectTransform
    [SerializeField] private Canvas targetCanvas;           // Canvas 组件
    
    [Header("显示效果微调")]
    [SerializeField] private Vector2 uiOffset = new Vector2(0f, 60f); // 弹出位置的纵向偏移

    // 内部简易对象池列表
    private List<TipText> _tipPool = new List<TipText>();
    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        presetData = new TipPresetData();
    }

    /// <summary>
    /// 内部核心：从对象池获取或创建新的提示文本实例
    /// </summary>
    private TipText GetOrCreateTip()
    {
        // 1. 遍历列表，寻找当前未激活的旧对象
        for (int i = 0; i < _tipPool.Count; i++)
        {
            if (_tipPool[i] != null && !_tipPool[i].gameObject.activeInHierarchy)
            {
                return _tipPool[i];
            }
        }

        // 2. 如果没找到未激活的，则说明池子满了，实例化一个全新的
        if (tipTextPrefab == null || canvasRect == null) return null;

        GameObject tipGo = Instantiate(tipTextPrefab, canvasRect);
        TipText tipScript = tipGo.GetComponent<TipText>();

        if (tipScript != null)
        {
            tipGo.SetActive(false); // 初始保持隐藏
            _tipPool.Add(tipScript); // 记录到池中
        }

        return tipScript;
    }

    /// <summary>
    /// 内部核心方法：处理3D世界坐标转换、位置初始化并触发显示
    /// </summary>
    /// <param name="target">世界场景物体的 Transform 变换组件</param>
    /// <param name="content">需要显示的具体文本内容</param>
    private void SpawnTipAtTarget(Transform target, string content)
    {
        if (target == null) return;

        // 1. 从池中获取一个可用的 TipText
        TipText tipScript = GetOrCreateTip();
        if (tipScript == null) return;

        GameObject tipGo = tipScript.gameObject;

        // 2. 将 3D 世界坐标转换为屏幕像素坐标
        Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position);
    
        // 安全裁剪：如果目标物体在相机背面，则不处理（防止在屏幕反方向穿帮弹出提示）
        if (screenPos.z < 0) return;

        // 3. 核心修正：将屏幕像素坐标精准转换为 Canvas 的局部坐标
        // 根据 Canvas 的 RenderMode 自动处理相机参数（Overlay 传 null，Camera 传主相机）
        bool isOverlay = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPos, 
            isOverlay ? null : Camera.main, 
            out Vector2 localPos
        );

        // 4. 严格控制时序：【先修正局部坐标】，【再激活物体】，完美解决旧坐标闪烁问题
        tipGo.transform.localPosition = localPos + uiOffset;
        tipGo.SetActive(true);

        // 5. 调用接口播放动画
        tipScript.ShowTip(content);
    }

    #region 外部快捷调用接口

    /// <summary>
    /// 在指定目标位置弹出“能量不足”提示。
    /// </summary>
    /// <param name="target">目标物体的 Transform（支持 3D 物体或 UI 元素）</param>
    public void ShowEnergyInsufficient(Transform target)
    {
        SpawnTipAtTarget(target, presetData.energyInsufficient);
    }

    /// <summary>
    /// 在指定目标位置弹出“弹药不足”提示。
    /// </summary>
    /// <param name="target">目标物体的 Transform（支持 3D 物体或 UI 元素）</param>
    public void ShowAmmoInsufficient(Transform target)
    {
        SpawnTipAtTarget(target, presetData.ammoInsufficient);
    }
    
    public void ShowReloadAmmo(Transform target)
    {
        SpawnTipAtTarget(target, presetData.reloadAmmo);
    }

    /// <summary>
    /// 在指定目标位置弹出“无法使用”提示。
    /// </summary>
    /// <param name="target">目标物体的 Transform（支持 3D 物体或 UI 元素）</param>
    public void ShowCannotUse(Transform target)
    {
        SpawnTipAtTarget(target, presetData.cannotUse);
    }

    /// <summary>
    /// 在指定目标位置弹出“无有效目标”提示。
    /// </summary>
    /// <param name="target">目标物体的 Transform（支持 3D 物体或 UI 元素）</param>
    public void ShowNoValidTarget(Transform target)
    {
        SpawnTipAtTarget(target, presetData.noValidTarget);
    }

    /// <summary>
    /// 在指定目标位置弹出动态的“XX道具不足”提示。
    /// </summary>
    /// <param name="target">目标物体的 Transform（支持 3D 物体或 UI 元素）</param>
    /// <param name="itemName">缺失的道具或资源名称（例如 "金币"、"血瓶"）</param>
    public void ShowItemInsufficient(Transform target, string itemName)
    {
        SpawnTipAtTarget(target, string.Format(presetData.itemInsufficientFormat, itemName));
    }
    
    public void ShowBuffAdded(Transform target, string buffName, int stackCount)
    {
        string stackText = stackCount > 1 ? $"{stackCount}" : "";
        SpawnTipAtTarget(target, string.Format(presetData.buffAddedFormat, stackText, buffName));
    }
    
    public void ShowMiss(Transform target)
    {
        SpawnTipAtTarget(target, presetData.missText);
    }
    
    public void ShowTip(Transform target, string content)
    {
        SpawnTipAtTarget(target, content);
    }

    #endregion
}