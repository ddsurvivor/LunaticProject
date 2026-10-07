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


public partial class 剧本System
{
    private Tweener shakeTween;
    private Action restoreShakePositions;

    public void 执行震动(float strength = 20f, float duration = 0.5f, int vibrato = 10)
    {
        StopReaderShake();
        Transform target = 震动目标 != null ? 震动目标 : transform;
        Vector3 origin = target.localPosition;
        var extraTargets = new List<RectTransform>();
        var extraOrigins = new List<Vector3>();
        foreach (var extra in 附加震动目标 ?? Array.Empty<RectTransform>())
        {
            if (extra == null || extra == target || extraTargets.Contains(extra)) continue;
            extraTargets.Add(extra);
            extraOrigins.Add(extra.localPosition);
        }

        // UI 使用锚点坐标；前景层复制同一偏移，保持原有覆盖顺序及画面同步。
        Tweener tween = target is RectTransform rect
            ? rect.DOShakeAnchorPos(duration, strength, vibrato)
            : target.DOShakePosition(duration, strength, vibrato);
        shakeTween = tween;
        tween.OnUpdate(() =>
        {
            if (target == null) return;
            Vector3 offset = target.localPosition - origin;
            for (int i = 0; i < extraTargets.Count; i++)
                if (extraTargets[i] != null) extraTargets[i].localPosition = extraOrigins[i] + offset;
        });
        restoreShakePositions = () =>
        {
            if (target != null) target.localPosition = origin;
            for (int i = 0; i < extraTargets.Count; i++)
                if (extraTargets[i] != null) extraTargets[i].localPosition = extraOrigins[i];
        };
        tween.OnKill(() =>
        {
            // DOTween 更新期间 Kill 的回调可能延后，旧回调不能覆盖新一轮震动。
            if (shakeTween != tween) return;
            restoreShakePositions?.Invoke();
            shakeTween = null;
            restoreShakePositions = null;
        });
    }

    private void StopReaderShake()
    {
        var previousTween = shakeTween;
        var restore = restoreShakePositions;
        shakeTween = null;
        restoreShakePositions = null;
        previousTween?.Kill();
        restore?.Invoke();
    }

    public void 进行指令(string tar)
    {
        if(tar == null || tar.Length <= 0) return;
        Debug.Log($"进行指令{tar}");
        var face = tar.Split(Center.Plot指令分隔符);
        foreach (var key in face)
        {
            string command = GetCommand(key.Trim());
            // ACH 单独处理并退出本条指令，避免成就名中的
            // CHAPTER、GAMEOVER 等字样被下方旧版 Contains 判断误识别为指令。
            if (command == Center.Command_Achievement)
            {
                var prams = 指令切割(key);
                if (prams == null || prams.Length != 1 || string.IsNullOrWhiteSpace(prams[0]))
                {
                    Debug.LogError($"ACH 指令格式错误：{key}，应为 ACH(成就API名)");
                    continue;
                }

                SteamAchievements.Unlock(prams[0]);
                continue;
            }

            if (command == Center.Command_End)
            {
                var prams = 指令切割(key);
                if (prams == null || prams.Length < 2 ||
                    string.IsNullOrWhiteSpace(prams[0]) || !int.TryParse(prams[1], out int result))
                {
                    Debug.LogError($"END 指令格式错误：{key}，应为 END(任务名,进度)");
                    continue;
                }

                GM.Ins.PLAYERPROFILE.保存任务进度(prams[0], result);
                大地图System.instance.剧情结束();
                continue;
            }
            //Debug.Log($"进行命令{command}");
            if (key.Contains(Center.Command_Modify))
            {
                var prams = 指令切割(key);
                if (prams.Length >= 4)
                {
                    int.TryParse(prams[0], out int index);
                    int.TryParse(prams[1], out int attr);
                    int.TryParse(prams[2], out int opIndex);
                    int.TryParse(prams[3], out int value);
                    GM.Ins.PLAYERPROFILE.ModPlayer(index, attr, opIndex, value);
                    // 消息系统
                    //大地图System.instance.messagePanel.ShowModifyMessage(index, attr, opIndex, value);
                    大地图System.instance.notificationManager.ShowModifyMessage(index, attr, opIndex, value);
                }
                else if (prams.Length >= 3)
                {
                    GM.Ins.PLAYERPROFILE.Modify(Convert.ToInt32(prams[0]), prams[1]
                        , Convert.ToInt32(prams[2]));
                }
            }

            if (key.Contains(Center.Command_background))
            {
                if (!key.Contains(Center.Command_FullCG) && !key.Contains(Center.Command_HalfCG))
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
            }

            if (key.Contains(Center.Command_SpeakerSet))
            {
                var prams = 指令切割(key);
                if(prams.Length < 1 || prams[0] == "")
                {
                    SPEAKERBG.gameObject.SetActive(false);
                }
                else
                {
                    SPEAKERBG.gameObject.SetActive(true);
                    LoadImage(prams[0], SPEAKERBG);
                }
            }

            if (key.Contains(Center.Command_Set))
            {
                var prams = 指令切割(key);
                变量.修改变量(prams[0], Convert.ToInt32(prams[1]));
            }

            if (key.Contains(Center.Command_Check)) //检定
            {
                当检定开始时?.Invoke();
                var prams = 指令切割(key);
                string 被检定属性 = prams[0];
                int 骰子数量 = Convert.ToInt32(prams[1]);
                int 骰子大小 = Convert.ToInt32(prams[2]);
                int 检定角色 = Convert.ToInt32(prams[3]);
                int 检定目标 = Convert.ToInt32(prams[4]);
                string 成功修改变量 = prams[5];
                int 修改结果 = Convert.ToInt32(prams[6]);
                string 失败修改变量 = prams[7];
                int 修改结果F = Convert.ToInt32(prams[8]);
                int 修正值 = GM.Ins.PLAYERPROFILE.获取数据<int>(被检定属性, 检定角色);
                int 随机值 = 0;
                int[] 骰子结果 = new int[骰子数量];
                for (int i = 0; i < 骰子数量; i++)
                {
                    int roll = Random.Range(1, 骰子大小 + 1);
                    骰子结果[i] = roll;
                    随机值 += roll;
                }

                int 最终值 = 随机值 + 修正值;
                if (最终值 >= 检定目标)
                {
                    变量.修改变量(成功修改变量, 修改结果);
                    Debug.Log(成功修改变量 + $"修改为{修改结果}");
                }
                else
                {
                    变量.修改变量(失败修改变量, 修改结果F);
                    Debug.Log(失败修改变量 + $"修改为{修改结果F}");
                }

                储存的检定结果 = $"{骰子数量}D{骰子大小}={随机值}  {随机值}+ {修正值}={最终值}";
                checkDicePanel.ShowResult(骰子数量, 骰子结果, 最终值 >= 检定目标);
                当检定结束时?.Invoke();
            }

            if (key.Contains(Center.Command_Choice))
            {
                isWaitingForChoice = true;
                autoPlayElapsed = 0f;
                选项按钮.Clear();
                int choiceStartLine = 已阅读;// 【新增】记录当前 CHOICE 指令的行号，作为相对索引计算的基准
                curBranchName = curPartName;// 遇到分支则存储一次
                var prams = 指令切割(key);
                int 选项长度 = Convert.ToInt32(prams[0]);
                for (int i = 0; i < 选项长度; i++)
                {
                    已阅读++;
                    string curChoiceId = $"{curPartName}_{已阅读}";
                    
                    // ==================== 【新增：选项解锁条件解析】 ====================
                    string 原始文本 = 当前说话内容;
                    string 无标签文本 = 原始文本;
                    bool 满足条件 = true;

                    // 匹配格式如 <Req=1,2,3> （使用等号避免与你的原始代码拆分说话人的冒号冲突）
                    var match = Regex.Match(原始文本, @"\[Req=([\d,]+)\]");
                    if (match.Success)
                    {
                        Debug.Log("匹配成功");
                        string[] reqs = match.Groups[1].Value.Split(',');
                        foreach (var req in reqs)
                        {
                            if (int.TryParse(req, out int reqIndex))
                            {
                                // 将相对序号(1,2,3)转换为当前剧本表的绝对ID
                                string requiredChoiceId = $"{curPartName}_{choiceStartLine + reqIndex}";
                                if (!choiceList.Contains(requiredChoiceId))
                                {
                                    满足条件 = false;
                                    break;
                                }
                            }
                        }
                        // 剔除标签字符串，避免显示在游戏中
                        无标签文本 = 原始文本.Replace(match.Value, "").Trim();
                    }

                    // 如果前置选项还没被点过，则直接跳过该选项的生成
                    if (!满足条件)
                    {
                        continue; 
                    }
                    // ====================================================================
                    
                    // 临时覆盖为无标签纯净文本，防止标签文字显示在 UI 上
                    已储存剧本[已阅读][2] = 无标签文本;
                    
                    GameObject go = 生成剧本预制体();
                    
                    // 预制体生成后，立刻恢复原始文本，确保玩家下次循环跳回这里时条件标签不丢失
                    已储存剧本[已阅读][2] = 原始文本;

                    if (go == null) continue; // 增加安全校验
                    GameObject text = go.GetComponent<打字机>()._textComponent.gameObject;
                    text.AddComponent<Button>();
                    go.GetComponent<打字机>().ShowOption();
                    // 查询选项id，如果存在，则显示灰色
                    text.GetComponent<Text>().color = 
                        choiceList.Contains(curChoiceId) ? choiceSelectedColor : choiceColor;
                    text.GetComponent<Text>().raycastTarget = true;
                    string 事件 = 当前事件;
                    text.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        if (!isWaitingForChoice) return;
                        GM.Ins.AM.播放音效("Key");

                        foreach (var VARIABLE in 选项按钮)
                        {
                            VARIABLE.GetComponent<Button>().enabled = false;
                            VARIABLE.GetComponent<Text>().color = Color.gray;
                        }

                        text.GetComponent<Text>().color = Color.white;
                        go.GetComponent<打字机>().ShowSelect();
                        // 记录选项id
                        choiceList.Add(curChoiceId);
                        Debug.Log($"<color=green>已选择选项</color>>>{curChoiceId}  当前选项总数：{choiceList.Count}");

                        // 先完成旧选项的清理，再执行分支，避免覆盖分支中新生成的选项状态。
                        选项按钮.Clear();
                        isWaitingForChoice = false;
                        autoPlayElapsed = 0f;
                        if (事件 != null) 进行指令(事件);
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
                间隔 = Convert.ToInt32(prams[0]);
            }

            if (key.Contains(Center.Command_Next))
            {
                var prams = 指令切割(key);
                已储存剧本 = 读取表格数据(prams[0], Center.Languageint);
                curPartName = prams[0];
                RecordReadingStory(prams[0]);
                // 自动保存
                //进度System.存档("cache");
                记录上段剧情 = 已储存剧本;
                try
                {
                    已阅读 = Convert.ToInt32(prams[1]) - 2;
                }
                catch (IndexOutOfRangeException e)
                {
                    已阅读 = 0;
                }

                已阅读 = 已阅读 > 1 ? 已阅读 : 0;
                Debug.Log($"跳转到表格{prams[0]}");
                Next();
            }

            if (command == Center.Command_Gameover)
            {
                //大地图System.instance.失败();// 打开失败页面
                // 自动保存
                //进度System.存档("读档");
                //已储存剧本 = 记录上段剧情;
                
                DOVirtual.DelayedCall(0.5f ,()=>
                {
                    // 退回到上一段剧情
                    设置新剧本(curBranchName);
                    Next();
                    //刷新();
                }, false);
                大地图System.instance.失败();
            }

            if (key.Contains(Center.Command_If))
            {
                if (key.Contains(Center.Command_Modify)) continue;
                var prams = 指令切割(key);
                if (prams[0] == 已阅读.ToString())
                {
                    Debug.LogError("跳转到的行与当前行相同,可能造成循环");
                }

                Debug.Log($"鉴定{prams[0]}值={prams[1]}");
                if (变量.获取变量(prams[0]) == Convert.ToInt32(prams[1]))
                {
                    已阅读 = Convert.ToInt32(prams[2]) - 2;
                    Debug.Log($"跳转到{prams[2]}");
                    try
                    {
                        进行指令(当前事件);
                    }
                    catch (IndexOutOfRangeException e)
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

            if (key.Contains(Center.Command_Jump))
            {
                var prams = 指令切割(key);
                已阅读 = Convert.ToInt32(prams[0]);
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
                if (GM.Ins.AM != null)
                {
                    if (prams.Length >= 2)
                    {
                        // 检查是否为STOP命令
                        if (prams[1].ToUpper() == "STOP")
                        {
                            GM.Ins.AM.停止音效(prams[0]);
                        }
                        else
                        {
                            int loopParam = Convert.ToInt32(prams[1]);
                            GM.Ins.AM.播放音效(prams[0], loopParam);
                        }
                    }
                    else
                    {
                        GM.Ins.AM.播放音效(prams[0]);
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
                if (GM.Ins.AM != null)
                {
                    if (prams[0] != "STOP")
                    {
                        GM.Ins.AM.播放音乐(prams[0]);
                    }
                    else
                    {
                        GM.Ins.AM.停止音乐();
                    }
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

            if (command.Equals(Center.Command_Battle)) // 进入战斗
            {
                var prams = 指令切割(key);
                if (prams.Length >= 3)
                {
                    string battleScene = prams[0];
                    string endLog = prams[1];
                    int.TryParse(prams[2], out int result);
                    GM.Ins.StartBattle(battleScene, endLog, result);
                }
                else if (prams.Length >= 2)
                {
                    string battleScene = prams[0];
                    string endLog = prams[1];
                    GM.Ins.StartBattle(battleScene, endLog);
                }
                else if (prams.Length >= 1)
                {
                    string battleScene = prams[0];
                    string endLog = "";
                    GM.Ins.StartBattle(battleScene, endLog);
                }
            }

            if (key.Contains(Center.Command_Daytime)) //时间推进
            {
                var prams = 指令切割(key);
                if (prams.Length >= 1)
                {
                    int.TryParse(prams[0], out int dayTimes);
                    大地图System.instance.GetDaytimeSystem()?.CostDaytime(dayTimes);
                }
            }

            if (key.Contains(Center.Command_Cgactive)) //开关CG
            {
                var prams = 指令切割(key);
                if (prams.Length >= 1)
                {
                    int.TryParse(prams[0], out int cgActive);
                    BG.gameObject.SetActive(cgActive == 1);
                    BG_black.gameObject.SetActive(cgActive == 1);
                }
            }

            if (key.Contains(Center.Command_Smallmap)) // 进入二级地图
            {
                var prams = 指令切割(key);
                if (prams.Length >= 2)
                {
                    int.TryParse(prams[0], out int mapID);
                    int.TryParse(prams[1], out int active);
                    大地图System.instance.SmallMapActive(mapID, active == 1);
                    GM.Ins.PLAYERPROFILE.curSmallMapIndex = active == 1 ? mapID : 0;
                }
            }

            if (key.Contains(Center.Command_Save)) // 保存任务节点进度
            {
                var prams = 指令切割(key);
                int.TryParse(prams[1], out int result);
                GM.Ins.PLAYERPROFILE.保存任务进度(prams[0], result);
                // 重新刷新任务节点
                大地图System.instance.RefreshAllNodes();
                // 自动保存
                //大地图System.instance.剧情结束();
                //大地图System.instance.剧情结束();
            }

            if (key.Contains(Center.Command_FullCG)) // 全屏CG
            {
                var prams = 指令切割(key);
                float duration = 2;
                float fadeTime = 1.5f;
                if (prams.Length >= 4)
                {
                    //自定义时长
                    duration = float.Parse(prams[1]);
                    fadeTime = float.Parse(prams[2]);
                    string _str = prams[3];
                    ShowText(_str, duration, fadeTime);
                }
                else if (prams.Length >= 3)
                {
                    //自定义时长
                    duration = float.Parse(prams[1]);
                    fadeTime = float.Parse(prams[2]);
                }
                else if (prams.Length >= 2)
                {
                    //自定义时长
                    duration = float.Parse(prams[1]);
                }
                else if (prams.Length >= 1)
                {
                    duration = 2;
                }
                else
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>("CG/" + prams[0]);

                if (texture == null)
                {
                    Debug.LogError("无法加载图片: " + prams[0]);
                    continue;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height)
                    , new Vector2(0.5f, 0.5f));
                FULLCG.sprite = sprite;
                FULLCG.transform.parent.gameObject.SetActive(true);
                FULLCG.color = new Color(1, 1, 1, 0);
                Sequence sequence = DOTween.Sequence();
                sequence.Append(FULLCG.DOFade(1f, fadeTime));
                sequence.AppendInterval(duration);
                sequence.Append(FULLCG.DOFade(0f, fadeTime));
                sequence.AppendCallback(() => FULLCG.transform.parent.gameObject.SetActive(false));
            }

            if (key.Contains(Center.Command_HalfCG)) // 半屏CG
            {
                var prams = 指令切割(key);
                //float duration =2;
                float fadeTime = CG淡入淡出时间;
                if (prams.Length >= 2)
                {
                    fadeTime = float.Parse(prams[2]);
                }
                else if (prams.Length < 1)
                {
                    continue;
                }

                Texture2D texture = Resources.Load<Texture2D>("CG/" + prams[0]);

                if (texture == null)
                {
                    Debug.LogError("无法加载图片: " + prams[0]);
                    continue;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height)
                    , new Vector2(0.5f, 0.5f));
                HALFCG.sprite = sprite;
                HALFCG.transform.parent.gameObject.SetActive(true);
                HALFCG.color = new Color(1, 1, 1, 0);
                Sequence sequence = DOTween.Sequence();
                sequence.Append(HALFCG.DOFade(1f, fadeTime));
            }

            if (key.Contains(Center.Command_Shop))
            {
                //Debug.LogError("打开商城");
                var prams = 指令切割(key);
                int result = 0;
                int discount = 100; // 默认没有折扣
                if (prams.Length >= 2)
                {
                    int.TryParse(prams[0], out result);
                    int.TryParse(prams[1], out discount);
                }
                else if (prams.Length >= 1)
                {
                    int.TryParse(prams[0], out result);
                }

                // 显示商店面板
                GM.Ins.marketSystem.OpenMarketPanel(result, discount);
            }

            if (key.Contains(Center.Command_Close))
            {
                EndReadingStory();
                this.gameObject.SetActive(false);
            }

            if (key.Contains(Center.Command_Chapter))
            {
                var prams = 指令切割(key);
                if (prams.Length >= 3)
                {
                    string title = prams[0];
                    // EXC 文件名确定的章节优先；其它剧本仍使用原来的章节指令。
                    if (PLAYERPROFILE.GetChapterNumberFromScript(GM.Ins.PLAYERPROFILE.currentScriptFile) == 0)
                    {
                        GM.Ins.PLAYERPROFILE.chapterTitle = title;
                        // Chapter maps use names such as CHAPTER-1.
                        var chapterMatch = Regex.Match(GM.Ins.PLAYERPROFILE.currentMap ?? "", @"CHAPTER-(\d+)");
                        if (chapterMatch.Success && int.TryParse(chapterMatch.Groups[1].Value, out int chapterNumber))
                            GM.Ins.PLAYERPROFILE.chapterNumber = chapterNumber;
                        else if (GM.Ins.PLAYERPROFILE.chapterNumber == 0)
                            GM.Ins.PLAYERPROFILE.chapterNumber = 1;
                    }
                    //int.TryParse(prams[0], out int result);
                    float.TryParse(prams[1], out float fadeTime);
                    float.TryParse(prams[1], out float duration);
                    大地图System.instance.chapterPanel.ShowChapter(title, fadeTime, duration);
                    GM.Ins.AM.播放音效("chapter-change",1);
                }
            }

            if (key.Contains(Center.Command_Logs))
            {
                // LOGS(世界崩塌;毁灭殆尽;万物终结,2,1)
                var prams = 指令切割(key);
                if (prams.Length >= 3)
                {
                    string[] title = prams[0].Split(";");
                    //int.TryParse(prams[0], out int result);
                    float.TryParse(prams[1], out float fadeTime);
                    float.TryParse(prams[1], out float duration);
                    大地图System.instance.chapterPanel.ShowChapters(title, fadeTime, duration);
                    GM.Ins.AM.播放音效("day-change",1);
                }
            }

            if (key.Contains(Center.Command_Tutorial))
            {
                var prams = 指令切割(key);
                if (prams.Length >= 1)
                {
                    if (大地图System.instance != null)
                    {
                        大地图System.instance.tutorial.Show(prams[0]);
                    }
                    else if (BattleScene.Ins!=null)
                    {
                        BattleScene.Ins.BM.tutorialManager.tutorialPanel.Show(prams[0]);
                    }
                }
            }

            if (key.Contains(Center.Command_Map))
            {
                // 
                var prams = 指令切割(key);
                if (prams.Length >= 1)
                {
                    GM.Ins.PLAYERPROFILE.daytime = Daytime.上午;// 进入地图重置为初始
                    大地图System.instance.打开地图(prams[0]);
                }
            }

            if (key.Contains(Center.Command_Item)) // +ITEM(int mode, int itemId, int count)
            {
                var prams = 指令切割(key);
                if (prams.Length >= 3)
                {
                    int.TryParse(prams[0], out int mode);
                    int.TryParse(prams[1], out int itemId);
                    int.TryParse(prams[2], out int count);

                    if (mode == 0)
                    {
                        // 获得物品
                        GM.Ins.PLAYERPROFILE.AddItem((ItemName)itemId, count);
                        大地图System.instance.notificationManager.PushNotification(new ItemPack(
                            (ItemName)itemId
                            , count));
                        /*大地图System.instance.itemGetPanel.ShowPanel(new ItemPack((ItemName)itemId
                            , count));*/
                    }
                    else if (mode == 1)
                    {
                        // 消耗物品
                        GM.Ins.PLAYERPROFILE.CostItem((ItemName)itemId, count);
                    }
                }
            }

            if (key.Contains(Center.Command_PlayerAd))
            {
                for (var index = 0; index < 3; index++)
                {
                    var player = GM.Ins.PLAYERPROFILE.player[index];
                    player.spriteName = $"PC0{index + 1}01";
                    GM.Ins.PLAYERPROFILE.dateYear = 1573;//6年后
                    GM.Ins.PLAYERPROFILE.dateMonth = 3;
                    GM.Ins.PLAYERPROFILE.daytime = Daytime.上午;
                    
                    大地图System.instance.GetDaytimeSystem()?.UpdateDaytimeImage();
                    player.curHealth += 50;
                }
            }

            if (key.Contains(Center.Command_LoadScene))
            {
                // 跳转场景
                var prams = 指令切割(key);
                if (prams.Length >= 1)
                {
                    string sceneName = prams[0];
                    EndReadingStory();
                    GM.Ins.LoadScene(sceneName);
                }
            }

            if (key.Contains(Center.Command_Finish))
            {
                EndReadingStory();
                LocalGameProgress.MarkGameFinished();
                大地图System.instance.endingCreditsPlayer.Play();
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

        if (match.Groups[1].Value == "")
        {
            Debug.LogError($"{command}指令格式错误，请检查是否有参数");
        }
        else
        {
            Debug.LogError($"{command}指令格式错误，请检查是否有括号");
        }

        return null;
    }

    public static string GetCommand(string command)
    {
        // 匹配开头任意非括号字符，并在后面可能有括号包裹的内容
        var match = Regex.Match(command, @"^([^(\s]+)(?:\(.*?\))?");
    
        if (match.Success)
        {
            // Groups[1] 对应的是第一个括号 ([^(\s]+) 捕获到的纯指令内容
            return match.Groups[1].Value;
        }

        return command; // 如果完全没匹配到，安全起见返回原字符串
    }

}
