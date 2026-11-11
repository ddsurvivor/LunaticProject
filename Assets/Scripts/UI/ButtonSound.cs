using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 标准 Button 的进入与点击音效，由 UISound 自动装配到未自带音效的按钮上。
/// </summary>
public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanPlay()) return;
        GM.Ins.AM.PlayAudio(AudioCueType.Hover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanPlay()) return;
        GM.Ins.AM.PlayAudio(AudioCueType.Click);
    }

    private bool CanPlay()
    {
        if (GM.Ins == null || GM.Ins.AM == null) return false;
        return _button == null || _button.interactable;
    }
}
