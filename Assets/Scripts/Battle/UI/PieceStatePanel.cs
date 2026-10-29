
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieceStatePanel : UIPanel
{
    public PieceController pc;
    
    //public Text nameText;
    //public Text healthText;
    //public Text mpText;
    public Text ammoText;
    public Text mpText;
    public Image healthBar;
    //public Image mpBar;
    public Image manaBar;

    public Transform upRoot;
    [HideInInspector] public float upRootBaseY = 50f; // 保留旧序列化值，新定位不再使用固定缩放偏移。
    [HideInInspector] public float upRootScaleFactor = 50f;
    //public Text manaText;

    public UnityEngine.UI.Image healthShadowBar;

    private readonly HealthBarFillAnimation healthAnimation = new();
    private readonly List<Graphic> headerGraphics = new();
    private readonly Vector3[] screenCorners = new Vector3[4];

    private void OnDisable() => healthAnimation.Reset();

    public void Update()
    {
        if (pc != null && Camera.main != null)
        {
            // 计算棋子在屏幕中的位置，更新行动面板的位置
            Vector3 screenPos = Camera.main.WorldToScreenPoint(pc.transform.position);
            transform.position = screenPos + new Vector3(0, 100, 0);
            
            PositionHeader();
        }
    }

    private void PositionHeader()
    {
        if (upRoot == null) return;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var sprite = pc.pieceDisplay != null ? pc.pieceDisplay.pieceSpriteRenderer : null;
        var head = sprite != null ? new Vector3(sprite.bounds.center.x, sprite.bounds.max.y, sprite.bounds.center.z)
            : pc.transform.position + Vector3.up * 2;
        var point = (Vector2)Camera.main.WorldToScreenPoint(head);
        point.y += 24 * canvas.scaleFactor;
        SetHeaderScreenPosition(point, uiCamera);
        upRoot.GetComponentsInChildren(false, headerGraphics);
        bool found = false;
        var bounds = new Rect();
        foreach (var graphic in headerGraphics)
        {
            if (!graphic.enabled) continue;
            var rect = ScreenBounds(graphic.rectTransform, uiCamera);
            if (!found) { bounds = rect; found = true; }
            else bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, rect.xMin), Mathf.Min(bounds.yMin, rect.yMin),
                Mathf.Max(bounds.xMax, rect.xMax), Mathf.Max(bounds.yMax, rect.yMax));
        }
        if (!found) return;
        var safe = Screen.safeArea;
        float margin = 12 * canvas.scaleFactor;
        float dx = bounds.xMin < safe.xMin + margin ? safe.xMin + margin - bounds.xMin
            : bounds.xMax > safe.xMax - margin ? safe.xMax - margin - bounds.xMax : 0;
        float ceiling = safe.yMax - margin;
        var timeline = BattleScene.Ins.UM.ActionTimelineRect;
        if (timeline != null)
        {
            var occupied = ScreenBounds(timeline, uiCamera);
            if (bounds.xMax + dx > occupied.xMin && bounds.xMin + dx < occupied.xMax)
                ceiling = Mathf.Min(ceiling, occupied.yMin - margin);
        }
        // 按实际可见资源图形的边界避让，而不是继续叠加与镜头缩放成反比的偏移。
        float dy = Mathf.Min(0, ceiling - bounds.yMax);
        dy = Mathf.Max(dy, safe.yMin + margin - bounds.yMin);
        SetHeaderScreenPosition(point + new Vector2(dx, dy), uiCamera);
    }

    private void SetHeaderScreenPosition(Vector2 point, Camera uiCamera)
    {
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)upRoot.parent, point, uiCamera, out var world))
            upRoot.position = world;
    }

    private Rect ScreenBounds(RectTransform rect, Camera uiCamera)
    {
        rect.GetWorldCorners(screenCorners);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (var corner in screenCorners)
        {
            var point = RectTransformUtility.WorldToScreenPoint(uiCamera, corner);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void OpenPanel(PieceController pc)
    {
        if(!pc.isPlayerPiece) return;
        this.pc = pc;
        GetComponent<ActionCostPreviewUI>()?.Bind(pc.unitAttrCenter);
        gameObject.SetActive(true);
        UpdateDisplay();
    }
    public override void UpdateDisplay()
    {
        base.UpdateDisplay();
        if (pc != null)
        {
            // 更新显示pc的状态信息
            //nameText.text = pc.pieceData.pieceName;
            //healthText.text = $"{pc.unitAttrCenter.CurHealth}/{pc.unitAttrCenter.MaxHealth}";
            //mpText.text = $"{pc.unitAttrCenter.CurMovePoint}/{pc.unitAttrCenter.MaxMovePoint}";
            healthAnimation.Update(healthBar, healthShadowBar, pc.unitAttrCenter.MaxHealth > 0
                ? (float)pc.unitAttrCenter.CurHealth / pc.unitAttrCenter.MaxHealth : 0f, pc);
            //mpBar.fillAmount = (float)pc.unitAttrCenter.CurMovePoint / pc.unitAttrCenter.MaxMovePoint;
            ammoText.text =  $"{pc.unitAttrCenter.AmmoCount}/{pc.unitAttrCenter.MaxAmmoCount}";
            mpText.text = $"{pc.unitAttrCenter.CurMovePoint}/{pc.unitAttrCenter.MaxMovePoint}";
            manaBar.fillAmount = pc.unitAttrCenter.MaxManaPoint > 0
                ? (float)pc.unitAttrCenter.ManaPoint / pc.unitAttrCenter.MaxManaPoint : 0f;
            //manaText.text = $"{pc.unitAttrCenter.ManaPoint}/{pc.unitAttrCenter.MaxManaPoint}";
        }
    }
}
