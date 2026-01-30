using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.UI;

public class DaytimeSystem : SerializedMonoBehaviour
{
    private int startYear = 1567;
    private int startMonth = 1;
    private int startDay = 45;

    
    public Text dateText;
    public Image dateImage;
    public Dictionary<Daytime, Sprite> daytimeSprites = new Dictionary<Daytime, Sprite>();
    public Sprite specialMapSprite;

    private void Start()
    {
        UpdateDaytimeImage();
    }
    // 推进一个时间点
    public void CostDaytime(int cost = 1)
    {
        if (cost == 3)// 特殊差分
        {
            dateImage.sprite = specialMapSprite;
            return;
        }
        // 日期向前推进
        for (int i = 0; i < cost; i++)
        {
            if (GM.Ins.PLAYERPROFILE.daytime == Daytime.夜晚)
            {
                GM.Ins.PLAYERPROFILE.date += 1;
                GM.Ins.PLAYERPROFILE.daytime = Daytime.上午;
            }
            else
            {
                GM.Ins.PLAYERPROFILE.daytime += 1;
            }
        }
        UpdateDaytimeImage();
    }
    
    public void UpdateDaytimeImage()
    {
        // 更新日期文本
        dateText.text = $"{startYear}  /  {startMonth}  /  {startDay + GM.Ins.PLAYERPROFILE.date} 【{GM.Ins.PLAYERPROFILE.daytime}】";
        if (daytimeSprites.ContainsKey(GM.Ins.PLAYERPROFILE.daytime))
        {
            dateImage.sprite = daytimeSprites[GM.Ins.PLAYERPROFILE.daytime];
        }
    }
}
