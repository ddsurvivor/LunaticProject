#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Collections.Generic;
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

/// <summary>剧情成就统一入口；队列跨场景保留，提交失败时在本次运行中重试。</summary>
public sealed class SteamAchievements : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    private const float RetrySeconds = 60f;
    private static SteamAchievements instance;
    private readonly HashSet<string> pending = new HashSet<string>();
    private readonly HashSet<string> confirmed = new HashSet<string>();
    private readonly List<string> submitting = new List<string>();
    private Callback<UserStatsStored_t> statsStored;
    private float nextAttempt;
    private bool waitingForStore;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }
#endif

    public static void Unlock(string apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName)) return;
#if !DISABLESTEAMWORKS
        if (instance == null)
            instance = new GameObject(nameof(SteamAchievements)).AddComponent<SteamAchievements>();

        apiName = apiName.Trim();
        if (!instance.confirmed.Contains(apiName) && instance.pending.Add(apiName))
            Debug.Log($"[SteamAchievements] 等待提交成就：{apiName}");
#endif
    }

#if !DISABLESTEAMWORKS
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (pending.Count == 0 || Time.realtimeSinceStartup < nextAttempt) return;
        nextAttempt = Time.realtimeSinceStartup + RetrySeconds;

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamAchievements] Steam 未初始化，成就保留在本次运行的队列中；请检查 Steam 客户端及 App ID。");
            return;
        }

        if (statsStored == null)
            statsStored = Callback<UserStatsStored_t>.Create(OnStatsStored);

        // 超时后重新提交；不要仅凭 GetAchievement 的本地缓存就认为已上传成功。
        waitingForStore = false;
        submitting.Clear();
        foreach (string apiName in pending)
        {
            if (SteamUserStats.SetAchievement(apiName))
                submitting.Add(apiName);
            else
                Debug.LogWarning($"[SteamAchievements] 设置失败：{apiName}。请检查 Steam 后台 API Name 和配置发布状态，将稍后重试。");
        }

        if (submitting.Count == 0) return;
        waitingForStore = SteamUserStats.StoreStats();
        if (!waitingForStore)
            Debug.LogWarning("[SteamAchievements] StoreStats 未成功发起，将在 60 秒后重试。");
    }

    private void OnStatsStored(UserStatsStored_t result)
    {
        if (!waitingForStore || result.m_nGameID != SteamUtils.GetAppID().m_AppId) return;
        waitingForStore = false;

        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogWarning($"[SteamAchievements] Steam 提交失败：{result.m_eResult}，将稍后重试。");
            return;
        }

        foreach (string apiName in submitting)
        {
            pending.Remove(apiName);
            confirmed.Add(apiName);
            Debug.Log($"[SteamAchievements] Steam 成就提交成功：{apiName}");
        }
        submitting.Clear();
        // 后续不同成就可立即提交，已经确认的成就不会重复提交。
        nextAttempt = 0f;
    }

    private void OnDestroy()
    {
        statsStored?.Dispose();
        if (instance == this) instance = null;
    }
#endif
}
