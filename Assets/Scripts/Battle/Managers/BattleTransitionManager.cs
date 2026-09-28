using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 使用旧版 UI 组件
using UnityEngine.SceneManagement;

 public class BattleTransitionManager : MonoBehaviour
{
    public static BattleTransitionManager Instance { get; private set; }

    [Header("UI 元素引用")]
    [Tooltip("用于控制黑屏渐变的面版，建议挂载 CanvasGroup 组件")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [Tooltip("整个加载界面的根节点 GameObject")]
    [SerializeField] private GameObject loadingPanel;
    [Tooltip("进度条图片，Image Type 需设置为 Filled")]
    [SerializeField] private Image progressBar;
    [Tooltip("提示文本或百分比文本（使用旧版 Text）")]
    [SerializeField] private Text progressText;

    [Header("转场配置")]
    [Tooltip("黑屏渐入/渐出的持续时间（秒）")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("手动的假加载持续时间（秒）")]
    [SerializeField] private float fakeLoadingDuration = 2.0f;

    private void Awake()
    {
        // 确保单例唯一性并设置为不灭物体
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitUIState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化UI状态
    /// </summary>
    private void InitUIState()
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    /// <summary>
    /// 外部调用的主入口：开始转场去战斗场景
    /// </summary>
    /// <param name="battleSceneName">目标战斗场景的名称</param>
    public void TransitionToBattle(string battleSceneName)
    {
        GM.Ins.StartBattle(battleSceneName);
    }
}