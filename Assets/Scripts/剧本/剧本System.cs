using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;


public partial class 剧本System : MonoBehaviour
{
    public static 剧本System instance;
    public List<GameObject> 已生成文本 = new List<GameObject>();
    public List<GameObject> 选项按钮 = new List<GameObject>();
    public GameObject Content;
    public GameObject 剧本预制体;
    public GameObject 剧本父物体Content;
    public Image BG, BG_black, SPEAKERBG, FULLCG, HALFCG;
    public Text 说话人TextObject;
    public float 起始生成偏移;
    public float 间隔;
    private int 已阅读 = 0;
    public static float Yoffset;
    public ScrollRect 进度条;
    public event Action 当文本更新时;
    public event Action 当检定开始时, 当检定结束时;
    public string 测试剧本;

    [Header("特效设置")] public float CG淡入淡出时间 = 0.5f;
    public RectTransform 震动目标;
    [Tooltip("与主目标同级、需要同步震动的前景层；对话框不加入此列表。")]
    public RectTransform[] 附加震动目标 = Array.Empty<RectTransform>();

    private string 储存的检定结果;
    public string[][] 已储存剧本;
    private string 当前事件 => 已储存剧本[已阅读][0];
    private string 当前说话人 => 已储存剧本[已阅读][1];
    private string 当前说话内容 => 已储存剧本[已阅读][2];
    private string[][] 记录上段剧情;

    int 语言偏移
    {
        get
        {
            switch (Center.Language)
            {
                case "CN":
                    return 0;
                case "EN":
                    return 1;
                case "JP":
                    return 2;
            }

            return 1;
        }
    }

    [SerializeField] [ReadOnly] private string curPartName;// 当前剧本名

    public CheckDicePanel checkDicePanel;

    public Color choiceColor;
    public Color choiceSelectedColor = Color.gray;
    private List<string> choiceList = new List<string>();//存储选项的列表

    
    private string curBranchName = "";//当前分支名

    public void 设置新剧本(string t)
    {
        刷新();
        curPartName = t;
        已储存剧本 = 读取表格数据(t, Center.Languageint);
        记录上段剧情 = 已储存剧本;
        RecordReadingStory(t);
    }

    public void Awake()
    {
        instance = this;
        if (测试剧本.Length > 1)
        {
            已储存剧本 = 读取表格数据(测试剧本, Center.Languageint);
            Debug.LogError("剧本测试中");
        }

        刷新();
    }

    public void 刷新()
    {
        已阅读 = 0;
        isWaitingForChoice = false;
        currentTypewriter = null;
        ResetCGLogMode();
        autoPlayElapsed = 0f;
        选项按钮.Clear();
        清空文本();
    }


    void 清空文本()
    {
        foreach (var VARIABLE in 已生成文本)
        {
            if (VARIABLE != null)
            {
                Destroy(VARIABLE.gameObject);
            }
        }

        Yoffset = 起始生成偏移;
        if (Content != null)
            Content.GetComponent<RectTransform>()
                .SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 起始生成偏移);
    }

    [ContextMenu("下一句")]
    public void Next()
    {
        if (isWaitingForChoice || 已储存剧本 == null) return;
        autoPlayElapsed = 0f;
        进度条.normalizedPosition = new Vector2(0, -1f);
        if (已阅读 >= 已储存剧本.Length)
        {
            return;
        }

        // CHOICE 会生成选项并自行推进游标，不能再生成后续正文或额外推进一行。
        if (当前说话内容.Contains(Center.Tag_notspawn) ||
            (!string.IsNullOrEmpty(当前事件) && 当前事件.Contains(Center.Command_Choice)))
        {
            进行指令(当前事件);
            return;
        }

        currentTypewriter = null;
        当文本更新时?.Invoke();

        GM.Ins.AM.播放音效("Key");
        进行指令(当前事件);
        生成剧本预制体();
        已阅读++;
        进度条.normalizedPosition = new Vector2(0, -1f);
    }

    public void SkipNext()
    {
        if (isWaitingForChoice || 已储存剧本 == null || 已阅读 >= 已储存剧本.Length - 1)
            return;

        // 保留指令行，由 OnClickSkip 退出循环后通过 Next 正常执行并显示选项。
        if (当前说话内容.Contains(Center.Tag_notspawn) ||
            (!string.IsNullOrEmpty(当前事件) && 当前事件.Contains(Center.Command_Choice)))
            return;

        已阅读++;
    }

    public void OnClickSkip(int num = 0)
    {
        if (isWaitingForChoice || 已储存剧本 == null || 已阅读 >= 已储存剧本.Length) return;
        StopAutoPlay();//停止自动播放
        Debug.Log($"跳过{已储存剧本.Length - 已阅读}条剧本，遇到选项时停止");
        GM.Ins.AM.播放音效("Key");
        int skipCount = 已储存剧本.Length - 已阅读 - num;
        for (int i = 0; i < skipCount; i++)
        {
            int previousLine = 已阅读;
            SkipNext();
            if (已阅读 == previousLine) break;
        }

        // 只执行一次停止处的内容；CHOICE 会进入等待选择状态，不再跳过选项行。
        Next();
    }


    public GameObject 生成剧本预制体()
    {
        if (isCGLogMode && !isWaitingForChoice)
        {
            if (!当前说话内容.Contains(Center.Tag_notspawn))
                ShowCGLogText(当前说话内容.Contains(Center.Tag_checkview) ? 储存的检定结果 : 当前说话内容);
            return null;
        }
        //   Debug.Log("调用生成");
        if (已阅读 > 0 && 当前说话内容 == 已储存剧本[已阅读 - 1][2])
        {
            return null;
        }

        if (当前说话内容.Contains(Center.Tag_notspawn))
        {
            return null;
        }

        GameObject go = Instantiate(剧本预制体, 剧本父物体Content.transform);
        Yoffset += 间隔;
        go.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -Yoffset);
        string 文本 = 当前说话内容;
        if (文本.Contains(Center.Tag_checkview))
        {
            文本 = 储存的检定结果;
        }

        currentTypewriter = go.GetComponent<打字机>();
        Yoffset += currentTypewriter.初始化(文本);
        说话人TextObject.text = 当前说话人;
        Content.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Yoffset);

        已生成文本.Add(go);
        return go;
    }

    public string[][] 读取表格数据(string 文件名, int 语言偏移)
    {
        string filepath = Application.streamingAssetsPath + "/" + 文件名;

        // 使用EPPlus打开临时路径的Excel文件
        using (var 包 = new ExcelPackage(new FileInfo(filepath)))
        {
            int 总行数 = 0;
            try
            {
                ExcelWorksheet 工作表 = 包.Workbook.Worksheets[1];
                总行数 = 工作表.Dimension.End.Row;

                // 获取工作表的总行数


                // 初始化结果数组，从第二行开始读取
                string[][] 数据 = new string[总行数 - 1][];

                for (int i = 2; i <= 总行数; i++) // 从第二行开始
                {
                    数据[i - 2] = new string[3]; // 初始化每行的数据

                    数据[i - 2][0] = 工作表.Cells[i, 1].Text; // 事件

                    // 根据语言偏移获取合并后的内容
                    string 合并内容 = 工作表.Cells[i, 2 + 语言偏移].Text;

                    // 找到冒号并分割
                    int 冒号位置 = 合并内容.IndexOf(':');
                    if (冒号位置 != -1)
                    {
                        数据[i - 2][1] = 合并内容.Substring(0, 冒号位置).Trim(); // 说话人
                        数据[i - 2][2] = 合并内容;
                    }
                    else
                    {
                        数据[i - 2][1] = "";
                        数据[i - 2][2] = 合并内容;
                    }
                }
                if (GM.Ins != null && GM.Ins.PLAYERPROFILE != null)
                    GM.Ins.PLAYERPROFILE.RecordScriptChapter(文件名);
                return 数据;
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError($"没有找到表格{文件名}检查Streamingassets文件夹里是否有这个表格");
            }

            return null;
        }
    }

    public void LoadImage(string imageName, Image targetimg)
    {
        Texture2D texture = Resources.Load<Texture2D>("CG/" + imageName);

        if (texture == null)
        {
            Debug.LogError("无法加载图片: " + imageName);
            return;
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height)
            , new Vector2(0.5f, 0.5f));
        targetimg.sprite = sprite;
    }

    /// <summary>
    /// 加载图片（带淡入淡出效果）
    /// </summary>
    /// <param name="targetimg"></param>
    /// <param name="fadeType">淡入淡出类型：0=无效果, 1=淡入, 2=淡出, 3=淡出后淡入</param>
    /// <param name="duration">淡入淡出时间（秒），-1表示使用默认时间</param>
    /// <param name="imageName"></param>
    public void LoadImageWithFade(string imageName, Image targetimg, int fadeType = 3
        , float duration = -1)
    {
        if (FULLCG.gameObject.activeInHierarchy)
            FULLCG.transform.parent.gameObject.SetActive(false);
        if (HALFCG.gameObject.activeInHierarchy)
            HALFCG.transform.parent.gameObject.SetActive(false);
        if (duration < 0)
        {
            duration = CG淡入淡出时间;
        }

        Texture2D texture = Resources.Load<Texture2D>("CG/" + imageName);

        if (texture == null)
        {
            Debug.LogWarning("无法加载图片: " + imageName);
            return;
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height)
            , new Vector2(0.5f, 0.5f));
        targetimg.gameObject.SetActive(true);
        // 根据淡入淡出类型执行不同的效果
        switch (fadeType)
        {
            case 0: // 无效果，直接切换
                targetimg.sprite = sprite;
                break;

            case 1: // 仅淡入
                targetimg.sprite = sprite;
                targetimg.color = new Color(1, 1, 1, 0);
                targetimg.DOFade(1f, duration);
                break;

            case 2: // 仅淡出
                targetimg.DOFade(0f, duration).OnComplete(() =>
                {
                    targetimg.sprite = sprite;
                    targetimg.color = new Color(1, 1, 1, 1);
                });
                break;

            case 3: // 淡出后淡入（默认）
                targetimg.DOFade(0f, duration / 2).OnComplete(() =>
                {
                    targetimg.sprite = sprite;
                    targetimg.DOFade(1f, duration / 2);
                });
                break;
        }
    }

    public Text logText;
    public void ShowText(string str, float fadeDuration, float displayDuration)
    {
        logText.color = new Color(logText.color.r, logText.color.g, logText.color.b, 0f); // 初始透明度为0
        logText.gameObject.SetActive(true);
        logText.text = str;
        Sequence fadeTweener = DOTween.Sequence();
        //fadeTweener.AppendInterval(startDelay);
        fadeTweener.Append(logText.DOFade(1f, fadeDuration).SetEase(Ease.Linear));
        fadeTweener.AppendInterval(displayDuration);
        fadeTweener.Append(logText.DOFade(0f, fadeDuration));
        fadeTweener.AppendCallback(() => logText.gameObject.SetActive(true));
    }

    [Button("切换下一个差分")]
    public void OnClickRest()
    {
        // 重置状态
        Debug.Log("进行差分切换");
        // 推进日期
        大地图System.instance.GetDaytimeSystem()?.CostDaytime(1);
    }

    


    // ====== Test ======= //
    [Button("测试Log")]
    public void TestLog(string log)
    {
        大地图System.instance.开始剧情(log);
    }

    [Button("测试指令")]
    public void TestCommand(string command)
    {
        进行指令(command);
    }
    
    
    
}
