# AudioManager 音频管理器使用说明

## 概述

AudioManager 是一个用于管理游戏音乐（BGM）和音效（SE）的单例管理器。

## 功能特点

### 1. 音乐（BGM）播放
- 同时只能播放一个BGM
- 播放新的BGM会自动停止当前播放的BGM
- BGM会循环播放
- 受BGM音量控制

### 2. 音效（SE）播放
- 可以同时播放多个音效
- 音效播放完毕后自动销毁
- 受SE音量控制

## 设置步骤

### 1. 在场景中添加AudioManager

1. 在Unity场景中创建一个空GameObject
2. 命名为 "AudioManager"
3. 添加 `AudioManager.cs` 脚本组件
4. 设置初始音量（可选）：
   - BGM音量：0-1之间的浮点数（默认1.0）
   - SE音量：0-1之间的浮点数（默认1.0）

### 2. 准备音频文件

1. 将音频文件放入 `Assets/StreamingAssets/SOUND/` 文件夹
2. 支持的格式：
   - .wav（推荐）
   - .mp3
   - .ogg

**示例文件结构：**
```
Assets/StreamingAssets/SOUND/
├── bgm_main.wav
├── bgm_battle.wav
├── sfx_click.wav
└── sfx_explosion.wav
```

## 剧本中使用

### 播放音乐
```
MUSIC(bgm_main)
```
- 参数：音频文件名（不含扩展名）
- 效果：播放指定的背景音乐，循环播放

### 播放音效

**播放一次（默认）：**
```
SOUND(sfx_click)
```
- 参数：音频文件名（不含扩展名）
- 效果：播放指定的音效，播放一次

**循环播放：**
```
SOUND(ambient_rain,1)
```
- 参数1：音频文件名（不含扩展名）
- 参数2：1 表示循环播放
- 效果：循环播放该音效，直到被停止
- 注意：如果该音效已经在循环播放，会先停止旧的再播放新的

**智能停止/播放：**
```
SOUND(ambient_rain,0)
```
- 参数1：音频文件名（不含扩展名）
- 参数2：0 表示智能处理
- 效果：
  - 如果该音效正在循环播放：停止循环，让它自然播放完当前这一次（淡出效果）
  - 如果该音效未在循环播放：播放一次该音效
- 使用场景：让循环音效平滑结束，或确保音效至少播放一次

### 组合使用
```
CG(scene1)+MUSIC(bgm_battle)+SPEAK(character1)
```
可以使用 `+` 符号组合多个命令

## 代码中使用

### 播放音乐
```csharp
AudioManager.instance.播放音乐("bgm_main");
```

### 播放音效
```csharp
// 播放一次
AudioManager.instance.播放音效("sfx_click");

// 循环播放
AudioManager.instance.播放音效("ambient_rain", 1);

// 停止循环音效
AudioManager.instance.播放音效("ambient_rain", 0);
```

### 停止音乐
```csharp
AudioManager.instance.停止音乐();
```

### 停止音效
```csharp
AudioManager.instance.停止音效("ambient_rain");
```

### 设置音量
```csharp
// 设置BGM音量
AudioManager.instance.设置BGM音量(0.5f);

// 设置SE音量
AudioManager.instance.设置SE音量(0.8f);
```

## 注意事项

1. **音频文件命名**
   - 文件名不要包含空格和特殊字符
   - 使用英文或数字命名
   - 示例：bgm_main, sfx_01, music_battle

2. **文件格式兼容性**
   - WAV格式在所有平台都有良好支持
   - MP3在某些独立平台可能不支持
   - OGG在移动平台可能不支持

3. **性能优化**
   - 音效文件应保持较小的大小
   - 避免同时播放过多音效

4. **AudioManager初始化**
   - AudioManager会在场景加载时自动初始化
   - 使用 DontDestroyOnLoad 确保在场景切换时保持
   - 只能有一个AudioManager实例

## 调试

如果音频无法播放，请检查：

1. AudioManager是否已添加到场景中
2. 音频文件是否放在正确的位置（StreamingAssets/SOUND/）
3. 文件名是否正确（不包含扩展名）
4. Unity控制台是否有错误信息
5. 音量是否设置为0

## 示例剧本

### 基础示例
```
CG(opening)+MUSIC(bgm_menu)
旁白:欢迎来到游戏
SOUND(sfx_welcome)
角色A:你好！
SOUND(sfx_greeting)
```

这个示例会：
1. 更改背景图为opening
2. 播放bgm_menu音乐（循环）
3. 显示旁白时播放欢迎音效
4. 显示角色对话时播放问候音效

### 循环音效示例
```
CG(forest)+SOUND(ambient_birds,1)
旁白:你来到了森林中，鸟儿在歌唱
角色A:这里真美！
SOUND(ambient_wind,1)
旁白:风开始吹了起来
角色A:我们该离开了
SOUND(ambient_birds,0)+SOUND(ambient_wind,0)
```

这个示例会：
1. 更改背景图为forest
2. 开始循环播放鸟叫声音效
3. 开始循环播放风声音效（与鸟叫声同时播放）
4. 离开森林时，鸟叫和风声会停止循环，但会自然播放完当前这一次（平滑过渡）

### 智能音效示例
```
SOUND(door_open,1)
旁白:门被打开了，发出吱呀的声音
SOUND(door_open,0)
旁白:门最终停止了晃动
```

在这个例子中：
- 第一次 `SOUND(door_open,1)` 会让门声循环播放
- 第二次 `SOUND(door_open,0)` 会停止循环，但让当前这一次播放完，听起来更自然

