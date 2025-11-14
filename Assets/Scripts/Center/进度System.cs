using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class 进度System : MonoBehaviour
{
  private const string 位置前缀 = "SaveFile";
  [ContextMenu("存档")]
  public void 存档(string 位置)
  {
    位置 = 位置前缀 + 位置;
    ES3.Save<Dictionary<string, int>>("EventFinish",PLAYERPROFILE.instance.已完成任务,位置);
    ES3.Save("Player",PLAYERPROFILE.player,位置);
    ES3.Save("MySkill",剧本技能.当前技能,位置);
    ES3.Save("Bag", 背包系统.当前背包,位置);
  }
  [ContextMenu("读档")]
  public void 读档(string 位置)
  {
    位置 = 位置前缀 + 位置;
    PLAYERPROFILE.instance.已完成任务 = ES3.Load<Dictionary<string, int>>("EventFinish",位置);
    PLAYERPROFILE.player=ES3.Load<PLAYERPROFILE.Player[]>("Player",位置);
    剧本技能.当前技能=ES3.Load<Dictionary<string, int>>("EventFinish",位置);
    背包系统.当前背包=ES3.Load<Dictionary<string, int>>("Bag",位置);
    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
  }
}
