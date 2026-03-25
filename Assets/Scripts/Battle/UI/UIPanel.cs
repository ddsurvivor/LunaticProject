using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ui页面基类
/// </summary>
public class UIPanel : MonoBehaviour
{
    [SerializeField]
    private bool hasInited;
    
    private void OnEnable()
    {
        if (!hasInited)
        {
            Init();
            hasInited = true;
            UpdateDisplay();
        }
        else
        {
            UpdateDisplay();
        }
    }

    public virtual void Init()
    {

    }

    public virtual void UpdateDisplay()
    {

    }

    public virtual void ShowPanel()
    {
        gameObject.SetActive(true);
    }
    public virtual void ClosePanel()
    {
        gameObject.SetActive(false);
    }
    
    
}
