using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class StartButton : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    // 开始界面按钮
    // 当鼠标移入时，字体颜色变成黑色，当鼠标移出时，字体颜色变成白色。鼠标移入时激活一个高亮gameobject，鼠标移出时取消激活。
    // 暴露开放的鼠标点击抬起事件，提供面板配置
    [Header("按钮文本")]
    public Text buttonText;

    [Header("高亮对象")]
    public GameObject highlightObject;

    [Header("点击抬起事件")]
    public UnityEvent onPointerUp;

    private void Reset()
    {
        // 自动查找Text和高亮对象（可选）
        if (buttonText == null)
            buttonText = GetComponentInChildren<Text>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = Color.black;
        if (highlightObject != null)
            highlightObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = Color.white;
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //Debug.Log("StartButton clicked!");
        onPointerUp?.Invoke();
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("StartButton down!");
    }
}
