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
    public Text slotText;
    public GameObject loadButton;

    public void SetData(PLAYERPROFILE profile)
    {
        if (slotText != null)
        {
            slotText.text = (index + 1).ToString("D2");
            slotText.color = profile == null
                ? new Color(0.45f, 0.45f, 0.42f)
                : new Color(0.937255f, 0.913725f, 0.819608f);
        }
        if (loadButton != null)
            loadButton.SetActive(profile != null);

        timeText.text = profile == null ? "空存档" : profile.lastSaveTime.ToString("yyyy-MM-dd HH:mm:ss");

        if (chapterText != null)
            chapterText.text = profile == null ? "" : profile.GetSaveChapterTitle();

        if (slotText != null)
        {
            // The new artwork places the empty-slot label on the title line.
            timeText.text = profile == null ? "" : timeText.text;
            if (chapterText != null)
            {
                chapterText.text = profile == null ? "空存档位" : chapterText.text;
                chapterText.color = slotText.color;
            }
        }

        if (storyIdText != null)
            storyIdText.text = profile == null || string.IsNullOrWhiteSpace(profile.currentStoryId)
                ? ""
                : $"{profile.currentStoryId}";

        if (slotText != null && chapterText != null && storyIdText != null && profile != null)
        {
            if (!string.IsNullOrWhiteSpace(storyIdText.text))
                chapterText.text += "　" + storyIdText.text;
            storyIdText.text = "";
        }
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

        if (!GM.Ins.CanLoadScene(string.IsNullOrWhiteSpace(playerprofile.currentScene) ? "Playing" : playerprofile.currentScene)) return;
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
