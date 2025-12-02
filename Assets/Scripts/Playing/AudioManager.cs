using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;
    public float BGM音量 => Options.Volume_BGM;
    public float SE音量 => Options.Volume_SE; 
    
    private AudioSource bgmAudioSource;  
    private GameObject seAudioSourcePool;
    
    private Dictionary<string, GameObject> 循环音效字典 = new Dictionary<string, GameObject>();
    private Dictionary<string, List<GameObject>> 所有音效字典 = new Dictionary<string, List<GameObject>>();
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
        seAudioSourcePool = new GameObject("SE音效池");
        seAudioSourcePool.transform.SetParent(transform);
    }

    public void 播放音乐(string audioName)
    {
        StartCoroutine(加载并播放BGM(audioName));
    }

    public void 播放按键音(string audioname)
    {
        播放音效(audioname);
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
            if (循环音效字典.ContainsKey(audioName))
            {
                停止循环让其播放完(audioName);
            }
            else
            {
                StartCoroutine(加载并播放SE(audioName));
            }
        }
        else if (loop == 1)
        {
            StartCoroutine(加载并播放循环SE(audioName));
        }
        else
        {
            StartCoroutine(加载并播放SE(audioName));
        }
    }
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
                    seSource.loop = false;
                    循环音效字典.Remove(audioName);
                    // 计算剩余播放时间并在播放完后销毁
                    float remainingTime = seSource.clip.length - seSource.time;
                    StartCoroutine(等待播放完成并清理(audioName, seObject, remainingTime + 0.1f));
                }
                else
                {
                    // 如果不在播放，立即清理
                    if (所有音效字典.ContainsKey(audioName) && 所有音效字典[audioName].Contains(seObject))
                    {
                        所有音效字典[audioName].Remove(seObject);
                        if (所有音效字典[audioName].Count == 0)
                        {
                            所有音效字典.Remove(audioName);
                        }
                    }
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

    public void 停止音效(string audioName)
    {
        // 停止循环音效
        if (循环音效字典.ContainsKey(audioName))
        {
            GameObject seObject = 循环音效字典[audioName];
            if (seObject != null)
            {
                Destroy(seObject);
            }
            循环音效字典.Remove(audioName);
        }
        
        // 停止所有正在播放的该音效（包括只播放一次的）
        if (所有音效字典.ContainsKey(audioName))
        {
            List<GameObject> seObjects = new List<GameObject>(所有音效字典[audioName]);
            foreach (GameObject seObject in seObjects)
            {
                if (seObject != null)
                {
                    AudioSource seSource = seObject.GetComponent<AudioSource>();
                    if (seSource != null && seSource.isPlaying)
                    {
                        seSource.Stop();
                    }
                    Destroy(seObject);
                }
            }
            所有音效字典.Remove(audioName);
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
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = clip;
            bgmAudioSource.volume = BGM音量;
            bgmAudioSource.Play();
        }
        else
        {
            Debug.LogError($"无法加载BGM: {audioName}");
        }
    }

    private IEnumerator 加载并播放SE(string audioName)
    {
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            GameObject seObject = new GameObject($"SE_{audioName}");
            seObject.transform.SetParent(seAudioSourcePool.transform);
            
            AudioSource seSource = seObject.AddComponent<AudioSource>();
            seSource.clip = clip;
            seSource.volume = SE音量;
            seSource.loop = false;
            seSource.playOnAwake = false;
            seSource.Play();
            
            // 添加到跟踪字典
            if (!所有音效字典.ContainsKey(audioName))
            {
                所有音效字典[audioName] = new List<GameObject>();
            }
            所有音效字典[audioName].Add(seObject);
            
            // 播放完成后从字典中移除并销毁
            StartCoroutine(等待播放完成并清理(audioName, seObject, clip.length + 0.1f));
        }
        else
        {
            Debug.LogWarning($"无法加载音效: {audioName}");
        }
    }
    
    private IEnumerator 等待播放完成并清理(string audioName, GameObject seObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (所有音效字典.ContainsKey(audioName) && seObject != null)
        {
            所有音效字典[audioName].Remove(seObject);
            if (所有音效字典[audioName].Count == 0)
            {
                所有音效字典.Remove(audioName);
            }
            
            if (seObject != null)
            {
                Destroy(seObject);
            }
        }
    }
    
    private IEnumerator 加载并播放循环SE(string audioName)
    {
        if (循环音效字典.ContainsKey(audioName))
        {
            停止音效(audioName);
        }
        AudioClip clip = null;
        yield return StartCoroutine(加载音频文件(audioName, (loadedClip) => { clip = loadedClip; }));

        if (clip != null)
        {
            GameObject seObject = new GameObject($"SE_Loop_{audioName}");
            seObject.transform.SetParent(seAudioSourcePool.transform);
            
            AudioSource seSource = seObject.AddComponent<AudioSource>();
            seSource.clip = clip;
            seSource.volume = SE音量;
            seSource.loop = true;
            seSource.playOnAwake = false;
            seSource.Play();
            循环音效字典[audioName] = seObject;
            
            // 也添加到所有音效字典中
            if (!所有音效字典.ContainsKey(audioName))
            {
                所有音效字典[audioName] = new List<GameObject>();
            }
            所有音效字典[audioName].Add(seObject);
        }
        else
        {
            Debug.LogWarning($"无法加载循环音效: {audioName}");
        }
    }

    private IEnumerator 加载音频文件(string audioName, System.Action<AudioClip> callback)
    {
        string soundFolder = Path.Combine(Application.streamingAssetsPath, "SOUND");
        string[] extensions = { ".wav", ".mp3", ".ogg" };
        
        foreach (string ext in extensions)
        {
            string filePath = Path.Combine(soundFolder, audioName + ext);
            if (File.Exists(filePath))
            {
                AudioType audioType = GetAudioType(ext);
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

