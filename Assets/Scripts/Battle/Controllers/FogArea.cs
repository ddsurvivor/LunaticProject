using UnityEngine;

/// <summary>
/// 迷雾交互行为
/// </summary>
public class FogArea : InteractArea
{
    public GameObject fogEffect;
    /// <summary>
    /// 关闭迷雾效果
    /// </summary>
    public override void TriggerAction()
    {
        base.TriggerAction();
        fogEffect.SetActive(false);
        gameObject.SetActive(false);
        BattleScene.Ins.BM.AIController.OnScanFog(fogEffect);
    }
}