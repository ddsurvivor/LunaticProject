using System;
using DG.Tweening;
using UnityEngine;


public class SidePanel : MonoBehaviour
{
    public Transform panel;
    public GameObject bkBG;

    public float slidePosX = 500f;
    public float slideDuration = 0.3f;
    public KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private PowerSpinePanel powerSpinePrefab;
    private PowerSpinePanel powerSpineWindow;
    private UnityEngine.UI.Button powerSpineEntry;

    private void Start()
    {
        if (panel == null || BattleScene.Ins != null) return;
        var prefab = Resources.Load<UnityEngine.UI.Button>("UI/PowerSpineEntry");
        if (prefab == null) return;
        powerSpineEntry = Instantiate(prefab, panel, false);
        powerSpineEntry.onClick.AddListener(OpenPowerSpine);
    }

    /// <summary>战外侧栏的设施入口；可指定共享模板变体，不允许战斗或转场中打开。</summary>
    public void OpenPowerSpine()
    {
        if (!PowerSpinePanel.CanOpen) return;
        if (powerSpineWindow == null)
        {
            var prefab = powerSpinePrefab != null ? powerSpinePrefab : Resources.Load<PowerSpinePanel>("UI/PowerSpinePanel");
            var canvas = GetComponentInParent<Canvas>();
            if (prefab == null || !prefab.HasTemplateBindings || canvas == null) return;
            powerSpineWindow = Instantiate(prefab, canvas.rootCanvas.transform, false);
        }
        ClosePanel();
        powerSpineWindow.transform.SetAsLastSibling(); powerSpineWindow.gameObject.SetActive(true);
    }

    public void Update()
    {
        if (powerSpineEntry != null) powerSpineEntry.gameObject.SetActive(PowerSpinePanel.CanOpen);
        if (Input.GetKeyUp(toggleKey))
        {
            // 如果侧栏当前不可见，则显示；如果可见，则关闭
            if (!panel.gameObject.activeSelf)
            {
                ShowPanel();
            }
            else
            {
                ClosePanel();
            }
        }
    }

    public void ShowPanel()
    {
        // 激活侧栏，并移动到指定点
        panel.gameObject.SetActive(true);
        panel.DOMoveX(0, slideDuration).From(-slidePosX).SetEase(Ease.OutCubic)
            .SetUpdate(UpdateType.Normal,true);
        GM.Ins.AM.PlayAudio(AudioCueType.Expand);
        if (bkBG != null)
        {
            bkBG.SetActive(true);
        }
    }

    public void ClosePanel()
    {
        // 移动侧栏回原位，并在动画完成后隐藏
        panel.DOMoveX(-slidePosX, slideDuration).SetEase(Ease.InCubic)
            .SetUpdate(UpdateType.Normal,true)
            .OnComplete(() =>
        {
            panel.gameObject.SetActive(false);
        });
        GM.Ins.AM.PlayAudio(AudioCueType.Collapse);
        if (bkBG != null)
        {
            bkBG.SetActive(false);
        }
    }
}
