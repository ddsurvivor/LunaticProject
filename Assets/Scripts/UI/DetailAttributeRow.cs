using UnityEngine;
using UnityEngine.UI;

public class DetailAttributeRow : MonoBehaviour {
    public Text attrNameText;
    public Text attrValueText; // 显示格式如 "85 / 100"
    public Image progressBar; // 进度条组件
    public Image previewBar;

    public Image icon;//属性图标
    private int value;
    
    /// <summary>
    /// 更新属性行显示
    /// </summary>
    public void UpdateInfo(string name, int currentVal, int maxVal = 150)
    {
        value = currentVal;
        attrNameText.text = name;
        attrValueText.text = currentVal.ToString();
        attrValueText.color = Color.white;
        ClosePreview();
        // 设置进度条
        if (progressBar != null) {
            //progressBar.maxValue = maxVal;
            progressBar.transform.localScale = new Vector3( (float)currentVal/maxVal, 1f,1f);
        }
    }
    public void ShowPreview(int value, int maxValue = 150) {
        if (previewBar != null) {
            previewBar.gameObject.SetActive(true);
            previewBar.transform.localScale = new Vector3((float)value / maxValue, 1f, 1f);
            attrValueText.text = value.ToString();
            attrValueText.color = Color.yellow; // 预览值显示为黄色
        }
    }
    public void ClosePreview() {
        if (previewBar != null) {
            previewBar.gameObject.SetActive(false);
        }
        attrValueText.text = value.ToString();
        attrValueText.color = Color.white; // 预览值显示为黄色
    }
}