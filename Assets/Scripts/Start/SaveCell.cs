using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveCell : SavePanel
{
    public Text timeText;
    public Text chapterText;
    public Text storyIdText;
    public int index;

    public void SetData(PLAYERPROFILE profile)
    {
        timeText.text = profile == null ? "空存档" : profile.lastSaveTime.ToString("yyyy-MM-dd HH:mm:ss");

        if (chapterText != null)
        {
            if (profile == null)
                chapterText.text = "";
            else if (string.IsNullOrWhiteSpace(profile.chapterTitle))
                chapterText.text = "章节未知";
            else
                chapterText.text = profile.chapterTitle;
                /*chapterText.text = profile.chapterNumber > 0
                    ? $"第{profile.chapterNumber}章  {profile.chapterTitle}"
                    : profile.chapterTitle;*/
        }

        if (storyIdText != null)
            storyIdText.text = profile == null || string.IsNullOrWhiteSpace(profile.currentStoryId)
                ? ""
                : $"{profile.currentStoryId}";
    }
    public void OnClickLoad()
    {
        // if (GM.Ins.DM.playerprofiles.ContainsKey(index))
        // {
        //     // 加载当前存档到玩家数据
        //     GM.Ins.PLAYERPROFILE = GM.Ins.DM.playerprofiles[index];
        //     GM.Ins.LoadPlayingScene();
        // }
        PLAYERPROFILE playerprofile = GM.Ins.DM.LoadData(index);
        if(playerprofile == null)
        {
            Debug.Log("存档不存在");
            return;
        }

        GM.Ins.PLAYERPROFILE = playerprofile;
        GM.Ins.LoadPlayingScene(playerprofile.currentScene);
    }

    public void OnClickSave()
    {
        // // 保存当前玩家数据到当前存档
        // if (GM.Ins.DM.playerprofiles.ContainsKey(index))
        // {
        //     GM.Ins.DM.playerprofiles[index] = GM.Ins.PLAYERPROFILE;
        // }
        // else
        // {
        //     GM.Ins.DM.playerprofiles.Add(index, GM.Ins.PLAYERPROFILE);
        // }
        // 同步保存到磁盘
        GM.Ins.DM.SaveData(index);
        SetData(GM.Ins.DM.playerprofiles[index]);
    }
}
