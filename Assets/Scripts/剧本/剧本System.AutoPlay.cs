using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;

public partial class 剧本System
{
    // ===== 自动播放系统 ====== //
    private bool isAutoPlay = false; // 玩家选择的自动模式，不因等待选项或关闭面板而清除
    private bool isFastForward = false;
    private bool isWaitingForChoice = false;
    private float autoPlayElapsed;
    public bool IsAutoPlay => isAutoPlay;
    public bool IsFastForward => isFastForward;
    public bool IsWaitingForChoice => isWaitingForChoice;

    [Header("自动播放")] [Tooltip("自动按钮；留空时查找子物体中已绑定 ToggleAutoPlay 的按钮")] [SerializeField]
    private CustomAdvancedButton autoPlayButton;

    [Tooltip("快进按钮；留空时查找子物体中已绑定 ToggleFastForward 的按钮")] [SerializeField]
    private CustomAdvancedButton fastForwardButton;

    [SerializeField] private float normalDelay = 2.0f;
    [SerializeField] private float fastForwardDelay = 0.3f;

    public void ToggleAutoPlay()
    {
        isAutoPlay = !isAutoPlay;
        autoPlayElapsed = 0f;
        SyncPlaybackButtons();
        Debug.Log(isAutoPlay ? "【剧情系统】已开启自动播放" : "【剧情系统】已关闭自动播放");
    }

    public void ToggleFastForward()
    {
        isFastForward = !isFastForward;
        autoPlayElapsed = 0f;
        SyncPlaybackButtons();
        Debug.Log($"【剧情系统】快进状态: {isFastForward}");
    }

    /// <summary>玩家主动关闭自动模式，同时同步按钮状态。</summary>
    public void StopAutoPlay()
    {
        isAutoPlay = false;
        autoPlayElapsed = 0f;
        SyncPlaybackButtons();
    }

    private void SyncPlaybackButtons()
    {
        SyncPlaybackButton(ref autoPlayButton, nameof(ToggleAutoPlay), isAutoPlay);
        SyncPlaybackButton(ref fastForwardButton, nameof(ToggleFastForward), isFastForward);
    }

    private void SyncPlaybackButton(ref CustomAdvancedButton target, string methodName, bool isOn)
    {
        if (target == null)
        {
            foreach (var button in GetComponentsInChildren<CustomAdvancedButton>(true))
            {
                if (CallsPlaybackMethod(button.onClickEvent, methodName) ||
                    (button.TryGetComponent<UnityEngine.UI.Button>(out var uiButton) &&
                     CallsPlaybackMethod(uiButton.onClick, methodName)))
                {
                    target = button;
                    break;
                }
            }
        }

        if (target != null)
        {
            target.SetToggleMode(true);
            target.SetIsOn(isOn);
        }
    }

    private bool CallsPlaybackMethod(UnityEngine.Events.UnityEvent clickEvent, string methodName)
    {
        if (clickEvent == null) return false;
        for (int i = 0; i < clickEvent.GetPersistentEventCount(); i++)
        {
            if (clickEvent.GetPersistentTarget(i) == this &&
                clickEvent.GetPersistentMethodName(i) == methodName)
                return true;
        }

        return false;
    }

    private void Update()
    {
        if (UpdateReaderInput()) return;

        // 使用随组件启停的计时，避免面板隐藏后残留已被 Unity 停止的协程引用。
        if (!isAutoPlay || isWaitingForChoice || 已储存剧本 == null || 已阅读 >= 已储存剧本.Length)
        {
            autoPlayElapsed = 0f;
            return;
        }

        autoPlayElapsed += Time.deltaTime;
        float delay = Mathf.Max(0f, isFastForward ? fastForwardDelay : normalDelay);
        if (autoPlayElapsed >= delay)
        {
            autoPlayElapsed = 0f;
            Next();
        }
    }
    private void OnEnable()
    {
        autoPlayElapsed = 0f;
        SyncPlaybackButtons();
    }

    private void OnDisable()
    {
        // 隐藏剧情只暂停推进；玩家选择的自动和快进模式保留到下一段剧本。
        autoPlayElapsed = 0f;
        GM.Ins?.AM.StopAll();
    }
}
