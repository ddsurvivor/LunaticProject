# 片尾字幕模块

脚本：Assets/Scripts/Playing/EndingCreditsPlayer.cs。使用项目现有的 UGUI Text，无需依赖 GM。

## 场景配置

在 Canvas 下创建以下层级（从上到下对应绘制顺序）：

```
EndingCreditsPanel（挂 EndingCreditsPlayer、专属 VideoPlayer）
├── Background（UI Image，铺满）
├── Video（UI RawImage，铺满）
└── Viewport（RectTransform + RectMask2D，铺满）
    └── CreditsText（旧版 UI Text，使用支持中文的字体）
```

将对应组件拖入脚本的 Viewport、Credits Text、Background Image、Video Image、Video Player。
这些物体保持激活；背景显隐由脚本控制组件 enabled。字幕和背景的颜色 Alpha 应大于 0。
Viewport 与 Text 的 localScale 设为 (1,1,1)，旋转归零。Text 水平拉伸，可通过 Left/Right 设置留白。
不要在 Viewport/Text 上添加 LayoutGroup、ContentSizeFitter 或 ScrollRect：脚本管理文本高度和滚动位置。
Canvas 推荐使用 Scale With Screen Size。字体、字号、行距和富文本支持在 Text 上设置。
这是自动滚动模块，无需 EventSystem；如增加可点击按钮，则 Canvas 需要 GraphicRaycaster，场景需要 EventSystem。

## 配置

- Credits：直接输入多行字幕；Text Asset 可指定 UTF-8 .txt 文件，配置后优先使用文件。
- Scroll Speed：每秒移动的 Canvas UI 单位，默认 60。
- Edge Padding：开场文本与视口底部、结束文本与视口顶部的距离。
- Play On Enable：面板每次启用时从头播放，默认开启。
- Background Mode：None / Image / Video。图片使用 Background Sprite；视频使用 Video Clip。
- 视频循环默认开启，字幕滚动结束决定模块结束。视频只作背景，加速字幕不会加速视频。
- 视频准备期间或失败时显示已配置的图片背景，字幕照常滚动。
- 使用专属 VideoPlayer，关闭其 Play On Awake；视频音轨、AudioSource 与音量在该组件上配置。
- 视频使用自动管理的 1920×1080 RenderTexture，推荐 16:9 视频显示区域。
- 默认按住 LeftShift 以 4 倍速滚动，可修改按键和倍率或禁用。
- 默认 F10 跳过；Debug Build Only 默认开启，正式发布版本不响应调试按键。
- 使用 unscaledDeltaTime，即使 Time.timeScale 为 0 也能播放。

## 代码和事件

```csharp
creditsPlayer.SetCredits("制作人员\n\n感谢游玩");
creditsPlayer.Play();
creditsPlayer.SetFastForward(true);  // 屏幕按钮按下
creditsPlayer.SetFastForward(false); // 屏幕按钮松开
creditsPlayer.Skip();                // 外部主动跳过，不受调试按键开关限制
creditsPlayer.Stop();                // 取消播放，不触发结束事件
```

On Finished 在自然结束或跳过时触发一次，bool 参数表示是否跳过。可在 Inspector 绑定关闭面板、
返回主菜单等方法，或使用 OnFinished.AddListener 注册动态 bool 回调。
结束后保留面板和图片背景，接收方负责关闭面板或切换场景。隐藏面板会停止视频并释放纹理；
重新播放从头开始。SetCredits 在下一次 Play 时生效。

## Play Mode 验收

1. 输入多行中文及空行：从下方进入，最后一行完全离开顶部后只触发一次结束事件。
2. 按住/松开 Shift：加速/恢复；将 Time.timeScale 设为 0 仍正常滚动。
3. F10 跳过触发 true，自然结束触发 false；Stop 和隐藏面板不触发事件。
4. 图片背景、视频背景、缺少视频引用分别播放；视频配置错误时字幕继续。
5. 视频准备期间隐藏面板、播放中重复 Play、重新显示面板：从头播放，无遗留视频音频。
6. 改变窗口尺寸，检查换行、滚动终点及上下遮罩。
7. 非 Development Build 中确认默认 F10 不可跳过。