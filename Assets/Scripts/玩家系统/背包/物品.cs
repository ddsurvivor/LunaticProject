using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 暂时弃用
/// </summary>
public class 物品 : MonoBehaviour
{
    public string Name;//技术名词
    public string[] Title;//各语言显示名称
    public string[] Info;//各语言介绍
    public int Time;//回合数
    public string[] Check;//可使用阶段

    private void Awake()
    {
        string title = Title[Center.Languageint];
        string info = Info[Center.Languageint];
        
    }
}
