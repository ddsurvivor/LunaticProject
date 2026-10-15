using UnityEngine;

public sealed class SpeedTimelineMarkerUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image portrait;
    [SerializeField] private UnityEngine.UI.Image factionAccent;
    [SerializeField] private GameObject activeHighlight;
    public RectTransform Rect => (RectTransform)transform;
    public UnityEngine.UI.Image Portrait => portrait;

    public void Configure(UnityEngine.UI.Image face, UnityEngine.UI.Image accent, GameObject highlight)
    {
        portrait = face;
        factionAccent = accent;
        activeHighlight = highlight;
    }

    public void SetState(bool active, bool ally)
    {
        factionAccent.color = ally ? new Color(0.38f, 0.75f, 0.85f) : new Color(0.9f, 0.4f, 0.36f);
        activeHighlight.SetActive(active);
    }
}
