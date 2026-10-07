using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飘字提示管理器
/// 伤害、Buff、治疗和提示共享对象池，同一目标从下向上排列并同时滚动。
/// </summary>
public class TipTextManager : MonoBehaviour
{
    /// <summary>
    /// 提示文本的预设文案数据结构
    /// </summary>
    [System.Serializable]
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
        
        public string healFormat = "+{0}HP";
    }

    [Header("文本内容配置")]
    [SerializeField] private TipPresetData presetData = new();

    [Header("基础设施配置")]
    [SerializeField] private GameObject tipTextPrefab;     // TipText 预制体
    
    [Header("显示效果微调")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Min(0.01f)] private float durationMultiplier = 2f;
    [SerializeField, Min(0.01f)] private float damageBaseDuration = 1.5f;
    [SerializeField, Min(1)] private int defaultFontSize = 100;
    [SerializeField, Min(1)] private int damageFontSize = 140;
    [SerializeField, Min(1)] private int maximumDamageFontSize = 220;
    [SerializeField, Min(0)] private float damageFontGrowth = 20f;
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField, Min(0.1f)] private float rowSpacing = 2.4f;

    private struct TipRequest
    {
        public Vector3 position;
        public string content;
        public Color color;
        public int fontSize;
        public float? baseDuration;
    }

    private sealed class TargetQueue
    {
        public readonly Queue<TipRequest> pending = new();
        public readonly List<TipText> active = new();
    }

    private readonly Dictionary<int, TargetQueue> targetQueues = new();
    private readonly List<int> finishedTargets = new();

    // 内部简易对象池列表
    private List<TipText> _tipPool = new List<TipText>();

    private void Awake()
    {
        presetData ??= new TipPresetData();
    }

    private void Update() => ProcessQueues();
    private void LateUpdate() => ArrangeStacks();

    internal void ProcessQueues()
    {
        finishedTargets.Clear();
        // 必须先清理所有目标的旧引用，再允许对象池将实例分配给其他目标。
        foreach (var queue in targetQueues.Values)
            queue.active.RemoveAll(tip => tip == null || !tip.IsPlaying);
        foreach (var pair in targetQueues)
        {
            var queue = pair.Value;
            // 同一帧的伤害、被动和Buff一起显示，不再等待前一条结束。
            while (queue.pending.Count > 0)
            {
                var tip = GetOrCreateTip();
                if (tip == null) { queue.pending.Clear(); break; }
                var request = queue.pending.Dequeue();
                tip.transform.position = request.position + worldOffset;
                tip.ShowTip(request.content, request.color, request.fontSize,
                    (request.baseDuration ?? tip.BaseDuration) * Mathf.Max(0.01f, durationMultiplier));
                if (tip.IsPlaying) queue.active.Add(tip);
            }
            if (queue.active.Count == 0) finishedTargets.Add(pair.Key);
        }
        foreach (int key in finishedTargets) targetQueues.Remove(key);
        ArrangeStacks();
    }

    private void ArrangeStacks()
    {
        foreach (var queue in targetQueues.Values)
        {
            TipText lower = null;
            // 新消息在底部，较早消息向上让位；每帧保持间距，避免不同寿命的跳字追上彼此。
            for (int i = queue.active.Count - 1; i >= 0; i--)
            {
                var tip = queue.active[i];
                if (tip == null || !tip.IsPlaying) continue;
                if (lower != null)
                {
                    float spacing = Mathf.Max(0.1f, rowSpacing) *
                        Mathf.Max(tip.FontSize, lower.FontSize) / Mathf.Max(1f, defaultFontSize);
                    tip.EnsureAbove(lower.transform.position.y + spacing);
                }
                lower = tip;
            }
        }
    }

    private void OnDisable()
    {
        targetQueues.Clear();
        foreach (var tip in _tipPool) if (tip != null) tip.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var tip in _tipPool) if (tip != null) Destroy(tip.gameObject);
        _tipPool.Clear();
    }

    /// <summary>
    /// 内部核心：从对象池获取或创建新的提示文本实例
    /// </summary>
    private TipText GetOrCreateTip()
    {
        // 1. 遍历列表，寻找当前未激活的旧对象
        for (int i = 0; i < _tipPool.Count; i++)
        {
            if (_tipPool[i] != null && !_tipPool[i].IsPlaying)
            {
                return _tipPool[i];
            }
        }

        // 2. 如果没找到未激活的，则说明池子满了，实例化一个全新的
        if (tipTextPrefab == null) return null;

        GameObject tipGo = Instantiate(tipTextPrefab);
        TipText tipScript = tipGo.GetComponent<TipText>();

        if (tipScript != null)
        {
            tipGo.SetActive(false); // 初始保持隐藏
            _tipPool.Add(tipScript); // 记录到池中
        }
        else Destroy(tipGo);

        return tipScript;
    }

    /// <summary>
    /// 内部核心方法：处理3D世界坐标转换、位置初始化并触发显示
    /// </summary>
    /// <param name="target">世界场景物体的 Transform 变换组件</param>
    /// <param name="content">需要显示的具体文本内容</param>
    private void SpawnTipAtTarget(Transform target, string content, Color? textColor = null,
        int? fontSize = null, float? baseDuration = null)
    {
        if (target == null || string.IsNullOrEmpty(content) || !isActiveAndEnabled) return;
        // 棋子的子节点也共享同一目标队列。保存事件位置，死亡/销毁后仍可显示剩余消息。
        var piece = target.GetComponentInParent<PieceController>();
        var owner = piece != null ? piece.transform : target;
        int key = owner.GetInstanceID();
        if (!targetQueues.TryGetValue(key, out var queue)) targetQueues[key] = queue = new TargetQueue();
        queue.pending.Enqueue(new TipRequest
        {
            position = owner.position, content = content, color = textColor ?? Color.white,
            fontSize = Mathf.Max(1, fontSize ?? defaultFontSize), baseDuration = baseDuration
        });
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
    
    public void ShowBuffAdded(Transform target, string buffName, int stackCount,
        Color? color = null, int? fontSize = null)
    {
        string stackText = stackCount > 1 ? $"{stackCount}" : "";
        SpawnTipAtTarget(target, string.Format(presetData.buffAddedFormat, stackText, buffName), color, fontSize);
    }
    
    public void ShowMiss(Transform target, Color? color = null, int? fontSize = null)
    {
        SpawnTipAtTarget(target, presetData.missText, color, fontSize);
    }
    
    public void ShowTip(Transform target, string content, Color? color = null, int? fontSize = null)
    {
        SpawnTipAtTarget(target, content, color, fontSize);
    }

    public void ShowHeal(Transform target, int healAmount, Color? color = null, int? fontSize = null)
    {
        // 显示绿色的治疗提示
        SpawnTipAtTarget(target, string.Format(presetData.healFormat, healAmount), color ?? Color.green, fontSize);
    }

    public int GetDamageFontSize(int damage)
    {
        int baseSize = Mathf.Max(defaultFontSize + 1, damageFontSize);
        return CalculateDamageFontSize(damage, baseSize, maximumDamageFontSize, damageFontGrowth);
    }

    public static int CalculateDamageFontSize(int damage, int baseSize = 140, int maximum = 220, float growthRate = 20f)
    {
        baseSize = Mathf.Max(1, baseSize);
        int growth = Mathf.RoundToInt(Mathf.Log10(Mathf.Max(0, damage) + 1f) * Mathf.Max(0f, growthRate));
        return Mathf.Clamp(baseSize + growth, baseSize, Mathf.Max(baseSize, maximum));
    }

    public void ShowDamage(Transform target, int damage, bool isCritical = false,
        Color? color = null, int? fontSize = null)
    {
        if (damage < 0) return;
        SpawnTipAtTarget(target, damage.ToString(), color ?? (isCritical ? criticalColor : damageColor),
            fontSize ?? GetDamageFontSize(damage), damageBaseDuration);
    }

    #endregion
}
