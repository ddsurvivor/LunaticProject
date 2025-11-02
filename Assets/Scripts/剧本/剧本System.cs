using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using DG.Tweening;


public class 剧本System: MonoBehaviour
{
    public static 剧本System instance;
    public List<GameObject> 已生成文本 = new List<GameObject>();
    public List<GameObject> 选项按钮 = new List<GameObject>();
    public GameObject Content;
    public GameObject 剧本预制体;
    public GameObject 剧本父物体Content;
    public Image BG,SPEAKERBG;
    public Text 说话人TextObject;
    public float 起始生成偏移;
    public float 间隔;
    private int 已阅读=0;
    public static float Yoffset;
    public ScrollRect 进度条;
    public event Action 当文本更新时;
    public event Action 当检定开始时,当检定结束时;
    public string 测试剧本;
    
    [Header("特效设置")]
    public float CG淡入淡出时间 = 0.5f; 
    public RectTransform 震动目标;

    private string 储存的检定结果;
    public string[][] 已储存剧本;
    private string 当前事件 => 已储存剧本[已阅读][0];
    private string 当前说话人 => 已储存剧本[已阅读][1];
    private string 当前说话内容 => 已储存剧本[已阅读][2];
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

    public void 设置新剧本(string t)
    {
        刷新();
        已储存剧本 = 读取表格数据(t, Center.Languageint);
    }

    public void Awake()
    {
        instance = this;
        if (测试剧本.Length>1)
        {
            已储存剧本 = 读取表格数据(测试剧本,Center.Languageint );
            Debug.LogError("剧本测试中");
        }
        刷新();
    }

    public void 刷新()
    {
        已阅读 = 0;
      清空文本();
      
    }

    void 清空文本()
    {
        foreach (var VARIABLE in 已生成文本)
        {
            Destroy(VARIABLE.gameObject);
        }

        Yoffset = 起始生成偏移;
        Content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,起始生成偏移);
    }
[ContextMenu("下一句")]
    public void Next()
    {
        进度条.normalizedPosition = new Vector2(0, -1f);
        if (已阅读>=已储存剧本.Length)
        {
            return;
        }
        if (当前说话内容.Contains(Center.Tag_notspawn))
        {
            进行指令(当前事件);
            return;
        }
        当文本更新时?.Invoke();
   
        进行指令(当前事件);
        生成剧本预制体();
        已阅读++;
    }

    public GameObject 生成剧本预制体()
    {
     //   Debug.Log("调用生成");
        if (已阅读>0 && 当前说话内容== 已储存剧本[已阅读-1][2])
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
        Yoffset+= go.GetComponent<打字机>().初始化(文本);
        说话人TextObject.text = 当前说话人;
        Content.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Yoffset);
        
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
                ExcelWorksheet  工作表 = 包.Workbook.Worksheets[1];
                总行数= 工作表.Dimension.End.Row;
                
                // 获取工作表的总行数
           

                // 初始化结果数组，从第二行开始读取
                string[][]  数据 = new string[总行数 - 1][];

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

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        targetimg.sprite = sprite;
    }

    /// <summary>
    /// 加载图片（带淡入淡出效果）
    /// </summary>
    /// <param name="targetimg"></param>
    /// <param name="fadeType">淡入淡出类型：0=无效果, 1=淡入, 2=淡出, 3=淡出后淡入</param>
    /// <param name="duration">淡入淡出时间（秒），-1表示使用默认时间</param>
    /// <param name="imageName"></param>
    public void LoadImageWithFade(string imageName, Image targetimg, int fadeType = 3, float duration = -1)
    {
        if (duration < 0)
        {
            duration = CG淡入淡出时间;
        }
        
        Texture2D texture = Resources.Load<Texture2D>("CG/" + imageName);

        if (texture == null)
        {
            Debug.LogError("无法加载图片: " + imageName);
            return;
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        
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
    
    public void 执行震动(float strength = 20f, float duration = 0.5f, int vibrato = 10)
    {
        if (震动目标 == null)
        {
            Debug.LogError("震动目标未设置，请在Inspector中设置震动目标");
            transform.DOShakePosition(duration, strength, vibrato);
        }
        else
        {
            震动目标.DOShakePosition(duration, strength, vibrato);
        }
    }
    
      public void 进行指令(string tar){
          Debug.Log($"进行指令{tar}");
                var face = tar.Split(Center.Plot指令分隔符);
                foreach (var key in face)
                {
                    if (key.Contains(Center.Command_Modify))
                    {
                        var prams = 指令切割(key);
                       PLAYERPROFILE.修改属性(Convert.ToInt32(prams[0]),prams[1],Convert.ToInt32(prams[2]));
                    }
                    if (key.Contains(Center.Command_background))
                    {
                        var prams = 指令切割(key);
                        if (prams.Length >= 3)
                        {
                            // 有淡入淡出类型和自定义时长
                            int fadeType = Convert.ToInt32(prams[1]);
                            float duration = float.Parse(prams[2]);
                            LoadImageWithFade(prams[0], BG, fadeType, duration);
                        }
                        else if (prams.Length >= 2)
                        {
                            // 有淡入淡出类型，使用默认时长
                            int fadeType = Convert.ToInt32(prams[1]);
                            LoadImageWithFade(prams[0], BG, fadeType);
                        }
                        else
                        {
                            // 使用默认淡出后淡入效果
                            LoadImageWithFade(prams[0], BG, 3);
                        }
                    }
                    if (key.Contains(Center.Command_SpeakerSet))
                    {
                        var prams = 指令切割(key);
                        LoadImage(prams[0],SPEAKERBG);
                    }
                    if (key.Contains(Center.Command_Set))
                    {
                        var prams = 指令切割(key);
                        变量.修改变量(prams[0],Convert.ToInt32(prams[1]));
                    }
                    if (key.Contains(Center.Command_Check))//检定
                    {
                        当检定开始时?.Invoke();
                        var prams = 指令切割(key);
                        string 被检定属性 = prams[0];
                        int 骰子数量=Convert.ToInt32(prams[1]);
                        int 骰子大小=Convert.ToInt32(prams[2]);
                        int 检定角色=Convert.ToInt32(prams[3]);
                        int 检定目标=Convert.ToInt32(prams[4]);
                        string 成功修改变量= prams[5];
                        int 修改结果=Convert.ToInt32(prams[6]);
                        string 失败修改变量 = prams[7];
                        int 修改结果F=Convert.ToInt32(prams[8]);
                        int 修正值 = PLAYERPROFILE.获取数据<int>(被检定属性,检定角色);
                        int 随机值 = 0;
                        for (int i = 0; i < 骰子数量; i++)
                        {
                            随机值 += Random.Range(0, 骰子大小)+1;
                        }
                        int 最终值 = 随机值 + 修正值;
                        if (最终值>=检定目标)
                        {
                           变量.修改变量(成功修改变量,修改结果);
                           Debug.Log(成功修改变量+$"修改为{修改结果}");
                        }
                        else
                        {
                            变量.修改变量(失败修改变量,修改结果F);
                            Debug.Log(失败修改变量+$"修改为{修改结果F}");
                        }

                        储存的检定结果 = $"{骰子数量}D{骰子大小}={随机值}  {随机值}+ {修正值}={最终值}";
                        当检定结束时?.Invoke();
                    }

                    if (key.Contains(Center.Command_Choice))
                    {
                        选项按钮.Clear();
                        var prams = 指令切割(key);
                        int 选项长度= Convert.ToInt32(prams[0]);
                        for (int i = 0; i < 选项长度; i++)
                        {
                            已阅读++;
                            GameObject go = 生成剧本预制体();
                            GameObject text=  go.GetComponent<打字机>().textComponent.gameObject;
                            text.AddComponent<Button>();
                            text.GetComponent<Text>().color = Color.green;
                            text.GetComponent<Text>().raycastTarget = true;
                            string 事件=当前事件;
                            text.GetComponent<Button>().onClick.AddListener(() =>
                            {
                                if (事件 != null) 进行指令(事件);
                            
                                foreach (var VARIABLE in 选项按钮)
                                {
                                    VARIABLE.GetComponent<Button>().enabled = false;
                                    VARIABLE.GetComponent<Text>().color = Color.gray;
                                }
                                text.GetComponent<Text>().color = Color.yellow;
                            });
                            选项按钮.Add(text);
                        }
                        已阅读++;
                    }
                    if (key.Contains(Center.Command_Debug))
                    {
                        Debug.Log($"<color=red>剧本Debug</color>>>{key}");
                    }
                    if (key.Contains(Center.Command_Setspace))
                    {
                        var prams = 指令切割(key);
                        间隔=Convert.ToInt32(prams[0]);
                    }

                    if (key.Contains(Center.Command_Next))
                    {
                        var prams = 指令切割(key);
                        已储存剧本 = 读取表格数据(prams[0], Center.Languageint);
                        try
                        {
                            已阅读 = Convert.ToInt32( prams[1])-2;
                        }
                        catch ( IndexOutOfRangeException e)
                        {
                            已阅读 = 0;
                        }

                        已阅读 = 已阅读 > 1 ? 已阅读 : 0;
                        Debug.Log($"跳转到表格{prams[0]}");
                        Next();
                    }
                    if (key.Contains(Center.Command_If))
                    {
                        var prams = 指令切割(key);
                        if (prams[0]==已阅读.ToString())
                        {
                            Debug.LogError("跳转到的行与当前行相同,可能造成循环");
                        }
                        Debug.Log($"鉴定{prams[0]}值={prams[1]}");
                        if (变量.获取变量(prams[0])==Convert.ToInt32(prams[1]))
                        {
                            已阅读=Convert.ToInt32(prams[2])-2;
                            Debug.Log($"跳转到{prams[2]}");
                            try
                            {
                                进行指令(当前事件);
                            }
                            catch ( IndexOutOfRangeException e)
                            {
                                Debug.Log($"当前阅读章节={已阅读}");
                                int i1 = 0;
                                foreach (var s in 已储存剧本)
                                {
                                   Debug.Log($"第{i1}行的指令为{s}");
                                   i1++;
                                }
                            }
                  
                        }
                        else
                        {
                            Debug.Log($"鉴定失败{prams[0]}的值是{变量.获取变量(prams[0])}");
                        }
                    }
                    if (key.Contains(Center.Command_Skip))
                    {
                        已阅读++;
                        Next();
                    }
                    if (key.Contains(Center.Command_End))
                    {
                        var prams = 指令切割(key);
                       大地图System.instance.剧情结束();
                       try
                       {
                           PLAYERPROFILE.instance.保存任务进度(prams[0], Convert.ToInt32(prams[1]));
                       }
                       catch (IndexOutOfRangeException e)
                       {
                           
                       }
                    
                 
                    }
                    if (key.Contains(Center.Command_Jump))
                    {   
                        var prams = 指令切割(key);
                        已阅读=Convert.ToInt32(prams[0]);
                    }
                    if (key.Contains(Center.Command_Refresh))
                    {   
                      刷新();
                    }
                    if (key.Contains(Center.Command_Clear))
                    {   
                        清空文本();
                    }
                    if (key.Contains(Center.Command_Sound))
                    {
                        var prams = 指令切割(key);
                        if (AudioManager.instance != null)
                        {
                            if (prams.Length >= 2)
                            {
                                int loopParam = Convert.ToInt32(prams[1]);
                                AudioManager.instance.播放音效(prams[0], loopParam);
                            }
                            else
                            {
                                AudioManager.instance.播放音效(prams[0]);
                            }
                        }
                        else
                        {
                            Debug.LogWarning("AudioManager未初始化，无法播放音效");
                        }
                    }
                    if (key.Contains(Center.Command_Music))
                    {
                        var prams = 指令切割(key);
                        if (AudioManager.instance != null)
                        {
                            AudioManager.instance.播放音乐(prams[0]);
                        }
                        else
                        {
                            Debug.LogWarning("AudioManager未初始化，无法播放音乐");
                        }
                    }
                    if (key.Contains(Center.Command_Shake))
                    {
                        var prams = 指令切割(key);
                        if (prams == null || prams.Length == 0 || string.IsNullOrEmpty(prams[0]))
                        {
                            执行震动();
                        }
                        else if (prams.Length >= 3)
                        {
                            // 完整参数：强度、时长、次数
                            float strength = float.Parse(prams[0]);
                            float duration = float.Parse(prams[1]);
                            int vibrato = Convert.ToInt32(prams[2]);
                            执行震动(strength, duration, vibrato);
                        }
                        else if (prams.Length >= 2)
                        {
                            // 强度和时长
                            float strength = float.Parse(prams[0]);
                            float duration = float.Parse(prams[1]);
                            执行震动(strength, duration);
                        }
                        else
                        {
                            // 仅强度
                            float strength = float.Parse(prams[0]);
                            执行震动(strength);
                        }
                    }
                }
      }
      public static string[] 指令切割(string command)
      {
          var match = Regex.Match(command, @"\(([^)]*)\)");
          if (match.Success)
          {
              var prams = match.Groups[1].Value.Split(',');
              return prams;
          }

          if(match.Groups[1].Value == "")
          {
              Debug.LogError($"指令格式错误，请检查是否有参数");
          }
          else
          {
              Debug.LogError($"指令格式错误，请检查是否有括号");
          }

          return null;
      }
}