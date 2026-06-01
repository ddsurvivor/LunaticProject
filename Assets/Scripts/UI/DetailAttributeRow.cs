using UnityEngine;
using UnityEngine.UI;

public class DetailAttributeRow : MonoBehaviour {
    public Text attrNameText;
    public Text attrValueText; // 显示格式如 "85 / 100"
    public Image progressBar; // 进度条组件

    public Image icon;//属性图标
    
    /// <summary>
    /// 更新属性行显示
    /// </summary>
    public void UpdateInfo(string name, int currentVal, int maxVal = 150) {
        attrNameText.text = name;
        attrValueText.text = currentVal.ToString();
        
        // 设置进度条
        if (progressBar != null) {
            //progressBar.maxValue = maxVal;
            progressBar.transform.localScale = new Vector3( (float)currentVal/maxVal, 1f,1f);
        }
    }
}