using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class DataManager : SerializedMonoBehaviour
{
    [OdinSerialize]
    public Dictionary<int, PLAYERPROFILE> playerprofiles = new();
    private string savePath = Application.streamingAssetsPath + "/Datas/";
    private int saveSlotCount = 10;
    public void Init()
    {
        // 测试加载
        //PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + "PlayerProfiles_0.json");
        LoadData();
    }

    public void LoadData()
    {
        playerprofiles.Clear();
        for (int i = 0; i < saveSlotCount; i++)
        {
            
            int j = i;
            PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + $"PlayerProfiles_{j}.json");
            if(playerprofile == null) continue;
            playerprofiles.Add(j,playerprofile);
        }
    }

    public void SaveData(int index)
    {
        JsonTool.SaveJson(GM.Ins.PLAYERPROFILE,savePath + $"PlayerProfiles_{index}.json");
    }
    [Button("测试保存")]
    public void TestSave(int index)
    {
        JsonTool.SaveJson(GM.Ins.PLAYERPROFILE,savePath + $"PlayerProfiles_{index}.json");
    }
}
