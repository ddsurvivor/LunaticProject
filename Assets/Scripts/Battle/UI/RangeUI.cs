using System.Collections.Generic;
using UnityEngine;


public class RangeUI : MonoBehaviour
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
    private float movelineLengthFactor = 4f;
    private float lineOffset = 1.8f;
    private float _curRange;

    private List<PieceController> _curTargets = new();
    public List<PieceController> GetCurTargets => _curTargets;
    private SkillPack _curSkillPack;

    public bool isPlayerRange = false;

    private PieceController _owner;

    private void Awake()
    {
        // 从上一级组件获取控制器引用
        if (_owner == null) _owner = GetComponentInParent<PieceController>();
    }

    public void ShowCircleRange(float radius)
    {
        circle.SetActive(true);
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
    }

    public void ShowCircleRange(Vector3 position, float radius)
    {
        circle.SetActive(true);
        transform.position =
            new Vector3(position.x, position.y + 0.1f, position.z); // 只改变x,z轴位置，y轴保持不变
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
    }

    public void ShowAttackRange(float radius)
    {
        attackCircle.SetActive(true);
        attackCircle.transform.localScale = radius * circleRadius * Vector3.one;
        attackIcon.SetActive(true);
        _curRange = radius;
    }

    public void ShowSkillRange(SkillPack skillPack)
    {
        CloseRange();
        if (skillPack == null) return;
        _curSkillPack = skillPack;
        _curRange = skillPack.rangeValue;
        _selectionPosition = _owner != null ? _owner.transform.position : transform.position;
        if (skillPack.rangeType == RangeType.Circle)
        {
            // 显示圆形范围
            skillCircle.SetActive(true);
            skillCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            skillIcon.SetActive(true);
            _curRange = skillPack.rangeValue;
        }
        else if (skillPack.rangeType == RangeType.Fan)
        {
            ShowFan(skillPack.rangeValue, skillPack.rangeAgle);
        }
        else if (skillPack.rangeType == RangeType.Grenade)
        {
            // 显示爆炸范围
            skillCircle.SetActive(true);
            skillCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            grenadeCircle.SetActive(true);
            grenadeCircle.transform.localScale =
                skillPack.explodeRadius * circleRadius * Vector3.one;
            _curRange = skillPack.rangeValue;
        }
        else if (skillPack.rangeType == RangeType.Nova)
        {
            // Nova类型：仅指定一个圆形范围，以自身为中心
            // 这里我们通常使用 explodeRadius 或 rangeValue 作为爆炸半径
            skillCircle.SetActive(true);
            float radius = skillPack.explodeRadius > 0
                ? skillPack.explodeRadius
                : skillPack.rangeValue;
            skillCircle.transform.localScale = radius * circleRadius * Vector3.one;

            // Nova 通常不需要显示准星图标，因为它不可选地
            skillIcon.SetActive(false);
            _curRange = radius;
        }
        else if (skillPack.rangeType == RangeType.Arc)
        {
            skillCircle.SetActive(true);
            skillCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;

            arcRoot.SetActive(true);

            float w = skillPack.arcWeight; // 技能宽度
            float d = skillPack.arcCenterDis; // 圆心距离
            float l = skillPack.rangeValue; // 弦长

            float r = SkillTargeting.GetArcRadius(skillPack); // 根据圆心距离和弦长计算半径

            // 调整弧线的尺寸以匹配计算得到的半径r
            arcOuter.transform.localScale = r * circleRadius * Vector3.one;
            //arcInnerMask.transform.localScale = r * circleRadius * Vector3.one;
            arcInnerMask.gameObject.SetActive(false);
            // 调整弧线的位置
            arcOuter.transform.localPosition = new Vector3((-d) * 100, 0, l * 100 / 2f);
            //arcInnerMask.transform.localPosition = new Vector3((-d + w / 2f)*100, 0, l*100 / 4f);

            // 根据弦长、半径，计算弧线的弧度值
            float angle = 2 * SkillTargeting.GetArcHalfAngle(skillPack);
            arcOuter.fillAmount = angle / 360f;
            //arcInnerMask.fillAmount = angle / 360f;

            // 修改旋转为0.5倍的angle
            arcOuter.transform.localRotation = Quaternion.Euler(90, 0, 90f + angle / 2f); //
            //arcInnerMask.transform.localRotation = Quaternion.Euler(90, 0, 180f - angle / 2f);

            // --- 第五步：设置两条边线 arcLine1 & arcLine2 ---
            arcLine1.transform.localScale = new Vector3(w, 1, 1);
            arcLine2.transform.localScale = new Vector3(w, 1, 1);
            arcLine1.localPosition = new Vector3(0, 0, 0);
            arcLine2.localPosition = new Vector3(0, 0, l * 100);
        }
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
    }


    public void ShowMoveIcon(bool option)
    {
        _isShowMoveIcon = option;
        moveIcon.SetActive(option);
        //moveStart.SetActive(option);
        //moveLine.gameObject.SetActive(option);
    }

    public void UpdateMove(Vector3 pos)
    {
        moveIcon.transform.position =
            (new Vector3(pos.x, moveIcon.transform.position.y, pos.z)); //保持y轴不变
        moveIcon.transform.position =
            new Vector3(pos.x, moveIcon.transform.position.y, pos.z); // 保持y轴不变

        // 标志旋转指向 move icon
        Vector3 dir = moveIcon.transform.position - moveStart.transform.position;
        dir.y = 0; // 忽略y轴

        if (dir != Vector3.zero)
        {
            // 1. 使用你之前注释掉的这行，计算 XZ 平面上的夹角
            float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;

            // 2. 完美的解法：因为你的图片是 X 轴拉伸，直接让 transform.right 指向目标
            // Unity 会自动处理好平躺的姿态，这是最不易出错的方法
            //moveStart.transform.right = dir;
            //moveLine.transform.right = dir;

            // 备用显式 Euler 解法：如果用 transform.right 发现图片翻转了，
            // 可以解开下方注释，假设图片原本绕 X 轴旋转了 90 度躺在地上，则直接控制它的本地 Z 轴：
            moveStart.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            //moveLine.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            
        }

        // move line 缩放 width
        float distance = (dir.magnitude - lineOffset) * movelineLengthFactor;
        //moveLine.size = new Vector2(distance, moveLine.size.y);
        // move line 缩放 width，长度根据 move start 和 move icon 之间的距离调整
    }

    /// <summary>将范围绑定到单位，供玩家输入和 AI 共用。</summary>
    public void Bind(PieceController owner)
    {
        _owner = owner;
        isPlayerRange = owner != null && owner.isPlayerPiece;
    }

    public void Update()
    {
        if (!isPlayerRange || _owner == null || _owner.IsPerformingAction ||
            (!_owner.IsUsingSkill && !_owner.isUsingOrder) || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        if (groundPlane.Raycast(ray, out float enter)) SetSelectionPosition(ray.GetPoint(enter));
    }

    /// <summary>鼠标或 AI 指定同一套世界坐标输入，同时更新形状和目标高亮。</summary>
    public void SetSelectionPosition(Vector3 position)
    {
        if (_owner == null) return;
        Vector3 origin = _owner.transform.position;
        _selectionPosition = SkillTargeting.ClampPosition(origin, position, _curRange);
        Vector3 iconPosition = _selectionPosition + Vector3.up * 0.1f;
        if (attackIcon.activeSelf) attackIcon.transform.position = iconPosition;
        skillIcon.transform.position = iconPosition;
        if (grenadeCircle.activeSelf) grenadeCircle.transform.position = iconPosition;
        Vector3 direction = position - origin;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            if (fanRoot.activeSelf) fanRoot.transform.rotation = rotation;
            if (arcRoot.activeSelf) arcRoot.transform.rotation = rotation;
        }
        if (_curSkillPack != null)
            ApplyTargets(SkillTargeting.Query(_owner, _curSkillPack, _selectionPosition));
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
    public Vector3 SelectionPosition => _selectionPosition;

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
        ApplyTargets(new List<PieceController>());
        _curSkillPack = null;
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

    private void ShowFan(float radius, float angle)
    {
        fanRoot.SetActive(true);
        fanCircle.transform.localScale = radius * circleRadius * Vector3.one;
        fanCircle.fillAmount = angle / 360f;
        float halfAngle = angle / 2f;
        fanCircle.transform.localRotation = Quaternion.Euler(90, 0, 180f + halfAngle);
        fanLine1.transform.localScale = radius * circleRadius * Vector3.one;
        fanLine2.transform.localScale = radius * circleRadius * Vector3.one;
        fanLine1.transform.localRotation = Quaternion.Euler(90, 0, halfAngle + 90);
        fanLine2.transform.localRotation = Quaternion.Euler(90, 0, -halfAngle + 90);
    }

    /// <summary>显示警戒指令范围。</summary>
    public void ShowOrderRange(OrderProfile orderProfile)
    {
        CloseRange();
        _curRange = orderProfile.sectorRadius;
        _owner.isUsingOrder = true;
        float r = orderProfile.sectorRadius;
        float angle = orderProfile.sectorAngleDeg;
        ShowFan(r, angle);
        _curSkillPack = new SkillPack()
        {
            target = SkillTarget.EnemyAll,
            rangeType = RangeType.Fan,
            rangeValue = r,
            rangeAgle = angle,
        };
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
