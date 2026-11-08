using UnityEngine;
using UnityEngine.Events;

public class BattleFinishPanel : MonoBehaviour
{
    public UnityEngine.UI.Text titleText;
    public UnityEngine.UI.Text descriptionText;
    
    public GameObject continueButton;
    public GameObject retryButton;
    [SerializeField] private RectTransform panel;
    [SerializeField] private UnityEngine.UI.ScrollRect rewardScroll;
    private UnityAction continueAction, retryAction; // 仅替换本视图绑定的业务监听。

    public RectTransform Panel => panel;
    public UnityEngine.UI.ScrollRect RewardScroll => rewardScroll;
    /// <summary>正式模板必须包含结果文字、两个按钮、滚动奖励区及独立暂停持有者。</summary>
    public bool HasTemplateBindings => panel != null && titleText != null && descriptionText != null &&
        continueButton != null && continueButton.GetComponent<UnityEngine.UI.Button>() != null &&
        retryButton != null && retryButton.GetComponent<UnityEngine.UI.Button>() != null &&
        rewardScroll != null && rewardScroll.content != null && rewardScroll.viewport != null && GetComponent<ActivePause>() != null;

    /// <summary>首次创建模板时保存布局引用，不在运行期重建或修改布局。</summary>
    public void Configure(RectTransform body, UnityEngine.UI.ScrollRect scroll) { panel = body; rewardScroll = scroll; }

    /// <summary>绑定原结果离场和重开入口，重复绑定不叠加回调，并保留其他监听。</summary>
    public void BindActions(UnityAction onContinue, UnityAction onRetry)
    {
        var next = continueButton.GetComponent<UnityEngine.UI.Button>();
        var retry = retryButton.GetComponent<UnityEngine.UI.Button>();
        if (continueAction != null) next.onClick.RemoveListener(continueAction);
        if (retryAction != null) retry.onClick.RemoveListener(retryAction);
        continueAction = onContinue; retryAction = onRetry;
        if (continueAction != null) next.onClick.AddListener(continueAction);
        if (retryAction != null) retry.onClick.AddListener(retryAction);
    }


    /// <summary>显示结果和只读胜利奖励预告；本入口不发放奖励。</summary>
    public void ShowPanel(bool win, FinishDrop drop =null, int exp = 0, int currency = 0)
    {
        gameObject.SetActive(true);
        titleText.text = win ? "战斗胜利" : "战斗失败";
        descriptionText.text = win ? "战斗胜利" : "战斗失败";
        continueButton.SetActive(true);
        retryButton.SetActive(true);

        if (win && drop != null)
        {
            descriptionText.text += "\n" + drop.PreviewSummary();
        }

        if (win && exp > 0)
        {
            descriptionText.text += $"\n全体经验：{exp}（离开战斗时结算）";
        }
        if (win && currency > 0)
            descriptionText.text += $"\n转账拦截：{currency} 标准货币（离开战斗时结算）";
        if (rewardScroll != null)
        {
            // 每次结果从顶部阅读，布局先按新奖励文字更新，避免沿用上次的滚动偏移。
            Canvas.ForceUpdateCanvases();
            rewardScroll.StopMovement();
            rewardScroll.verticalNormalizedPosition = 1f;
        }
    }
}
