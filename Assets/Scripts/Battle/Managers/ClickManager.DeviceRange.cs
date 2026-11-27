using UnityEngine;

public partial class ClickManager
{
    private LineRenderer deviceRangeOutline;
    private Material deviceRangeMaterial; // 检查用圆环独占，随输入管理器销毁。
    private const int DeviceRangeSegments = 90; // 与现有范围线相同的4度采样间隔。

    private void UpdateDeviceRangeInspection()
    {
        var battle = BattleScene.Ins?.BM;
        var device = lastHoveredPiece as SceneBattleDevice;
        if (battle == null || !battle.CanInspectEnemyTargets || GM.Ins.IsTransitioning || battle.HasCombatPresentation ||
            battle.BurstInputPending || battle.moveManager.IsMoving || _isDragging ||
            (_selectedPiece != null && (_selectedPiece.IsUsingSkill || _selectedPiece.isUsingOrder)) ||
            device == null || !device.IsInitialized || !device.isActiveAndEnabled || device.isDead ||
            device.gameObject.scene != gameObject.scene || device.radius <= 0 ||
            float.IsNaN(device.radius) || float.IsInfinity(device.radius))
        {
            HideDeviceRangeInspection();
            return;
        }
        if (deviceRangeOutline == null)
        {
            deviceRangeMaterial = BattleRangeStyle.CreateMaterial();
            var node = new GameObject("DeviceRangeInspection");
            node.transform.SetParent(transform, false);
            deviceRangeOutline = node.AddComponent<LineRenderer>();
            BattleRangeStyle.Configure(deviceRangeOutline, deviceRangeMaterial, BattleRangeStyle.Unavailable);
            deviceRangeOutline.positionCount = DeviceRangeSegments + 1;
        }
        deviceRangeOutline.gameObject.SetActive(true);
        var color = !device.IsOperational ? BattleRangeStyle.Unavailable :
            device.isPlayerPiece ? BattleRangeStyle.Support : BattleRangeStyle.Attack;
        deviceRangeOutline.startColor = deviceRangeOutline.endColor = color;
        // 直接读取装置判定半径，显示同高度截面；不占用技能范围或改变目标/效果。
        var center = device.transform.position + Vector3.up * .08f;
        for (int i = 0; i <= DeviceRangeSegments; i++)
        {
            float angle = i * Mathf.PI * 2 / DeviceRangeSegments;
            deviceRangeOutline.SetPosition(i, center + device.radius * new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)));
        }
    }

    private void HideDeviceRangeInspection()
    {
        if (deviceRangeOutline != null) deviceRangeOutline.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (deviceRangeMaterial != null) Destroy(deviceRangeMaterial);
    }
}
