using UnityEngine;

/// <summary>指挥节点专属规则；通过场景装置组件接入光环、集火和正式战斗生命周期。</summary>
[RequireComponent(typeof(SceneBattleDevice))]
public sealed class CommandNodeDevice : MonoBehaviour
{
    private SceneBattleDevice device;

    private void Awake()
    {
        device = GetComponent<SceneBattleDevice>();
        // 指挥节点只提供速度和命中增益，不能继承移动指挥中心的暴击加成。
        device.kind = SceneDeviceKind.MobileCommandCenter;
        device.criticalBonus = 0;
        device.OnDead.AddListener(PlayDeath);
    }

    private void PlayDeath()
    {
        var display = device.pieceDisplay;
        if (display == null || display.pieceSpriteRenderer == null || display.deathSprites == null || display.deathSprites.Count == 0) return;
        // 通用装置死亡时立即关闭渲染；节点恢复渲染直至自身死亡序列结束。
        display.pieceSpriteRenderer.enabled = true;
        display.ChangeDisplayState(PieceDisplayState.Death, finish: () =>
        {
            if (display != null && display.pieceSpriteRenderer != null) display.pieceSpriteRenderer.enabled = false;
        });
    }

    private void OnDestroy()
    {
        if (device != null) device.OnDead.RemoveListener(PlayDeath);
    }
}
