using UnityEngine;
using UnityEngine.UI;

public class AttributeRowUI : MonoBehaviour {
    public Text attrNameText;
    public Text valueText;
    public Text pendingAddText; // 显示 +N
    public Slider progressBar;
    public Button plusBtn;
    public Button minusBtn;

    private int baseValue;
    private int pendingAdd = 0;
    private int maxValue = 100;

    public int PendingAdd => pendingAdd;

    public void Setup(string name, int currentVal, int max) {
        attrNameText.text = name;
        baseValue = currentVal;
        maxValue = max;
        pendingAdd = 0;
        UpdateUI();
    }

    public void ChangePending(int delta, ref int globalPoints) {
        // 增加时检查剩余点数，减少时检查是否大于0
        if (delta > 0 && globalPoints <= 0) return;
        if (delta < 0 && pendingAdd <= 0) return;

        pendingAdd += delta;
        globalPoints -= delta;
        UpdateUI();
    }

    public void ResetRow(ref int globalPoints) {
        globalPoints += pendingAdd;
        pendingAdd = 0;
        UpdateUI();
    }

    public void UpdateUI() {
        valueText.text = baseValue.ToString();
        pendingAddText.text = pendingAdd > 0 ? "+" + pendingAdd : "0";
        progressBar.value = (float)(baseValue + pendingAdd) / maxValue;
    }
    
    public void Commit() {
        baseValue += pendingAdd; // 将当前的加点正式合入基础值
        pendingAdd = 0;
        UpdateUI();
    }
}