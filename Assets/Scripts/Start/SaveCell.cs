using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveCell : SavePanel
{
    public Text timeText;
    public int index;

    public void SetData(string time)
    {
        timeText.text = time;
    }
    public void OnClickLoad()
    {
        if (GM.Ins.DM.playerprofiles.ContainsKey(index))
        {
            // 加载当前存档到玩家数据
            GM.Ins.PLAYERPROFILE = GM.Ins.DM.playerprofiles[index];
            GM.Ins.LoadPlayingScene();
        }
    }

    public void OnClickSave()
    {
        GM.Ins.PLAYERPROFILE.lastSaveTime = System.DateTime.Now;
        // 保存当前玩家数据到当前存档
        if (GM.Ins.DM.playerprofiles.ContainsKey(index))
        {
            GM.Ins.DM.playerprofiles[index] = GM.Ins.PLAYERPROFILE;
        }
        else
        {
            GM.Ins.DM.playerprofiles.Add(index, GM.Ins.PLAYERPROFILE);
        }
        // 同步保存到磁盘
        GM.Ins.DM.SaveData(index);
    }
}
