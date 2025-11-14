using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Center : MonoBehaviour
{
    
    public static Center instance;
    public Font[] Fonts;
    
    public static string Language = "CN";

    public static int Languageint
    {
        get {
            switch (Language)
            {case "CN":
                return 0;
            case "EN":
                return 1;
            case "JP":
                return 2;
            }

            return 1;
        }
    }

    #region Plots

    public static string Command_background="CG";//CG修改
    public static string Command_SpeakerSet="SPEAK";//说话人修改
    public static string Command_Check="CHECK";//鉴定
    public static string Command_Set="SET";//修改变量
    public static string Command_Choice="ISCHOICE";//设置分支
    public static string Command_Debug="DEBUG";//输出debug信息
    public static string Command_Setspace="SETSPACE";//设置空格
    public static string Command_Next="NEXT";//下一段
    public static string Command_If="IF";//检查分支
    public static string Command_End = "END";//结束
    public static string Command_Skip="SKIP";//跳过本行
    public static string Command_Jump="JUMPTO";//跳转到行
    public static string Command_Gameover="GAMEOVER";//失败
    public static string Command_Refresh="REFRESH";
    public static string Command_Clear="CLEAR";
    public static string Command_Sound = "SOUND";
    public static string Command_Music = "MUSIC";
    public static string Command_Modify="MODIFY";
    public static string Command_Shake="SHAKE";//震动
    public static string Tag_checkview = "!CHECKVIEW";
    public static string Tag_notspawn = "!DONTSPAWN";
 
    public static readonly char Plot指令分隔符 = '+';

    #endregion

    private void Awake()
    {
        instance = this;
    }

    public Font GetFont()
    {
        return Fonts[Languageint];
    }
}