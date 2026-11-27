using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;          // ← 必须使用这个

public class TutorialUI : MonoBehaviour
{
    public TutorialDatabaseSO database;
    [Header("UI组件引用")]
    public Image displayImage;
    public Text displayText;
    public Text inforText;
    public Button prevBtn;
    public Button nextBtn;
    public GameObject completeBtn;

    private List<TutorialPage> _pages;
    private int _currentIndex = 0;
    private string _currentLevelName;
    
    private System.Action transientCompletion; // 关卡提示不写入已读存档，关闭后交还流程控制器。
    private bool transientImageEnabled;
    private bool _isBattleStart = false; // 是否在战斗开始时显示教程

    private void Awake()
    {
        //prevBtn.onClick.AddListener(PrevPage);
        //nextBtn.onClick.AddListener(NextPage);
        //completeBtn.onClick.AddListener(CompleteTutorial);

        //gameObject.SetActive(false);
    }

    public void Show(string levelName)
    {
        // 使用接口获取教程数据
        TutorialData data = database.GetTutorial(levelName);
        if (data != null)
        {
            this.Show(data, levelName);
        }
    }

    /// <summary>
    /// 显示教程（使用新的 TutorialData 类型）
    /// </summary>
    public void Show(TutorialData data, string levelName, bool battleStart = false)
    {
        if (data == null || data.pages.Count == 0) return;

        transientCompletion = null;
        _currentLevelName = levelName;
        _pages = data.pages;
        _currentIndex = 0;
        gameObject.SetActive(true);
        UpdatePage();
        _isBattleStart = battleStart;
    }

    private void UpdatePage()
    {
        TutorialPage page = _pages[_currentIndex];
        displayImage.sprite = page.image;
        displayText.text = page.title;
        inforText.text = page.description;

        // 按钮状态
        prevBtn.gameObject.SetActive(_currentIndex > 0);
        nextBtn.gameObject.SetActive(_currentIndex < _pages.Count - 1);
        completeBtn.gameObject.SetActive(_currentIndex == _pages.Count - 1);
    }

    public void NextPage()
    {
        if (_currentIndex < _pages.Count - 1)
        {
            _currentIndex++;
            UpdatePage();
            GM.Ins.AM.PlayAudio(AudioCueType.Click);
        }
    }

    public void PrevPage()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            UpdatePage();
            GM.Ins.AM.PlayAudio(AudioCueType.Click);
        }
    }

    /// <summary>显示一次关卡提示；不受永久已读标记影响，完成后回调调用方。</summary>
    public void ShowTransient(string title, string body, System.Action onComplete, Sprite illustration = null)
    {
        Show(new TutorialData { pages = new List<TutorialPage> { new TutorialPage { title = title, description = body, image = illustration } } }, null);
        transientCompletion = onComplete;
        transientImageEnabled = displayImage.enabled;
        displayImage.enabled = illustration != null;
    }

    public void CompleteTutorial()
    {
        if (!gameObject.activeInHierarchy) return;
        var completion = transientCompletion;
        transientCompletion = null;
        if (completion == null) GM.Ins.PLAYERPROFILE.seenTutorials.Add(_currentLevelName);
        else displayImage.enabled = transientImageEnabled;
        bool battleStart = _isBattleStart;
        _isBattleStart = false;
        if (battleStart) BattleScene.Ins.BM.CompleteBattleIntroduction();
        gameObject.SetActive(false);
        GM.Ins.AM.PlayAudio(AudioCueType.Close);
        completion?.Invoke();
    }

    private void OnDisable()
    {
        var completion = transientCompletion;
        transientCompletion = null;
        if (completion == null) return;
        // Esc关闭也算跳过本次说明；转场回调由调用方校验所属战斗，不能推进新场景。
        displayImage.enabled = transientImageEnabled;
        completion.Invoke();
    }
}
