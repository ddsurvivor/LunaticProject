using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class 任务节点 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool 是主线;
    [LabelText("图标序号")]public int iconIndex;
    public Sprite[] 按钮节点图片;
    public string[] 前置任务要求;
    public int[] 前置任务进度要求;
    [TextArea]
    public string missionDes;
    
    // 详细信息
    public GameObject detialInfoPanel;
    public Image detialInfoIcon;
    //public Text 任务名称Text;
    public Text titleText;
    public Text nameText;
    public Text descText;
    //public Button 接受任务Button;


    //public Sprite[] icons = new Sprite[3];
    public Image icon;
    public GameObject smallPanel;
    private Vector3 smallPanelOriginalPos;
    public bool isShowDetialInfo;
    public Image arrow;
    public GameObject button;
    private Vector3 buttonOriginalPos;
    public GameObject closeBtn;
    
    private void OnEnable()
    {
        // GetComponent<Button>().onClick.RemoveAllListeners();
        // GetComponent<Button>().onClick.AddListener(() =>
        // {
        //     大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
        // });
        //刷新按钮可点击状态();
    }

    private void Start()
    {
        smallPanelOriginalPos = smallPanel.transform.localPosition;
        smallPanel.SetActive(false);
        buttonOriginalPos = button.transform.localPosition;
        if (iconIndex < 按钮节点图片.Length)
        {
            icon.sprite = 按钮节点图片[iconIndex];
        }
    }


    public void OnClick()
    {
        //detialInfoPanel.SetActive(true);
        //任务描述Text.text = missionDes;
        ShowDetialInfo();
    }

    public void OnClickStart()
    {
        大地图System.instance.开始剧情 (name.Replace("(Clone)",""));
    }

    public void UpdateState()
    {
        
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
    
    Sequence infoSequence;
    private void ShowDetialInfo()
    {
        if(isShowDetialInfo) return;
        isShowDetialInfo = true;
        smallPanel.SetActive(false);
        detialInfoPanel.SetActive(true);
        descText.gameObject.SetActive(false);
        descText.text = missionDes;
        nameText.text = titleText.text;
        nameText.gameObject.SetActive(false);
        detialInfoIcon.fillAmount = 0f;
        arrow.fillAmount = 0f;
        //arrow.gameObject.SetActive(true);
        button.transform.localPosition = buttonOriginalPos;
        button.SetActive(false);
        closeBtn.SetActive(false);
        infoSequence?.Kill();
        infoSequence = DOTween.Sequence();
        // 首先arrow填充变成1， 之后info icon 的填充变到1，之后文本激活，最后按钮向下移动100
        infoSequence.Append(arrow.DOFillAmount(1f, 0.3f).SetEase(Ease.OutCubic));
        infoSequence.Append(detialInfoIcon.DOFillAmount(1f, 0.3f).SetEase(Ease.OutCubic));
        infoSequence.AppendCallback(() =>
        {
            descText.gameObject.SetActive(true);
            nameText.gameObject.SetActive(true);
            closeBtn.SetActive(true);
            button.SetActive(true);
        });
        infoSequence.Append(button.transform.DOLocalMoveY(100, 0.3f).SetEase(Ease.OutBack).From());
    }
    public void CloseDetialInfo()
    {
        isShowDetialInfo = false;
        detialInfoPanel.SetActive(false);
        arrow.fillAmount = 0f;
    }

    private Tweener currentTween;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isShowDetialInfo) return;
        currentTween?.Kill();
        smallPanel.SetActive(true);
        icon.transform.localScale = Vector3.one * 1.2f;
        currentTween = smallPanel.transform.DOLocalMoveX(-100, 0.2f).SetEase(Ease.OutBack).From();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isShowDetialInfo) return;
        currentTween?.Kill();
        smallPanel.SetActive(false);
        icon.transform.localScale = Vector3.one;
        smallPanel.transform.localPosition = smallPanelOriginalPos;
    }
}
