using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // 必须引用，处理鼠标事件
using System.Collections.Generic;

public class CharacterUnitUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Data")]
    public int unitID; // 角色编号
    public Player playerData;

    [Header("Normal State UI")]
    public Image portraitImage;
    public Text nameText;
    public Text levelText;

    [Header("Detail Panel UI")]
    public GameObject detailPanel;      // 详细面板的 GameObject
    //public Transform detailContainer;   // 放置属性行的容器 (建议挂载 Vertical Layout Group)
    //public GameObject detailRowPrefab;  // 属性行预制体

    [SerializeField]
    private List<DetailAttributeRow> cachedRows = new List<DetailAttributeRow>();

    void Start() {
        InitBasicInfo();
        detailPanel.SetActive(false); // 初始隐藏详细面板
        //PrepareDetailRows();
    }

    // 初始化常驻显示信息
    public void InitBasicInfo()
    {
        playerData = GM.Ins.PLAYERPROFILE.GetPlayer(unitID);
        nameText.text = playerData.NAME;
        levelText.text = playerData.Level.ToString();
        // portraitImage.sprite = ... (根据需要赋值)
    }

    // 预先生成10行属性，避免实时 Instantiate 造成卡顿
    /*void PrepareDetailRows() {
        for (int i = 0; i < 10; i++) {
            GameObject go = Instantiate(detailRowPrefab, detailContainer);
            DetailAttributeRow row = go.GetComponent<DetailAttributeRow>();
            cachedRows.Add(row);
        }
    }*/

    // 刷新详细面板中的数值
    void RefreshDetailPanel() {
        for (int i = 0; i < 10; i++) {
            string n = playerData.GetAttrName(i);
            int v = playerData.AccessAttribute(i, AttrOp.Get);
            if(i<cachedRows.Count) cachedRows[i].UpdateInfo(n, v);
        }
    }

    // --- 接口实现 ---

    // 鼠标移入
    public void OnPointerEnter(PointerEventData eventData) {
        RefreshDetailPanel();
        detailPanel.SetActive(true);
    }

    // 鼠标移出
    public void OnPointerExit(PointerEventData eventData) {
        detailPanel.SetActive(false);
    }
}