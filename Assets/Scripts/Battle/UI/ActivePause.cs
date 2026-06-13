using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivePause : MonoBehaviour
{
    // 当这个物体被激活时，就会暂停游戏
    private void OnEnable()
    {
        //Time.timeScale = 0f; // 暂停游戏
        BattleScene.Ins.TM.PauseTime();
    }
    
    // 当这个物体被禁用时，恢复游戏
    private void OnDisable()
    {
        //Time.timeScale = 1f; // 恢复游戏
        BattleScene.Ins.TM.ResumeTime();
    }
}
