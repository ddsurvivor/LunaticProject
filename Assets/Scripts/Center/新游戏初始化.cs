using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class 新游戏初始化 : MonoBehaviour
{
    public void Awake()
    {
        Debug.Log("测试中,调用初始化方法");
        Init();
    }

    private void Init()
    {
        PLAYERPROFILE.player[0] = new PLAYERPROFILE.Player();
        PLAYERPROFILE.player[0] .NAME = "qiuwu";
        PLAYERPROFILE.player[0] .HP = 6;
        PLAYERPROFILE.player[0] .PHYSIQUE = 4;
        PLAYERPROFILE.player[0] .TACTICS = 5;
        PLAYERPROFILE.player[0] .YIZHI = 3;
    }
}
