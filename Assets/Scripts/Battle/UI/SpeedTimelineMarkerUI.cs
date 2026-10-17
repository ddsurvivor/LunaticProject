using UnityEngine;

public sealed class SpeedTimelineMarkerUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private UnityEngine.UI.Image factionAccent;
    [SerializeField] private GameObject activeHighlight; // 仅当前行动角色显示的强调框。
    public RectTransform Rect => (RectTransform)transform;
    public UnityEngine.UI.Image Portrait => portrait;

    /// <summary>供预制体生成器绑定头像、阵营色条和当前行动强调框。</summary>
    public void Configure(UnityEngine.UI.Image face, UnityEngine.UI.Image accent, GameObject highlight)
    {
        portrait = face;
        factionAccent = accent;
        activeHighlight = highlight;
    }

    /// <summary>按阵营设置色条，并切换当前行动角色的高亮表现。</summary>
    public void SetState(bool active, bool ally)
    {
        factionAccent.color = ally ? new Color(0.38f, 0.75f, 0.85f) : new Color(0.9f, 0.4f, 0.36f);
        activeHighlight.SetActive(active);
    }
}
