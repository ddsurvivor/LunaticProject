using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public float BGM音量 => Options.Volume_BGM; // BGM音量
    public float SE音量 => Options.Volume_SE;  // SE音量
    
    private AudioSource bgmAudioSource;  
    private GameObject seAudioSourcePool;
    
    // 存储循环播放的音效 <音效名称, AudioSource对象>
    private Dictionary<string, GameObject> 循环音效字典 = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            初始化();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void 初始化()
    {
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.volume = BGM音量;
        // 创建SE播放器池容器
        seAudioSourcePool = new GameObject("SE音效池");
        seAudioSourcePool.transform.SetParent(transform);
    }

    public void 播放音乐(string audioName)
    {
        StartCoroutine(加载并播放BGM(audioName));
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioName">音频文件名</param>
    /// <param name="loop">是否循环播放。0=停止循环/播放一次，1=循环播放，默认=播放一次</param>
    public void 播放音效(string audioName, int loop = -1)
    {
        if (loop == 0)
        {
            // 检查是否有循环版本在播放
            if (循环音效字典.ContainsKey(audioName))
            {
                // 如果有循环版本，停止循环但让它自然播放完
                停止循环让其播放完(audioName);
            }
            else
            {
                // 如果没有循环版本，播放一次
                StartCoroutine(加载并播放SE(audioName));
            }
        }
        else if (loop == 1)
        {
            // 循环播放音效
            StartCoroutine(加载并播放循环SE(audioName));
        }
        else
        {
            // 默认：播放一次
            StartCoroutine(加载并播放SE(audioName));
        }
    }
    
    /// <summary>
    /// 停止循环，但让音效自然播放完当前这一次
    /// </summary>
    private void 停止循环让其播放完(string audioName)
    {
        if (循环音效字典.ContainsKey(audioName))
        {
            GameObject seObject = 循环音效字典[audioName];
            if (seObject != null)
            {
                AudioSource seSource = seObject.GetComponent<AudioSource>();
                if (seSource != null && seSource.isPlaying)
                {
                    // 停止循环
                    seSource.loop = false;
                    Debug.Log($"停止循环音效(将自然结束): {audioName}");
                    
                    // 从字典中移除
                    循环音效字典.Remove(audioName);
                    
                    // 计算剩余播放时间并在播放完后销毁
                    float remainingTime = seSource.clip.length - seSource.time;
                    Destroy(seObject, remainingTime + 0.1f);
                }
                else
                {
                    // 如果没在播放，直接销毁
                    Destroy(seObject);
                    循环音效字典.Remove(audioName);
                }
            }
            else
            {
                循环音效字典.Remove(audioName);
            }
        }
    }
    
    /// <summary>
    /// 立即停止指定的循环音效
    /// </summary>
    public void 停止音效(string audioName)
    {
        if (循环音效字典.ContainsKey(audioName))
        {
            GameObject seObject = 循环音效字典[audioName];
            if (seObject != null)
            {
                Destroy(seObject);
                Debug.Log($"立即停止循环音效: {audioName}");
            }
            循环音效字典.Remove(audioName);
        }
    }
    
    public void 停止音乐()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        { 
            bgmAudioSource.Stop();
        }
    }

    private IEnumerator 加载并播放BGM(string audioName)
    {
        // 尝试加载音频文件
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            // 停止当前BGM并播放新的
            bgmAudioSource.Stop();
            bgmAudioSource.clip = clip;
            bgmAudioSource.volume = BGM音量;
            bgmAudioSource.Play();
            Debug.Log($"开始播放BGM: {audioName}");
        }
        else
        {
            Debug.LogWarning($"无法加载BGM: {audioName}");
        }
    }

    private IEnumerator 加载并播放SE(string audioName)
    {
        // 尝试加载音频文件
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            // 创建临时AudioSource播放音效
            GameObject seObject = new GameObject($"SE_{audioName}");
            seObject.transform.SetParent(seAudioSourcePool.transform);
            
            AudioSource seSource = seObject.AddComponent<AudioSource>();
            seSource.clip = clip;
            seSource.volume = SE音量;
            seSource.loop = false;
            seSource.playOnAwake = false;
            seSource.Play();

            Debug.Log($"播放音效: {audioName}");

            // 播放完毕后销毁
            Destroy(seObject, clip.length + 0.1f);
        }
        else
        {
            Debug.LogWarning($"无法加载音效: {audioName}");
        }
    }
    
    private IEnumerator 加载并播放循环SE(string audioName)
    {
        // 如果该音效已经在循环播放，先停止它
        if (循环音效字典.ContainsKey(audioName))
        {
            停止音效(audioName);
        }
        
        // 尝试加载音频文件
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            // 创建循环播放的AudioSource
            GameObject seObject = new GameObject($"SE_Loop_{audioName}");
            seObject.transform.SetParent(seAudioSourcePool.transform);
            
            AudioSource seSource = seObject.AddComponent<AudioSource>();
            seSource.clip = clip;
            seSource.volume = SE音量;
            seSource.loop = true; // 循环播放
            seSource.playOnAwake = false;
            seSource.Play();

            Debug.Log($"循环播放音效: {audioName}");

            // 添加到字典中以便后续管理
            循环音效字典[audioName] = seObject;
        }
        else
        {
            Debug.LogWarning($"无法加载循环音效: {audioName}");
        }
    }

    private IEnumerator 加载音频文件(string audioName, System.Action<AudioClip> callback)
    {
        string soundFolder = Path.Combine(Application.streamingAssetsPath, "SOUND");
        
        // 尝试不同的文件扩展名
        string[] extensions = { ".wav", ".mp3", ".ogg" };
        
        foreach (string ext in extensions)
        {
            string filePath = Path.Combine(soundFolder, audioName + ext);
            
            // 检查文件是否存在
            if (File.Exists(filePath))
            {
                AudioType audioType = GetAudioType(ext);
                
                // 使用UnityWebRequest加载音频
                using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + filePath, audioType))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                        callback?.Invoke(clip);
                        yield break;
                    }
                    else
                    {
                        Debug.LogWarning($"加载音频失败: {filePath}, 错误: {www.error}");
                    }
                }
            }
        }
        Debug.LogError($"找不到音频文件: {audioName} (已尝试 .wav, .mp3, .ogg)");
        callback?.Invoke(null);
    }

    private AudioType GetAudioType(string extension)
    {
        switch (extension.ToLower())
        {
            case ".wav":
                return AudioType.WAV;
            case ".mp3":
                return AudioType.MPEG;
            case ".ogg":
                return AudioType.OGGVORBIS;
            default:
                return AudioType.UNKNOWN;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

