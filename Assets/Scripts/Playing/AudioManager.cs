using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AudioManager : SerializedMonoBehaviour
{
    public float BGM音量 => GM.Ins.DM.settingsData.bgmVolume;
    public float SE音量  => GM.Ins.DM.settingsData.sfxVolume;
    
    [Title("核心配置")]
    public AudioConfig audioConfig;
    public AudioMixer audioMixer;

    [Title("运行时状态 (只读观察)")]
    [ShowInInspector, ReadOnly]
    private AudioSource bgmAudioSource;
    private GameObject seAudioSourcePool;
    
    // 【优化 1：Addressables 内存缓存】避免重复加载，解决 AudioClip 内存泄漏
    private Dictionary<string, AsyncOperationHandle<AudioClip>> _audioCache = new Dictionary<string, AsyncOperationHandle<AudioClip>>();

    // 【优化 2：简单的对象池】彻底消除 new GameObject 和 Destroy 带来的 GC
    private List<AudioSource> _sePool = new List<AudioSource>();
    private int _initPoolSize = 15; // 初始池大小

    // 【优化 3：记录循环音效】直接记录 AudioSource 引用，抛弃复杂的 List<GameObject>
    private Dictionary<string, AudioSource> 循环音效字典 = new Dictionary<string, AudioSource>();

    // 【引入方案：时间戳拦截法】解决瞬间“播放又停止”导致的竞态残留
    private Dictionary<string, float> lastStopTimeDict = new Dictionary<string, float>();
    private float lastStopAllTime = 0f;

    public void 初始化()
    {
        // 初始化 BGM
        bgmAudioSource = gameObject.AddComponent<AudioSource>();
        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;
        
        // 初始化 SE 对象池
        seAudioSourcePool = new GameObject("SE音效池");
        seAudioSourcePool.transform.SetParent(transform);
        
        for (int i = 0; i < _initPoolSize; i++)
        {
            CreateNewAudioSourceToPool(i);
        }
    }

    /// <summary>
    /// 从对象池获取一个空闲的 AudioSource
    /// </summary>
    private AudioSource GetAvailableSESource()
    {
        foreach (var source in _sePool)
        {
            // 只要没有在播放，就认为它是空闲可复用的 (无需 Destroy)
            if (!source.isPlaying) 
                return source;
        }
        
        // 如果池子不够用，动态扩容
        return CreateNewAudioSourceToPool(_sePool.Count);
    }

    private AudioSource CreateNewAudioSourceToPool(int index)
    {
        GameObject go = new GameObject($"SE_Source_{index}");
        go.transform.SetParent(seAudioSourcePool.transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        _sePool.Add(source);
        return source;
    }

    #region 公开播放接口

    public void PlayAudio(AudioCueType audioCueType, int loop = -1)
    {
        string audioName = audioConfig.GetAudioPath(audioCueType);
        播放音效(audioName, loop);
    }

    public void 播放音乐(string audioName)
    {
        加载并播放(audioName, true, false);
    }

    public void 播放按键音(string audioname)
    {
        播放音效(audioname);
    }

    public void 播放音效(string audioName, int loop = -1)
    {
        if (loop == 0) // 停止循环，但让当前这遍播放完
        {
            if (循环音效字典.ContainsKey(audioName))
                停止循环让其播放完(audioName);
            else
                加载并播放(audioName, false, false);
        }
        else if (loop == 1) // 循环播放
        {
            加载并播放(audioName, false, true);
        }
        else // 播放一次 (默认)
        {
            加载并播放(audioName, false, false);
        }
    }

    #endregion

    #region 核心加载与播放逻辑 (Addressables + 时间戳拦截)

    /// <summary>
    /// 统一的加载并播放入口，使用回调和时间戳拦截代替协程
    /// </summary>
    private void 加载并播放(string audioName, bool isBGM, bool isLoopSE)
    {
        if (string.IsNullOrWhiteSpace(audioName)) return;

        // 记录发起播放请求的时间
        float requestTime = Time.unscaledTime;

        // 加载完毕后的实际执行逻辑 (闭包回调)
        System.Action<AudioClip> onReadyToPlay = (clip) =>
        {
            // 【核心拦截】：如果加载期间，触发了全局停止，或者停止了该特定音效，直接丢弃[cite: 2]
            if (lastStopAllTime > requestTime) return;
            if (lastStopTimeDict.TryGetValue(audioName, out float stopTime) && stopTime > requestTime) return;

            if (isBGM)
            {
                bgmAudioSource.Stop();
                bgmAudioSource.clip = clip;
                bgmAudioSource.volume = BGM音量;
                bgmAudioSource.Play();
            }
            else
            {
                // 如果是循环音效，先停掉之前的
                if (isLoopSE) 停止音效(audioName);

                // 从对象池取出一个组件播放
                AudioSource seSource = GetAvailableSESource();
                seSource.clip = clip;
                seSource.volume = SE音量;
                seSource.loop = isLoopSE;
                seSource.Play();

                if (isLoopSE) 循环音效字典[audioName] = seSource;
                
                // 注意：播放单次 SE 再也不需要开启协程去等待它播放完毕然后清理了！
                // 因为我们的对象池逻辑只认 `!source.isPlaying`，它播完自动就变成了可用状态！
            }
        };

        // 执行 Addressables 加载
        LoadAudioClipAsync(audioName, onReadyToPlay);
    }

    private void LoadAudioClipAsync(string audioName, System.Action<AudioClip> onComplete)
    {
        // 1. 检查缓存，如果已经加载过，直接返回结果，0 延迟
        if (_audioCache.TryGetValue(audioName, out var handle))
        {
            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(handle.Result);
            }
            else // 正在加载中，追加回调
            {
                handle.Completed += h => { if (h.Status == AsyncOperationStatus.Succeeded) onComplete?.Invoke(h.Result); };
            }
            return;
        }

        // 2. 首次加载
        var newHandle = Addressables.LoadAssetAsync<AudioClip>(audioName);
        _audioCache[audioName] = newHandle;
        
        newHandle.Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded)
            {
                onComplete?.Invoke(h.Result);
            }
            else
            {
                Debug.LogWarning($"[AudioManager] 无法通过 Addressables 加载音效: {audioName}");
                _audioCache.Remove(audioName);
            }
        };
    }

    #endregion

    #region 停止与控制逻辑

    private void 停止循环让其播放完(string audioName)
    {
        if (循环音效字典.TryGetValue(audioName, out AudioSource source))
        {
            if (source != null && source.isPlaying)
            {
                // 直接取消循环即可，对象池会自动在它播完后将其回收，不再需要开启等待协程！
                source.loop = false; 
            }
            循环音效字典.Remove(audioName);
        }
    }

    public void 停止音效(string audioName)
    {
        // 记录停止指令的最新时间[cite: 2]
        lastStopTimeDict[audioName] = Time.unscaledTime;

        // 1. 停止循环字典中的引用
        if (循环音效字典.TryGetValue(audioName, out AudioSource loopSource))
        {
            if (loopSource != null) loopSource.Stop();
            循环音效字典.Remove(audioName);
        }

        // 2. 停止对象池中所有正在播放该音效的组件 (包括单次播放的)
        if (_audioCache.TryGetValue(audioName, out var handle) && handle.IsDone)
        {
            AudioClip targetClip = handle.Result;
            foreach (var source in _sePool)
            {
                if (source.isPlaying && source.clip == targetClip)
                {
                    source.Stop();
                    source.clip = null; // 释放池内引用
                }
            }
        }
    }

    public void StopAll()
    {
        // 记录全局停止指令的时间[cite: 2]
        lastStopAllTime = Time.unscaledTime;

        foreach (var source in _sePool)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
        }
        循环音效字典.Clear();
    }
    
    public void 停止音乐()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        { 
            bgmAudioSource.Stop();
        }
    }

    #endregion

    private void OnDestroy()
    {
        // 释放 Addressables 内存
        foreach (var kvp in _audioCache)
        {
            if (kvp.Value.IsValid())
            {
                Addressables.Release(kvp.Value);
            }
        }
        _audioCache.Clear();
    }
    
    
    /// <summary>
    /// 直接传入 AudioClip 播放音效 (已整合进对象池，0 GC)
    /// </summary>
    /// <param name="clip">要播放的音频切片</param>
    public void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        // 直接从我们写好的对象池中取出一个空闲的 AudioSource
        AudioSource seSource = GetAvailableSESource();
        
        seSource.clip = clip;
        seSource.volume = SE音量;
        seSource.loop = false;
        seSource.Play();
    }
}