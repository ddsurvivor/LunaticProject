// PieceHeadUI.cs
using UnityEngine;
using UnityEngine.UI;
//using TMPro;

public class PieceHeadUI : MonoBehaviour
{
    [Header("UI组件")]
    public Image pieceIcon;          // 棋子头像
    public Image healthBar;          // 血量条
    public Image healthBarBackground; // 血量条背景
    public Text healthText; // 血量文本
    public GameObject selectionHighlight; // 选中高亮框
    
    [Header("属性")]
    public int pieceId;             // 棋子唯一ID
    [Range(0, 1)] public float healthPercent = 1f; // 血量百分比
    
    [Header("缩放设置")]
    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.2f, 1.2f, 1.2f);
    
    private void Start()
    {
        if (pieceIcon == null)
            pieceIcon = GetComponent<Image>();
            
        UpdateHealthDisplay();
        SetSelected(false);
    }
    
    // 设置选中状态
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlight != null)
            selectionHighlight.SetActive(isSelected);
            
        // 设置缩放
        transform.localScale = isSelected ? selectedScale : normalScale;
        
        // 可选：添加选中时的颜色变化
        if (pieceIcon != null)
        {
            pieceIcon.color = isSelected ? 
                new Color(1.2f, 1.2f, 1.2f, 1f) : 
                Color.white;
        }
    }
    
    // 更新血量显示
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
        UpdateHealthDisplay();
    }
    
    // 更新血量UI
    private void UpdateHealthDisplay()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = healthPercent;
            
            // 根据血量改变颜色
            if (healthPercent > 0.6f)
                healthBar.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBar.color = Color.yellow;
            else
                healthBar.color = Color.red;
        }
        
        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(healthPercent * 100) + "%";
        }
    }
    
    // 点击头像（可选功能）
    public void OnHeadClick()
    {
        // 可以通过事件系统通知选中该棋子
        // PieceHeadListManager.Instance?.SelectPiece(pieceId);
    }
}