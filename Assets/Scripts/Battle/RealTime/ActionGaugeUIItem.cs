using UnityEngine;
using UnityEngine.UI;

public class ActionGaugeUIItem : MonoBehaviour
{
    [SerializeField] private Text nameText;
    [SerializeField] private Text mpValueText; // 可选：显示数字（如 "3 / 6"）

    [Header("Filled Image Progress Bars")] [SerializeField]
    private Image currentMPFill; // 已获得的整数行动力 (底色/硬格)

    [SerializeField] private Image chargingFill; // 正在充能中的动态进度 (前景色/平滑充能)

    /// <summary>
    /// 刷新 UI 进度条
    /// </summary>
    /// <param name="unitName">棋子名称</param>
    /// <param name="currentMP">当前整数行动力</param>
    /// <param name="maxMP">最大行动力上限</param>
    /// <param name="chargeProgress">单点充能进度 (0.0 ~ 1.0)</param>
    public void UpdateView(string unitName, int currentMP, int maxMP, float chargeProgress)
    {
        if (nameText != null)
            nameText.text = unitName;

        if (mpValueText != null)
            mpValueText.text = $"{currentMP}/{maxMP}";

        float maxMPFloat = Mathf.Max(1, maxMP);

        // 1. 已获取的整数行动力比例 (如 3点/6点 = 0.5)
        if (currentMPFill != null)
        {
            currentMPFill.fillAmount = (float)currentMP / maxMPFloat;
        }

        // 2. 连续平滑充能总进度 (已完成整数 + 当前充能比例)
        if (chargingFill != null)
        {
            chargingFill.fillAmount = chargeProgress / 1.0f;
        }
    }
}