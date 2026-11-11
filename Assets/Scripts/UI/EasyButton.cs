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

    private bool pointerOver; // 抬起时仍在按钮内才算有效点击，用于播放点击音效

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

        // Esc 可能刚在释放时打开菜单，未收到按下事件的关闭按钮不能处理这次释放。
        if (Input.GetKeyUp(bindKey) && isHolding)
        {
            ButtonUp();
            PlayClickSound();
        }
        if (holdTime!=0 && isHolding)
        {
            timer += Time.unscaledDeltaTime;
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
                .SetEase(Ease.InOutElastic).SetUpdate(true).OnComplete(()=>OnBtnDown.Invoke());
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
            transform.DOScale(btnScale, 0.2f).SetEase(Ease.InOutElastic).SetUpdate(true).OnComplete(()=>
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
        if (pointerOver) PlayClickSound();
        ButtonUp();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        if (GM.Ins != null && GM.Ins.AM != null)
            GM.Ins.AM.PlayAudio(AudioCueType.Hover);
        OnEnter.Invoke();
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        OnExit.Invoke();
    }

    private void PlayClickSound()
    {
        if (GM.Ins == null || GM.Ins.AM == null) return;
        GM.Ins.AM.PlayAudio(AudioCueType.Click);
    }
    
    #endregion
}
