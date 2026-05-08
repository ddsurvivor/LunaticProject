using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class RangeUI : MonoBehaviour
{
    public GameObject circle;
    public GameObject moveIcon;
    public GameObject attackCircle;
    public GameObject attackIcon;
    public GameObject skillIcon;
    public GameObject skillCircle;
    public GameObject grenadeCircle; // 爆炸范围圈
    public GameObject highlightCircle;
    public GameObject selectCircle;
    public GameObject fanRoot; // 扇形范围根节点
    public Image fanCircle; // 扇形范围圈
    public GameObject fanLine1;
    public GameObject fanLine2;
    public Transform fanPos;

    [Header("Arc Settings")] [SerializeField]
    private GameObject arcRoot;

    [SerializeField] private Image arcOuter; // 外圆环
    [SerializeField] private Image arcInnerMask; // 内圆覆盖（实现宽度）
    [SerializeField] private RectTransform arcLine1; // 边线1
    [SerializeField] private RectTransform arcLine2; // 边线2
    private float circleRadiusFactor = 1f / 11f; // 基础缩放系数（对应正圆图片的原始尺寸）
    private bool _isShowMoveIcon = false;

    private float circleRadius = 1f / 11f;
    private float _curRange;

    private List<PieceController> _curTargets = new();
    public List<PieceController> GetCurTargets => _curTargets;
    private SkillPack _curSkillPack;

    public bool isPlayerRange = false;

    private PieceController _owner;
    // public void Awake()
    // {
    //     circle.SetActive(false);
    //     moveIcon.SetActive(false);
    //     attackCircle.SetActive(false);
    //     attackIcon.SetActive(false);
    //     if(skillIcon!= null) skillIcon.SetActive(false);
    //     if(skillCircle!=null) skillCircle.SetActive(false);
    //     grenadeCircle.SetActive(false);
    //     highlightCircle.SetActive(false);
    //     fanRoot.SetActive(false);
    //         
    // }
    private void Awake()
    {
        // 从上一级组件获取控制器引用
        if(_owner==null) _owner = GetComponentInParent<PieceController>();
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
        _curSkillPack = skillPack;
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
            // 显示扇形范围
            fanRoot.SetActive(true);
            fanCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            fanCircle.fillAmount = skillPack.rangeAgle / 360f;
            float halfAngle = skillPack.rangeAgle / 2f;
            fanCircle.transform.localRotation = Quaternion.Euler(90, 0
                , 180f - skillPack.rangeAgle + skillPack.rangeAgle + halfAngle);
            fanLine1.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            fanLine2.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            fanLine1.transform.localRotation = Quaternion.Euler(90, 0, halfAngle + 90);
            fanLine2.transform.localRotation = Quaternion.Euler(90, 0, -halfAngle + 90);
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

            float r = Mathf.Sqrt(d * d + (l * l) / 4); // 根据圆心距离和弦长计算半径

            // 调整弧线的尺寸以匹配计算得到的半径r
            arcOuter.transform.localScale = r * circleRadius * Vector3.one;
            //arcInnerMask.transform.localScale = r * circleRadius * Vector3.one;
            arcInnerMask.gameObject.SetActive(false);
            // 调整弧线的位置
            arcOuter.transform.localPosition = new Vector3((-d)*100, 0, l*100 / 2f);
            //arcInnerMask.transform.localPosition = new Vector3((-d + w / 2f)*100, 0, l*100 / 4f);

            // 根据弦长、半径，计算弧线的弧度值
            float angle = 2 * Mathf.Asin(l / (2 * r)) * Mathf.Rad2Deg;
            arcOuter.fillAmount = angle / 360f;
            //arcInnerMask.fillAmount = angle / 360f;
            
            // 修改旋转为0.5倍的angle
            arcOuter.transform.localRotation = Quaternion.Euler(90, 0, 90f + angle / 2f);//
            //arcInnerMask.transform.localRotation = Quaternion.Euler(90, 0, 180f - angle / 2f);

            // --- 第五步：设置两条边线 arcLine1 & arcLine2 ---
            arcLine1.transform.localScale = new Vector3(w, 1, 1);
            arcLine2.transform.localScale = new Vector3(w, 1, 1);
            arcLine1.localPosition = new Vector3(0, 0, 0);
            arcLine2.localPosition = new Vector3(0, 0, l*100);
        }
    }

    public void CloseRange()
    {
        foreach (var piece in _curTargets)
        {
            piece.rangeUI?.ShowHighlight(false);
            if (piece is EnemyController enemy)
            {
                enemy.ShowHighlight(false);
            }
        }
        _curTargets.Clear();
        circle.SetActive(false);
        moveIcon.SetActive(false);
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
    }

    public void Update()
    {
        if(!isPlayerRange) return;
        if(_owner != null && !_owner.IsUsingSkill) return;
        // attackIcon跟随鼠标移动
        if (attackIcon.activeInHierarchy)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 忽略y轴，只在xz平面
                float distance = direction.magnitude;
                if (distance > _curRange)
                {
                    direction = direction.normalized * _curRange;
                }

                attackIcon.transform.position = transform.position + direction + Vector3.up * 0.1f;
            }
        }

        if (skillIcon != null && skillIcon.activeInHierarchy) // 单体敌人锁定
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 忽略y轴，只在xz平面
                float distance = direction.magnitude;
                if (distance > _curRange)
                {
                    direction = direction.normalized * _curRange;
                }

                skillIcon.transform.position = transform.position + direction + Vector3.up * 0.1f;
            }
        }

        if (grenadeCircle.activeInHierarchy) // 爆炸范围锁定
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 忽略y轴，只在xz平面
                float distance = direction.magnitude;
                if (distance > _curRange)
                {
                    direction = direction.normalized * _curRange;
                }

                grenadeCircle.transform.position =
                    transform.position + direction + Vector3.up * 0.1f;
            }
        }

        if (fanRoot.activeInHierarchy) // 扇形范围锁定
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 忽略y轴，只在xz平面
                fanRoot.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        if (arcRoot != null && arcRoot.activeInHierarchy)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0; // 忽略y轴，只在xz平面
                arcRoot.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        HighlightTarget();
    }

    private void FixedUpdate()
    {
    }

    private void HighlightTarget()
    {
        if (skillIcon != null && skillIcon.activeInHierarchy) // 单体敌人锁定
        {
            // 检测球体范围内的所有敌人
            Collider[] hitColliders = Physics.OverlapSphere(skillIcon.transform.position, 1f);

            CheckTarget(hitColliders);
        }
        else if (grenadeCircle.activeInHierarchy) // 爆炸范围锁定
        {
            float explodeRadius = _curSkillPack.explodeRadius;
            // 检测球体范围内的所有敌人
            Collider[] hitColliders =
                Physics.OverlapSphere(grenadeCircle.transform.position, explodeRadius);

            CheckTarget(hitColliders);
        }
        else if (fanRoot.activeInHierarchy)
        {
            // 1. 获取扇形参数
            float halfAngle = _curSkillPack.rangeAgle / 2f;
            float range = _curSkillPack.rangeValue;
            Vector3 origin = fanRoot.transform.position;
            Vector3 forward = fanRoot.transform.forward;

            // 2. 获取范围内所有碰撞体
            Collider[] colliders = Physics.OverlapSphere(origin, range);

            HashSet<PieceController> hitPieces = new HashSet<PieceController>();
            foreach (var collider in colliders)
            {
                PieceController piece = collider.GetComponent<PieceController>();
                if (piece == null) continue;

                // 3. 判断是否在扇形角度范围内
                Vector3 dir = (piece.transform.position - origin);
                dir.y = 0; // 忽略y轴
                if (dir.magnitude > range || dir.magnitude < 1f) continue; // 超出半径

                float angle = Vector3.Angle(forward, dir);
                if (angle <= halfAngle)
                {
                    hitPieces.Add(piece);
                }
            }

            CheckTarget(hitPieces);
        }
        /*else if (fanRoot.activeInHierarchy)
        {
            // 进行扇形有限距离的穿透射线检测
            // 根据扇形角度，等间距的发射多根射线进行检测，结果需要去掉重复
            float halfAngle = _curSkillPack.rangeAgle / 2f;
            int rayCount = Mathf.CeilToInt(_curSkillPack.rangeAgle / 5f); // 每5度发射一根射线
            HashSet<PieceController> hitPieces = new HashSet<PieceController>();
            for (int i = 0; i <= rayCount; i++)
            {
                float angle = -halfAngle + i * (_curSkillPack.rangeAgle / rayCount);
                Vector3 direction = Quaternion.Euler(0, angle, 0) * fanRoot.transform.forward;
                Ray ray = new Ray(fanRoot.transform.position, direction);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, _curSkillPack.rangeValue))
                {
                    PieceController piece = hitInfo.collider.GetComponent<PieceController>();
                    if (piece != null)
                    {
                        hitPieces.Add(piece);
                    }
                }
            }

            CheckTarget(hitPieces);
        }*/
        else if (arcRoot.activeInHierarchy)
        {
            float w = _curSkillPack.arcWeight; // 技能宽度
            float d = _curSkillPack.arcCenterDis; // 圆心距离
            float l = _curSkillPack.rangeValue; // 弦长

            float r = Mathf.Sqrt(d * d + (l * l) / 4); // 根据圆心距离和弦长计算半径
            float innerR = r - w / 2f; // 圆环内半径
            float outerR = r + w / 2f; // 圆环外半径
            HashSet<PieceController> hitPieces = new HashSet<PieceController>();

            // 计算圆心角 (弧度转角度)
            float halfAngleDeg = 2 * Mathf.Asin(l / (2 * r)) * Mathf.Rad2Deg;
            
            Vector3 centerPos = arcOuter.transform.position;
            // --- 2. 物理粗筛 (Broad-phase) ---
            // 以 arcRoot 为圆心，外圆半径为范围，找出所有潜在碰撞体
            int count = Physics.OverlapSphereNonAlloc(
                centerPos, 
                outerR, 
                _overlapResults
            );

            Vector3 forward = arcRoot.transform.forward;

            // --- 3. 几何精筛 (Narrow-phase) ---
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapResults[i];
                // 通过所有检查，记录目标
                PieceController piece = col.GetComponent<PieceController>();
                if (piece == null) continue;
                
                Vector3 targetPos = col.transform.position;
                Vector3 dirToTarget = targetPos - centerPos;
        
                // A. 距离过滤 (是否在圆环带厚度内)
                // 使用 sqrMagnitude (平方和) 避免开方运算，提升性能
                float distSq = dirToTarget.sqrMagnitude;
                if (distSq < innerR * innerR || distSq > outerR * outerR)
                    continue;

                // B. 角度过滤 (是否在圆弧开口内)
                float angleToTarget = Vector3.Angle(forward, dirToTarget);
                if (angleToTarget > halfAngleDeg)
                    continue;

                hitPieces.Add(piece);
            }

            CheckTarget(hitPieces);
            
        }
    }
    private Collider[] _overlapResults = new Collider[20]; // 预分配数组提升性能

    private void CheckTarget(Collider[] hitColliders)
    {
        List<PieceController> newTargets = new();
        if (hitColliders.Length <= 0) return;
        foreach (var collider in hitColliders)
        {
            PieceController piece = collider.transform.GetComponent<PieceController>();
            if (piece == null) continue;
            if (_curSkillPack.target == SkillTarget.All)
            {
                piece.ShowHighlight(true);
                newTargets.Add(piece);
            }
            else if (_curSkillPack.target == SkillTarget.EnemyAll)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Enemy)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Ally)
            {
                if (piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
        }

        foreach (var piece in _curTargets)
        {
            if (newTargets.Contains(piece))
            {
                continue;
            }

            piece.ShowHighlight(false);
            piece.hitInfoPanel?.gameObject.SetActive(false);
        }

        _curTargets = newTargets;
        foreach (var target in _curTargets)
        {
            // 显示命中率、伤害等
            target.hitInfoPanel?.UpdateDisplay(_owner, _curSkillPack, target);
        }
    }

    private void CheckTarget(HashSet<PieceController> hitPieces)
    {
        Debug.Log("Hit Pieces Count: " + hitPieces.Count);
        List<PieceController> newTargets = new();
        foreach (var piece in hitPieces)
        {
            if (_curSkillPack.target == SkillTarget.All)
            {
                piece.ShowHighlight(true);
                newTargets.Add(piece);
            }
            else if (_curSkillPack.target == SkillTarget.EnemyAll)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Enemy)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Ally)
            {
                if (piece.isPlayerPiece)
                {
                    piece.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
        }

        foreach (var piece in _curTargets)
        {
            if (newTargets.Contains(piece))
            {
                continue;
            }

            piece.ShowHighlight(false);
            piece.hitInfoPanel?.gameObject.SetActive(false);
        }

        _curTargets = newTargets;
        foreach (var target in _curTargets)
        {
            // 显示命中率、伤害等
            target.hitInfoPanel?.UpdateDisplay(_owner, _curSkillPack, target);
        }
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
            if(fanPos!=null) return fanPos;
        }

        return null;
    }
    
    public void ShowSelect(bool option)
    {
        selectCircle.SetActive(option);
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