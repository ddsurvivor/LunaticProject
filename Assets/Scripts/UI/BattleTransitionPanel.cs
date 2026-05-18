using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 使用旧版 UI 组件
using UnityEngine.SceneManagement;

 public class BattleTransitionPanel : MonoBehaviour
{
    //public static BattleTransitionManager Instance { get; private set; }

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
        
        InitUIState();
        
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
        StartCoroutine(TransitionRoutine(battleSceneName));
    }

    private IEnumerator TransitionRoutine(string battleSceneName)
    {
        // ==========================================
        // 1. 黑色背景渐入 (Fade In)
        // ==========================================
        yield return StartCoroutine(Fade(1f));

        // ==========================================
        // 2. 显示加载界面并重置进度条
        // ==========================================
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (progressBar != null) progressBar.fillAmount = 0f;
        if (progressText != null) progressText.text = "0%";

        // ==========================================
        // 3. 异步加载战斗场景（先不激活）
        // ==========================================
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[BattleTransition] 无法加载场景: {battleSceneName}，请检查 Build Settings。");
            yield break;
        }
        // 当此项为 false 时，场景加载到 90% (0.9) 就会暂停，等待手动激活
        asyncLoad.allowSceneActivation = false;

        // ==========================================
        // 4. 手动控制时长的假进度条逻辑
        // ==========================================
        float elapsed = 0f;
        while (elapsed < fakeLoadingDuration || asyncLoad.progress < 0.9f)
        {
            elapsed += Time.deltaTime;
            
            // 计算时间带来的“假进度” (0 ~ 1)
            float timeProgress = elapsed / fakeLoadingDuration;
            // 实际的加载进度 (asyncLoad.progress 最大为 0.9，所以映射到 0 ~ 1)
            float realProgress = asyncLoad.progress / 0.9f; 
            
            // 取两者的较小值，确保即使实际加载完了，也会等足手动设置的时长
            float currentProgress = Mathf.Min(timeProgress, realProgress, 1f);

            // 更新 UI
            if (progressBar != null) progressBar.fillAmount = currentProgress;
            if (progressText != null) progressText.text = $"{(currentProgress * 100f):F0}%";

            yield return null;
        }

        // 强行平滑到 100% 并稍微停顿，视觉体验更佳
        if (progressBar != null) progressBar.fillAmount = 1f;
        if (progressText != null) progressText.text = "100%";
        yield return new WaitForSeconds(0.1f);

        // ==========================================
        // 5. 允许激活新场景并等待加载完成
        // ==========================================
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // ==========================================
        // 6. 关闭加载界面
        // ==========================================
        if (loadingPanel != null) loadingPanel.SetActive(false);

        // ==========================================
        // 7. 黑色背景淡出 (Fade Out)
        // ==========================================
        yield return StartCoroutine(Fade(0f));

        // ==========================================
        // 8. 场景完全加载并恢复后，通知战斗管理器
        // ==========================================
        NotifyBattleManager();
    }

    /// <summary>
    /// 控制 CanvasGroup 变暗或变透明的通用协程
    /// </summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// 寻找新场景中的 BattleManager 并发送转场结束通知
    /// </summary>
    private void NotifyBattleManager()
    {
        // 此处假设新场景中存在带有特定命名或单例的 BattleManager
        // 采用 FindObjectOfType 进行解耦查找（也可以替换为你项目中的全局事件系统）
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.StartBattle();
        }
        else
        {
            Debug.LogWarning("[BattleTransition] 转场已完成，但在新场景中未找到 BattleManager。");
        }
    }
}