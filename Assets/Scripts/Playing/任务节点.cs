using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class 任务节点 : MonoBehaviour
{
    public bool 是主线;
    public Sprite[] 按钮节点图片;
    public string[] 前置任务要求;
    public int[] 前置任务进度要求;
    [TextArea]
    public string missionDes;
    
    // 详细信息
    public GameObject detialInfoPanel;
    //public Text 任务名称Text;
    public Text 任务描述Text;
    //public Button 接受任务Button;
    private void OnEnable()
    {
        // GetComponent<Button>().onClick.RemoveAllListeners();
        // GetComponent<Button>().onClick.AddListener(() =>
        // {
        //     大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
        // });
        //刷新按钮可点击状态();
    }
    
    public void OnClick()
    {
        detialInfoPanel.SetActive(true);
        任务描述Text.text = missionDes;
    }

    public void OnClickStart()
    {
        大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
    }

    public void UpdateState()
    {
        GetComponent<Image>().sprite = 是主线 ? 按钮节点图片[0] : 按钮节点图片[1];
        //GetComponentInChildren<Text>().text = gameObject.name.Replace("(Clone)","");
        
        if (前置任务要求.Length!=前置任务进度要求.Length)
        {
            Debug.LogError("任务要求数量与任务进度要求数量不相同!");
        }
        if(GM.Ins.PLAYERPROFILE.获取任务进度( gameObject.name.Replace("(Clone)",""))>=1)
        {
            Debug.Log($"当前节点已完成则不显示{gameObject.name}");
            // 
            gameObject.SetActive(false);
            //GetComponent<Button>().enabled = false;
            return;
        }
        for (int i = 0; i < 前置任务要求.Length; i++)
        {
            int i1 = i;
            
            if (GM.Ins.PLAYERPROFILE.获取任务进度(前置任务要求[i1])<前置任务进度要求[i1])
            {
                //Debug.Log($"{name}不符合任务要求{前置任务要求[i1]}进度{前置任务进度要求[i1]}");
                gameObject.SetActive(false);
                //GetComponent<Button>().enabled = false;
                return;
            }
            else
            {
                Debug.Log($"符合任务要求{前置任务要求[i1]}进度{前置任务进度要求[i1]}");
                gameObject.SetActive(true);
                //GetComponent<Button>().enabled = true;
            }
        }
    }
    
}
