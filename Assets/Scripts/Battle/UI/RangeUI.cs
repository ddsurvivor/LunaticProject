using System.Collections.Generic;
using UnityEngine;


public partial class RangeUI : MonoBehaviour
{
    public GameObject circle;
    public GameObject moveIcon;
    public GameObject moveStart;
    public SpriteRenderer moveLine;
    public GameObject attackCircle;
    public GameObject attackIcon;
    public GameObject skillIcon;
    public GameObject skillCircle;
    public GameObject grenadeCircle; // 爆炸范围圈
    public GameObject highlightCircle;
    public GameObject selectCircle;
    public GameObject fanRoot; // 扇形范围根节点
    public UnityEngine.UI.Image fanCircle; // 扇形范围圈
    public GameObject fanLine1;
    public GameObject fanLine2;
    public Transform fanPos;

    [Header("Arc Settings")] [SerializeField]
    private GameObject arcRoot;

    [SerializeField] private UnityEngine.UI.Image arcOuter; // 外圆环
    [SerializeField] private UnityEngine.UI.Image arcInnerMask; // 内圆覆盖（实现宽度）
    [SerializeField] private RectTransform arcLine1; // 边线1

    [SerializeField] private RectTransform arcLine2; // 边线2

    //private float circleRadiusFactor = 1f / 11f / 1.6f; // 基础缩放系数（对应正圆图片的原始尺寸）
    private bool _isShowMoveIcon = false;

    private float circleRadius = 1f / 11f / 1.8f;
    private float _curRange;

    private List<PieceController> _curTargets = new();
    /// <summary>确认行动前重新查询当前目标，避免死亡或离场单位沿用旧高亮快照。</summary>
    public List<PieceController> GetCurTargets { get { RefreshTargets(); return _curTargets; } }
    private SkillPack _curSkillPack;

    public bool isPlayerRange = false;

    private PieceController _owner;

    private void Awake()
    {
        // 从上一级组件获取控制器引用
        if (_owner == null) _owner = GetComponentInParent<PieceController>();
        PreparePresentation();
    }

    public void ShowCircleRange(float radius)
    {
        CloseRange();
        circle.SetActive(true);
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
        DrawRangePresentation();
    }

    public void ShowCircleRange(Vector3 position, float radius)
    {
        CloseRange();
        circle.SetActive(true);
        transform.position =
            new Vector3(position.x, position.y + 0.1f, position.z);
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
        DrawRangePresentation();
    }

    public void ShowAttackRange(float radius)
    {
        CloseRange();
        attackCircle.SetActive(true);
        attackCircle.transform.localScale = radius * circleRadius * Vector3.one;
        attackIcon.SetActive(true);
        _curRange = radius;
        DrawRangePresentation();
    }

    /// <summary>只显示检查用圆环，不启用准星、技能选点或目标高亮。</summary>
    public void ShowInspectionRange(Vector3 position, float radius)
    {
        CloseRange();
        transform.position = new Vector3(position.x, position.y + 0.1f, position.z);
        attackCircle.SetActive(true);
        attackCircle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
        DrawRangePresentation();
    }

    public void ShowSkillRange(SkillPack skillPack)
    {
        CloseRange();
        if (skillPack == null) return;
        _curSkillPack = skillPack;
        _curRange = skillPack.rangeValue;
        _selectionPosition = _owner != null ? _owner.transform.position : transform.position;
        skillIcon.SetActive(skillPack.target != SkillTarget.Self && skillPack.rangeType != RangeType.Nova);
        if (skillPack.rangeType == RangeType.Grenade) grenadeCircle.SetActive(true);
        if (skillPack.rangeType == RangeType.Fan) ShowFan();
        if (skillPack.rangeType == RangeType.Arc) arcRoot.SetActive(true);
        SetSelectionPosition(_selectionPosition);
    }

    public void CloseRange()
    {
        ApplyTargets(new List<PieceController>());
        _curSkillPack = null;
        circle.SetActive(false);
        //moveIcon.SetActive(false);
        ShowMoveIcon(false);
        attackCircle.SetActive(false);
        attackIcon.SetActive(false);
        skillIcon.SetActive(false);
        skillCircle.SetActive(false);
        grenadeCircle.SetActive(false);
        highlightCircle.SetActive(false);
        fanRoot.SetActive(false);
        arcRoot.SetActive(false);
        ShowSelect(false);
        HideRangePresentation();
    }


    public void ShowMoveIcon(bool option)
    {
        _isShowMoveIcon = option;
        moveIcon.SetActive(option);
        if (destinationMarker != null) destinationMarker.gameObject.SetActive(option);
        //moveStart.SetActive(option);
        //moveLine.gameObject.SetActive(option);
    }

    public void UpdateMove(Vector3 pos)
    {
        moveIcon.transform.position = pos + Vector3.up * SurfaceOffset;
        DrawDestinationMarker(pos);
    }

    /// <summary>将范围绑定到单位，供玩家输入和 AI 共用。</summary>
    public void Bind(PieceController owner)
    {
        _owner = owner;
        isPlayerRange = owner != null && owner.isPlayerPiece;
    }

    public void Update()
    {
        if (BattleScene.Ins != null && BattleScene.Ins.TM != null && BattleScene.Ins.TM.IsPaused) return;
        if (!isPlayerRange || _curSkillPack == null || _owner == null || _owner.IsPerformingAction ||
            (!_owner.IsUsingSkill && !_owner.isUsingOrder) || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            var piece = hit.collider.GetComponentInParent<PieceController>();
            if (piece != null && piece != _owner) { SetSelectionPosition(piece.transform.position); return; }
            if (hit.collider.CompareTag("Ground")) { SetSelectionPosition(hit.point); return; }
        }
        Plane groundPlane = new Plane(Vector3.up, _owner.transform.position);
        if (groundPlane.Raycast(ray, out float enter)) SetSelectionPosition(ray.GetPoint(enter));
    }

    /// <summary>鼠标或 AI 指定同一套世界坐标输入，同时更新形状和目标高亮。</summary>
    public void SetSelectionPosition(Vector3 position)
    {
        if (_owner == null) return;
        Vector3 origin = _owner.transform.position;
        _curRange = _curSkillPack != null ? BattleRangeRules.CastRange(_owner, _curSkillPack, position) : _curRange;
        _selectionPosition = _curSkillPack != null ? BattleRangeRules.ClampPosition(_owner, _curSkillPack, position)
            : SkillTargeting.ClampPosition(origin, position, _curRange);
        Vector3 iconPosition = _selectionPosition + Vector3.up * SurfaceOffset;
        if (attackIcon.activeSelf) attackIcon.transform.position = iconPosition;
        skillIcon.transform.position = iconPosition;
        if (grenadeCircle.activeSelf) grenadeCircle.transform.position = iconPosition;
        Vector3 direction = position - origin;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.0001f) direction = _owner.transform.forward;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (fanRoot.activeSelf) fanRoot.transform.rotation = rotation;
            if (arcRoot.activeSelf) arcRoot.transform.rotation = rotation;
        }
        RefreshTargets();
        DrawRangePresentation();
    }

    /// <summary>显式目标类型入口，例如 SkillTarget.Ally 或 SkillTarget.AllyBody。</summary>
    public void ShowSkillRange(SkillPack skillPack, SkillTarget targetType)
    {
        ShowSkillRange(skillPack?.WithTarget(targetType));
    }

    public void ShowPreview(PieceController owner, SkillPack skill, Vector3 position)
    {
        Bind(owner);
        ShowSkillRange(skill);
        SetSelectionPosition(position);
    }

    private Vector3 _selectionPosition;
    /// <summary>正式选点不包含图形的离地偏移；自身和 Nova 始终使用施法者位置。</summary>
    public Vector3 SelectionPosition => _owner != null && _curSkillPack != null &&
        (_curSkillPack.target == SkillTarget.Self || _curSkillPack.rangeType == RangeType.Nova)
            ? _owner.transform.position : _selectionPosition;

    private void ApplyTargets(List<PieceController> newTargets)
    {
        foreach (var piece in _curTargets)
            if (piece != null && !newTargets.Contains(piece)) piece.ShowHighlight(false);
        foreach (var piece in newTargets)
        {
            piece.ShowHighlight(true);
            if (!_curTargets.Contains(piece)) piece.OnBeTarget(_owner, _curSkillPack);
        }
        _curTargets = newTargets;
    }

    private void OnDisable()
    {
        CloseRange();
    }

    public void ShowHighlight(bool option)
    {
        highlightCircle.SetActive(option);
    }

    public Vector3 GetAtkPos()
    {
        if (attackIcon.activeInHierarchy)
        {
            return attackIcon.transform.position;
        }

        return Vector3.zero;
    }

    public Transform GetSkillTransform()
    {
        if (_curSkillPack != null)
        {
            if (_curSkillPack.target == SkillTarget.Self || _curSkillPack.rangeType == RangeType.Nova)
                return _owner != null ? _owner.transform : transform;
            if (_curSkillPack.rangeType == RangeType.Arc) return skillIcon.transform;
        }
        if (grenadeCircle.activeInHierarchy)
        {
            return grenadeCircle.transform;
        }
        else if (skillIcon.activeInHierarchy)
        {
            return skillIcon.transform;
        }
        else if (fanRoot.activeInHierarchy)
        {
            if (fanPos != null) return fanPos;
        }

        return null;
    }

    public void ShowSelect(bool option)
    {
        selectCircle.SetActive(option);
    }

    private void ShowFan()
    {
        fanRoot.SetActive(true);
    }

    /// <summary>显示警戒指令范围。</summary>
    public void ShowOrderRange(OrderProfile orderProfile)
    {
        CloseRange();
        _curRange = orderProfile.sectorRadius;
        _owner.isUsingOrder = true;
        float r = orderProfile.sectorRadius;
        float angle = orderProfile.sectorAngleDeg;
        ShowFan();
        _curSkillPack = new SkillPack()
        {
            target = SkillTarget.EnemyAll,
            rangeType = RangeType.Fan,
            rangeValue = r,
            rangeAgle = angle,
        };
        SetSelectionPosition(_owner.transform.position + _owner.transform.forward * r);
    }


    private void OnDrawGizmos()
    {
        // 只在编辑器和运行时显示
        if (fanRoot == null || !fanRoot.activeInHierarchy || _curSkillPack == null)
            return;
        if (_curSkillPack.rangeType != RangeType.Fan)
            return;

        // 设置射线颜色
        Gizmos.color = Color.cyan;

        float halfAngle = _curSkillPack.rangeAgle / 2f;
        int rayCount = Mathf.CeilToInt(_curSkillPack.rangeAgle / 5f);
        Vector3 origin = fanRoot.transform.position;
        Vector3 forward = fanRoot.transform.forward;

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = -halfAngle + i * (_curSkillPack.rangeAgle / rayCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 end = origin + dir.normalized * _curSkillPack.rangeValue;

            // 射线可视化
            Gizmos.DrawRay(origin, dir.normalized * _curSkillPack.rangeValue);

            // 可选：命中目标时画球
#if UNITY_EDITOR
            if (Physics.Raycast(origin, dir, out RaycastHit hit, _curSkillPack.rangeValue))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.color = Color.cyan;
            }
#endif
        }
    }
}
