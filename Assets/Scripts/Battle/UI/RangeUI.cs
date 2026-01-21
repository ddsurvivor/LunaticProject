using System;
using System.Collections.Generic;
using UnityEngine;


public class RangeUI : MonoBehaviour
{
    public GameObject circle;
    public GameObject moveIcon;
    public GameObject attackCircle;
    public GameObject attackIcon;
    public GameObject skillIcon;
    public GameObject skillCircle;
    public GameObject grenadeCircle;// 爆炸范围圈
    public GameObject highlightCircle;
    private bool _isShowMoveIcon = false;

    private float circleRadius = 8.39f / 5f;
    private float _curRange;

    private List<PieceController> _curTargets = new ();
    public List<PieceController> GetCurTargets => _curTargets;
    private SkillPack _curSkillPack;
    public void ShowCircleRange(float radius)
    {
        circle.SetActive(true);
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curRange = radius;
    }
    public void ShowCircleRange(Vector3 position, float radius)
    {
        circle.SetActive(true);
        transform.position = position;
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
        }
        else if (skillPack.rangeType == RangeType.Grenade)
        {
            // todo 显示爆炸范围
            skillCircle.SetActive(true);
            skillCircle.transform.localScale = skillPack.rangeValue * circleRadius * Vector3.one;
            grenadeCircle.SetActive(true);
            grenadeCircle.transform.localScale = skillPack.explodeRadius * circleRadius * Vector3.one;
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
        foreach (var piece in _curTargets)
        {
            piece.rangeUI.ShowHighlight(false);
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
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 direction = hitPoint - transform.position;
                    float distance = direction.magnitude;
                    if (distance > _curRange)
                    {
                        direction = direction.normalized * _curRange;
                    }
                    attackIcon.transform.position = transform.position + direction + Vector3.up * 0.1f;
                }
            }

            if (skillIcon.activeInHierarchy)// 单体敌人锁定
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 direction = hitPoint - transform.position;
                    float distance = direction.magnitude;
                    if (distance > _curRange)
                    {
                        direction = direction.normalized * _curRange;
                    }
                    skillIcon.transform.position = transform.position + direction + Vector3.up * 0.1f;
                }
            }

            if (grenadeCircle.activeInHierarchy)// 爆炸范围锁定
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 direction = hitPoint - transform.position;
                    float distance = direction.magnitude;
                    if (distance > _curRange)
                    {
                        direction = direction.normalized * _curRange;
                    }
                    grenadeCircle.transform.position = transform.position + direction + Vector3.up * 0.1f;
                }
            }
    }

    private void FixedUpdate()
    {
        HighlightTarget();
    }

    private void HighlightTarget()
    {
        List<PieceController> newTargets = new();
        if (skillIcon.activeInHierarchy) // 单体敌人锁定
        {
            // 检测球体范围内的所有敌人
            Collider[] hitColliders = Physics.OverlapSphere(skillIcon.transform.position, 3f);
        
            if (hitColliders.Length <= 0) return;
            foreach (var collider in hitColliders)
            {
                PieceController piece = collider.transform.GetComponent<PieceController>();
                if (piece == null) continue;
                if (_curSkillPack.target == SkillTarget.All)
                {
                    piece.rangeUI.ShowHighlight(true);
                    newTargets.Add(piece);
                }
                else if (_curSkillPack.target == SkillTarget.EnemyAll)
                {
                    if (!piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                    }
                }
                else if (_curSkillPack.target == SkillTarget.Enemy)
                {
                    if (!piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                        return;
                    }
                }
                else if (_curSkillPack.target == SkillTarget.Ally)
                {
                    if (piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                        return;
                    }
                }
            }
        }
        else if (grenadeCircle.activeInHierarchy) // 爆炸范围锁定
        {
            float explodeRadius = _curSkillPack.explodeRadius;
            // 检测球体范围内的所有敌人
            Collider[] hitColliders = Physics.OverlapSphere(grenadeCircle.transform.position, explodeRadius);
        
            if (hitColliders.Length <= 0) return;
            foreach (var collider in hitColliders)
            {
                PieceController piece = collider.transform.GetComponent<PieceController>();
                if (piece == null) continue;
                if (_curSkillPack.target == SkillTarget.All)
                {
                    piece.rangeUI.ShowHighlight(true);
                    newTargets.Add(piece);
                }
                else if (_curSkillPack.target == SkillTarget.EnemyAll)
                {
                    if (!piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                    }
                }
                else if (_curSkillPack.target == SkillTarget.Enemy)
                {
                    if (!piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                        return;
                    }
                }
                else if (_curSkillPack.target == SkillTarget.Ally)
                {
                    if (piece.isPlayerPiece)
                    {
                        piece.rangeUI.ShowHighlight(true);
                        newTargets.Add(piece);
                        return;
                    }
                }
            }
        }
        foreach (var piece in _curTargets)
        {
            if (newTargets.Contains(piece))
            {
                continue;
            }
            piece.rangeUI.ShowHighlight(false);
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
}