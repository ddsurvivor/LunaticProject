using UnityEngine;

/// <summary>指挥节点专属规则；通过场景装置组件接入光环、集火和正式战斗生命周期。</summary>
[RequireComponent(typeof(SceneBattleDevice))]
public sealed class CommandNodeDevice : MonoBehaviour
{
    private void Awake()
    {
        var device = GetComponent<SceneBattleDevice>();
        // 指挥节点只提供速度和命中增益，不能继承移动指挥中心的暴击加成。
        device.kind = SceneDeviceKind.MobileCommandCenter;
        device.criticalBonus = 0;
    }
}
