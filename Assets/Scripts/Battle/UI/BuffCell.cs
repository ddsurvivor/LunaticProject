using UnityEngine;

/// <summary>共享状态单元；缺图时用文字回退，不依赖每个词条都有专用美术。</summary>
public class BuffCell : MonoBehaviour
{
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Text countText;
    public UnityEngine.UI.Text nameText;
    private UnityEngine.UI.Text fallback;

    /// <summary>兼容旧Buff面板，保留层数/永久和屏障生命含义。</summary>
    public void SetData(BuffState state)
    {
        if (state == null) return;
        Sprite sprite = null;
        BattleScene.Ins?.BM?.spriteManager?.buffIconDic?.TryGetValue(state.buffType, out sprite);
        SetEntry(new BattleStatusEntry(state.buffType.ToChinese(), state.buffType == BuffType.SlowingField
            ? state.barrierHealth.ToString() : state.stacks < 0 ? "∞" : state.stacks.ToString(), "", sprite));
    }

    /// <summary>显示只读条目；有图时显示图标，缺图仍保留名称和数值。</summary>
    public void SetEntry(BattleStatusEntry entry)
    {
        if (entry == null) return;
        if (icon != null) { icon.sprite = entry.Icon; icon.enabled = entry.Icon != null; }
        if (countText != null) { countText.text = entry.Value; countText.gameObject.SetActive(true); }
        if (nameText != null) nameText.text = entry.Name;
        else if (icon != null)
        {
            if (fallback == null)
            {
                var go = new GameObject("MissingIconLabel", typeof(RectTransform), typeof(UnityEngine.UI.Text));
                go.transform.SetParent(icon.transform, false);
                fallback = go.GetComponent<UnityEngine.UI.Text>();
                fallback.font = countText != null ? countText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
                fallback.fontSize = 12;
                fallback.alignment = TextAnchor.MiddleCenter;
                fallback.color = Color.white;
                fallback.raycastTarget = false;
                fallback.rectTransform.anchorMin = Vector2.zero;
                fallback.rectTransform.anchorMax = Vector2.one;
                fallback.rectTransform.offsetMin = fallback.rectTransform.offsetMax = Vector2.zero;
            }
            fallback.gameObject.SetActive(entry.Icon == null);
            fallback.text = entry.Name.Length > 2 ? entry.Name.Substring(0, 2) : entry.Name;
        }
    }
}
