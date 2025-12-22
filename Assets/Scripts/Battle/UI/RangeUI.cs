using System;
using UnityEngine;


public class RangeUI : MonoBehaviour
{
    public GameObject circle;
    public GameObject moveIcon;
    public GameObject attackCircle;
    public GameObject attackIcon;
    private bool _isShowMoveIcon = false;

    private float circleRadius = 8.39f / 5f;
    private float _curRange;
    public void ShowCircleRange(float radius)
    {
        circle.SetActive(true);
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
    
    public void ShowSkillRange(float radius)
    {
        
    }

    public void CloseRange()
    {
        circle.SetActive(false);
        moveIcon.SetActive(false);
        attackCircle.SetActive(false);
        attackIcon.SetActive(false);
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