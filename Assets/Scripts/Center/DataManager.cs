using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public List<PLAYERPROFILE> playerprofiles = new();
    private string savePath = Application.streamingAssetsPath + "/Datas/";
    public void Init()
    {
        // 测试加载
        PLAYERPROFILE playerprofile = JsonTool.LoadJson<PLAYERPROFILE>(savePath + "PlayerProfiles_0.json");
    }
    [Button("测试保存")]
    public void TestSave(int index)
    {
        JsonTool.SaveJson(GM.Ins.PLAYERPROFILE,savePath + $"PlayerProfiles_{index}.json");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
