using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

/// <summary>
/// 简易的按钮，开放了点击、长按，按下、抬起的事件
/// </summary>
public class EasyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

    public Text text;
    [Header("绑定按键")]
    public KeyCode bindKey;
    
    [Header("是否开启动画")]
    public bool withAnim;

    [FoldoutGroup("事件")]
    [Header("点击事件")]
    public UnityEvent OnBtnClick;
    [FoldoutGroup("事件")][Header("长按事件")]
    public UnityEvent OnBtnHold;

    [FoldoutGroup("事件")][Header("按下事件")]
    public UnityEvent OnBtnDown;
    [FoldoutGroup("事件")][Header("抬起事件")]
    public UnityEvent OnBtnUp;

    [FoldoutGroup("事件")][Header("鼠标进入事件")]
    public UnityEvent OnEnter;

    [FoldoutGroup("事件")][Header("鼠标离开事件")]
    public UnityEvent OnExit;
    
    [FoldoutGroup("事件")][Header("按住判定时间")]
    [SerializeField] private float holdTime;

    private float timer;

    private bool isHolding;

    private Vector3 textScale;

    private Vector3 btnScale;
    // Start is called before the first frame update
    void Start()
    {
        if(text!=null) textScale = text.transform.localScale;
        btnScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(bindKey))
        {
            ButtonDown();
        }

        if (Input.GetKeyUp(bindKey))
        {
            ButtonUp();
        }
        if (holdTime!=0 && isHolding)
        {
            timer += Time.deltaTime;
            if (timer > holdTime)
            {
                OnBtnHold.Invoke();
            }
        }

        
    }

    public void ButtonDown()
    {
        isHolding = true;
        
        if (withAnim)
        {
            transform.DOScale(0.8f*btnScale, 0.2f)
                .SetEase(Ease.InOutElastic).OnComplete(()=>OnBtnDown.Invoke());
        }
        else
        {
            OnBtnDown.Invoke();
        }
    }
    public void ButtonUp()
    {
        isHolding = false;

        if (withAnim)
        {
            transform.DOScale(btnScale, 0.2f).SetEase(Ease.InOutElastic).OnComplete(()=>
            {
                OnBtnClick?.Invoke();
                OnBtnUp?.Invoke();
            });
        }
        else
        {
            OnBtnClick?.Invoke();
            OnBtnUp?.Invoke();
        }
    }

    #region UnityEvent



    

    public void OnPointerDown(PointerEventData eventData)
    {
        ButtonDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ButtonUp();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnEnter.Invoke();
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnExit.Invoke();
    }
    
    #endregion
}
