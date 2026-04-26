using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class DaytimeSystem : SerializedMonoBehaviour
{
    
    
    public Text dateText;
    public Text timeText;
    public Image dateImage;
    public Dictionary<Daytime, GameObject> daytimeSprites = new ();

    private void Start()
    {
        UpdateDaytimeImage();
    }
    // 推进一个时间点
    public void CostDaytime(int cost = 1)
    {
        if (cost == 3)// 特殊差分
        {
            GM.Ins.PLAYERPROFILE.daytime = Daytime.轰炸;
            CloseAllSprite();
            daytimeSprites[GM.Ins.PLAYERPROFILE.daytime].SetActive(true);
            //dateImage.sprite = daytimeSprites[GM.Ins.PLAYERPROFILE.daytime];
            return;
        }
        // 日期向前推进
        for (int i = 0; i < cost; i++)
        {
            if (GM.Ins.PLAYERPROFILE.daytime == Daytime.夜晚)
            {
                GM.Ins.PLAYERPROFILE.dateDay += 1;
                GM.Ins.PLAYERPROFILE.daytime = Daytime.上午;
            }
            else
            {
                GM.Ins.PLAYERPROFILE.daytime += 1;
            }
        }
        UpdateDaytimeImage();
    }

    public void NextDay()
    {
        GM.Ins.PLAYERPROFILE.dateDay += 1;
        GM.Ins.PLAYERPROFILE.daytime = Daytime.上午;
        UpdateDaytimeImage();
    }
    
    public void UpdateDaytimeImage()
    {
        // 更新日期文本
        dateText.text = $"{GM.Ins.PLAYERPROFILE.dateYear} / Q{GM.Ins.PLAYERPROFILE.dateMonth} / {GM.Ins.PLAYERPROFILE.dateDay}";
        timeText.text = GM.Ins.PLAYERPROFILE.daytime == Daytime.轰炸 ? 
            "夜晚" : GM.Ins.PLAYERPROFILE.daytime.ToString();
        if (daytimeSprites.ContainsKey(GM.Ins.PLAYERPROFILE.daytime))
        {
            CloseAllSprite();
            daytimeSprites[GM.Ins.PLAYERPROFILE.daytime].SetActive(true);
        }
    }
    
    private void CloseAllSprite()
    {
        foreach (var sprite in daytimeSprites.Values)
        {
            sprite.SetActive(false);
        }
    }
}
