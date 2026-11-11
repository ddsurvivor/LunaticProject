using UnityEngine;

public partial class 剧本System
{
    private UIPanel storySettingsMenu; // 使用场景已有 Esc 绑定的菜单，不依赖编辑器查找或名称约定。

    private bool IsSettingsMenuOpen
    {
        get
        {
            var map = 大地图System.instance;
            if (map == null || map.剧情 != this) return false;
            if (storySettingsMenu == null)
            {
                foreach (var shortcut in map.GetComponents<KeyPanel>())
                {
                    if (shortcut.keyCode != KeyCode.Escape || shortcut.uiPanel == null) continue;
                    storySettingsMenu = shortcut.uiPanel;
                    break;
                }
            }
            return storySettingsMenu != null && storySettingsMenu.gameObject.activeInHierarchy;
        }
    }

    /// <summary>复用现有设置标签页入口；菜单显示期间暂停剧情推进，关闭后沿用原自动播放状态。</summary>
    public void OpenStorySettings(UIPanel menu)
    {
        if (menu == null || IsStoryClosed || IsReaderHiding) return;
        storySettingsMenu = menu;
        foreach (var tabs in menu.GetComponentsInChildren<UITabController>(true))
        {
            if (!tabs.TryShowSettingsTab()) continue;
            // 设置页可能属于嵌套标签组，切页后还需激活外层菜单。
            if (!menu.gameObject.activeInHierarchy) menu.Open();
            return;
        }
        menu.Open();
    }
}
