using UnityEngine;

public partial class EnemyController
{
    private readonly Vector3[] targetLinePositions = new Vector3[21];
    private bool targetLineStyleCached;
    private Color targetLineStartColor, targetLineEndColor;
    private float targetLineWidth;

    // 保留现有公开入口：点击显示交由统一交互管理器保持。
    public void ShowTargetLine()
    {
        if (BattleScene.Ins?.CM != null) BattleScene.Ins.CM.PinEnemyTargetLine(this);
        else RenderTargetLine();
    }

    internal bool RenderTargetLine(PieceController previewPiece = null, Vector3? previewPosition = null,
        bool breathe = false, bool requirePreviewTarget = false)
    {
        if (tagetLine == null) return false;
        if (!isActived || isDead || !gameObject.activeInHierarchy)
        {
            HideTargetLine();
            return false;
        }
        var ai = player as AIController ?? BattleScene.Ins?.BM?.AIController;
        _curTargetPc = ai?.PreviewEnemyTarget(this, previewPiece, previewPosition);
        if (_curTargetPc == null || _curTargetPc.isDead || !_curTargetPc.gameObject.activeInHierarchy ||
            (requirePreviewTarget && (previewPiece == null || _curTargetPc != previewPiece)))
        {
            HideTargetLine();
            return false;
        }
        CacheTargetLineStyle();
        Vector3 start = transform.position + Vector3.up * 1.5f;
        Vector3 end = (_curTargetPc == previewPiece && previewPosition.HasValue
            ? previewPosition.Value : _curTargetPc.transform.position) + Vector3.up * 1.5f;
        Vector3 control = (start + end) * 0.5f + Vector3.up * 2.5f;
        for (int i = 0; i < targetLinePositions.Length; i++)
        {
            float t = i / (float)(targetLinePositions.Length - 1);
            targetLinePositions[i] = (1 - t) * (1 - t) * start + 2 * (1 - t) * t * control + t * t * end;
        }
        tagetLine.useWorldSpace = true;
        tagetLine.positionCount = targetLinePositions.Length;
        tagetLine.SetPositions(targetLinePositions);
        // 不实例化/修改共享材质，保留预制体原色；移动预览仅呼吸透明度和宽度。
        float pulse = breathe ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 1.2f) : 1f;
        float alpha = breathe ? Mathf.Lerp(0.35f, 1f, pulse) : 1f;
        var startColor = targetLineStartColor;
        var endColor = targetLineEndColor;
        startColor.a *= alpha;
        endColor.a *= alpha;
        tagetLine.startColor = startColor;
        tagetLine.endColor = endColor;
        tagetLine.widthMultiplier = targetLineWidth * (breathe ? Mathf.Lerp(0.8f, 1.15f, pulse) : 1f);
        // enabled 不能激活被 SetActive(false) 的承载物体。
        if (!tagetLine.gameObject.activeSelf) tagetLine.gameObject.SetActive(true);
        tagetLine.enabled = true;
        return true;
    }

    private void CacheTargetLineStyle()
    {
        if (targetLineStyleCached) return;
        targetLineStartColor = tagetLine.startColor;
        targetLineEndColor = tagetLine.endColor;
        targetLineWidth = tagetLine.widthMultiplier;
        targetLineStyleCached = true;
    }

    public void HideTargetLine()
    {
        _curTargetPc = null;
        if (tagetLine == null) return;
        tagetLine.enabled = false;
        tagetLine.positionCount = 0;
        // 只关闭独立指示线物体，兼容 LineRenderer 挂在敌人根对象上的配置。
        if (tagetLine.gameObject != gameObject && tagetLine.gameObject.activeSelf)
            tagetLine.gameObject.SetActive(false);
        if (!targetLineStyleCached) return;
        tagetLine.startColor = targetLineStartColor;
        tagetLine.endColor = targetLineEndColor;
        tagetLine.widthMultiplier = targetLineWidth;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        HideTargetLine();
    }
}
