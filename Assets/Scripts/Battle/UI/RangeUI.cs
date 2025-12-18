using System;
using UnityEngine;


public class RangeUI : MonoBehaviour
{
    public GameObject circle;
    public GameObject moveIcon;
    private bool _isShowMoveIcon = false;

    private float circleRadius = 8.39f / 5f;
    private float _curMoveRange;
    public void ShowCircleRange(float radius)
    {
        circle.SetActive(true);
        circle.transform.localScale = radius * circleRadius * Vector3.one;
        _curMoveRange = radius;
    }

    public void CloseRange()
    {
        circle.SetActive(false);
    }
    
    public void ShowMoveIcon(bool option)
    {
        _isShowMoveIcon = option;
        moveIcon.SetActive(option);
    }

    public void Update()
    {
        if (_isShowMoveIcon)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // 假设地面Layer为"Ground"
            int groundLayer = LayerMask.GetMask("Ground");
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                // Vector3 point = hit.point;
                // moveIcon.transform.position = new Vector3(point.x, moveIcon.transform.position.y, point.z);
                
                Vector3 point = hit.point;
                Vector3 centerPos = transform.position;
                Vector3 flatPoint = new Vector3(point.x, centerPos.y, point.z);
                float dist = Vector3.Distance(flatPoint, centerPos);
                Vector3 targetPos;
                if (dist <= _curMoveRange)
                {
                    targetPos = flatPoint;
                }
                else
                {
                    Vector3 dir = (flatPoint - centerPos).normalized;
                    targetPos = centerPos + dir * _curMoveRange;
                }
                moveIcon.transform.position = targetPos;
            }
        }   
    }
}