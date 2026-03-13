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
    public Button completeBtn;

    private List<TutorialPage> _pages;
    private int _currentIndex = 0;
    private string _currentLevelName;

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
    public void Show(TutorialData data, string levelName)
    {
        if (data == null || data.pages.Count == 0) return;

        _currentLevelName = levelName;
        _pages = data.pages;
        _currentIndex = 0;
        gameObject.SetActive(true);
        UpdatePage();
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
        }
    }

    public void PrevPage()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            UpdatePage();
        }
    }

    public void CompleteTutorial()
    {
        //PlayerPrefs.SetInt("TutorialSeen_" + _currentLevelName, 1);
        //PlayerPrefs.Save();
        GM.Ins.PLAYERPROFILE.seenTutorials.Add(_currentLevelName);
        gameObject.SetActive(false);
    }
}