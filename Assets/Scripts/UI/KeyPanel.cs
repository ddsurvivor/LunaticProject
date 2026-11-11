using UnityEngine;

/// <summary>打开绑定的菜单；Esc 同时支持发布包，其余调试快捷键仅在编辑器启用。</summary>
public class KeyPanel : MonoBehaviour
{
    public KeyCode keyCode;

    public GameObject panel;

    public UIPanel uiPanel;
    private bool openOnEscapeRelease; // 按下时菜单已打开，则本次释放只交给菜单的关闭按钮。

    void Update()
    {
        if (keyCode == KeyCode.Escape)
        {
            if (Input.GetKeyDown(keyCode))
                openOnEscapeRelease = (uiPanel != null && !uiPanel.gameObject.activeInHierarchy) ||
                                      (panel != null && !panel.activeInHierarchy);
            if (Input.GetKeyUp(keyCode) && openOnEscapeRelease)
            {
                openOnEscapeRelease = false;
                if (panel != null) panel.SetActive(true);
                if (uiPanel != null)
                {
                    var map = 大地图System.instance;
                    if (map != null && map.剧情 != null && map.剧情.isActiveAndEnabled && !map.剧情.IsStoryClosed)
                        map.剧情.OpenStorySettings(uiPanel);
                    else
                        uiPanel.Open();
                }
            }
            return;
        }
#if UNITY_EDITOR
        if (Input.GetKeyDown(keyCode))
        {
            if(panel!=null) panel.SetActive(true);
            if(uiPanel!= null && !uiPanel.gameObject.activeInHierarchy) uiPanel.Open();
        }
#endif
    }
    
}
