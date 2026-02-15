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
    public GameObject fanRoot; // 扇形范围根节点
    public Image fanCircle; // 扇形范围圈
    public GameObject fanLine1;
    public GameObject fanLine2;
    private bool _isShowMoveIcon = false;

    private float circleRadius = 1f/11f;
    private float _curRange;

    private List<PieceController> _curTargets = new();
    public List<PieceController> GetCurTargets => _curTargets;
    private SkillPack _curSkillPack;

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
            new Vector3(position.x, position.y +0.1f, position.z); // 只改变x,z轴位置，y轴保持不变
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
            // todo 显示扇形范围
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
            // todo 显示爆炸范围
            skillCircle.SetActive(true);
            skillCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            grenadeCircle.SetActive(true);
            grenadeCircle.transform.localScale =
                skillPack.explodeRadius * circleRadius * Vector3.one;
            _curRange = skillPack.rangeValue;
        }
    }

    public void CloseRange()
    {
        circle.SetActive(false);
        moveIcon.SetActive(false);
        attackCircle.SetActive(false);
        attackIcon.SetActive(false);
        skillIcon.SetActive(false);
        skillCircle.SetActive(false);
        grenadeCircle.SetActive(false);
        highlightCircle.SetActive(false);
        fanRoot.SetActive(false);
        foreach (var piece in _curTargets)
        {
            piece.rangeUI?.ShowHighlight(false);
        }

        _curTargets.Clear();
    }


    public void ShowMoveIcon(bool option)
    {
        _isShowMoveIcon = option;
        moveIcon.SetActive(option);
    }

    public void Update()
    {
        // attackIcon跟随鼠标移动
        if (attackIcon.activeInHierarchy)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0;// 忽略y轴，只在xz平面
                float distance = direction.magnitude;
                if (distance > _curRange)
                {
                    direction = direction.normalized * _curRange;
                }

                attackIcon.transform.position = transform.position + direction + Vector3.up * 0.1f;
            }
        }

        if (skillIcon!=null && skillIcon.activeInHierarchy) // 单体敌人锁定
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 direction = hitPoint - transform.position;
                direction.y = 0;// 忽略y轴，只在xz平面
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
                direction.y = 0;// 忽略y轴，只在xz平面
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
        
        HighlightTarget();
    }

    private void FixedUpdate()
    {
        
    }

    private void HighlightTarget()
    {
        if (skillIcon!=null && skillIcon.activeInHierarchy) // 单体敌人锁定
        {
            // 检测球体范围内的所有敌人
            Collider[] hitColliders = Physics.OverlapSphere(skillIcon.transform.position, 3f);

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
        }
    }

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
                piece.rangeUI?.ShowHighlight(true);
                newTargets.Add(piece);
            }
            else if (_curSkillPack.target == SkillTarget.EnemyAll)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Enemy)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Ally)
            {
                if (piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
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

            piece.rangeUI?.ShowHighlight(false);
        }

        _curTargets = newTargets;
    }

    private void CheckTarget(HashSet<PieceController> hitPieces)
    {
        Debug.Log("Hit Pieces Count: " + hitPieces.Count);
        List<PieceController> newTargets = new();
        foreach (var piece in hitPieces)
        {
            if (_curSkillPack.target == SkillTarget.All)
            {
                piece.rangeUI?.ShowHighlight(true);
                newTargets.Add(piece);
            }
            else if (_curSkillPack.target == SkillTarget.EnemyAll)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Enemy)
            {
                if (!piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
                    newTargets.Add(piece);
                }
            }
            else if (_curSkillPack.target == SkillTarget.Ally)
            {
                if (piece.isPlayerPiece)
                {
                    piece.rangeUI?.ShowHighlight(true);
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

            piece.rangeUI?.ShowHighlight(false);
        }

        _curTargets = newTargets;
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
            return fanRoot.transform;
        }
        return null;
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