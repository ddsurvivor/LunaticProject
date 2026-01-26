using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class DaytimeSystem : SerializedMonoBehaviour
{
    public int date = 1;// 当前日期
    public enum Daytime
    {
        早晨 = 0 ,
        中午 = 1,
        夜晚 = 2,
    }

    public Daytime daytime = Daytime.早晨;
    public Text dateText;
    public Image dateImage;
    public Dictionary<Daytime, Sprite> daytimeSprites = new Dictionary<Daytime, Sprite>();

    private void Start()
    {
        UpdateDaytimeImage();
    }
    // 推进一个时间点
    public void CostDaytime(int cost = 1)
    {
        // 日期向前推进
        for (int i = 0; i < cost; i++)
        {
            if (daytime == Daytime.夜晚)
            {
                date += 1;
                daytime = Daytime.早晨;
            }
            else
            {
                daytime += 1;
            }
        }
        UpdateDaytimeImage();
    }
    
    public void UpdateDaytimeImage()
    {
        dateText.text = "第 " + date + " 天 " + daytime.ToString();
        if (daytimeSprites.ContainsKey(daytime))
        {
            dateImage.sprite = daytimeSprites[daytime];
        }
    }
}
