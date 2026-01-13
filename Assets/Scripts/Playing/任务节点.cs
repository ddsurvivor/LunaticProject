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
    private void OnEnable()
    {
        // GetComponent<Button>().onClick.RemoveAllListeners();
        // GetComponent<Button>().onClick.AddListener(() =>
        // {
        //     大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
        // });
        刷新按钮可点击状态();
    }
    
    public void OnClick()
    {
        大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
    }

    public void 刷新按钮可点击状态()
    {
        GetComponent<Image>().sprite = 是主线 ? 按钮节点图片[0] : 按钮节点图片[1];
        if (前置任务要求.Length!=前置任务进度要求.Length)
        {
            Debug.LogError("任务要求数量与任务进度要求数量不相同!");
        }
        for (int i = 0; i < 前置任务要求.Length; i++)
        {
            int i1 = i;
            if (PLAYERPROFILE.instance.获取任务进度(前置任务要求[i1])<前置任务进度要求[i1])
            {
                gameObject.SetActive(false);
                //GetComponent<Button>().enabled = false;
                return;
            }
            else
            {
                gameObject.SetActive(true);
                //GetComponent<Button>().enabled = true;
            }
        }
    }
    
}
