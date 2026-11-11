using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 按钮音效自动装配：为使用标准 Button 的 UI 补充进入与点击音效。
/// 自带音效逻辑的按钮组件（EasyButton/StartButton/CustomAdvancedButton/ActionBtn/TutorialUI）跳过，避免重复播放。
/// </summary>
public static class UISound
{
    public static void AttachToScene(Scene scene)
    {
        if (!scene.IsValid()) return;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            AttachToObject(root);
        }
    }

    public static void AttachToObject(GameObject go)
    {
        if (go == null) return;
        foreach (Button button in go.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponent<ButtonSound>() != null) continue;
            if (HasBuiltInSound(button)) continue;
            button.gameObject.AddComponent<ButtonSound>();
        }
    }

    /// <summary>该按钮所在层级已自行播放音效，无需装配。</summary>
    private static bool HasBuiltInSound(Button button)
    {
        if (button.GetComponent<EasyButton>() != null) return true;
        if (button.GetComponent<StartButton>() != null) return true;
        if (button.GetComponent<CustomAdvancedButton>() != null) return true;
        if (button.GetComponent<ActionBtn>() != null) return true;
        if (button.GetComponentInParent<TutorialUI>() != null) return true;
        return false;
    }
}
