using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using CloudLeaderboardEntry = Unity.Services.Leaderboards.Models.LeaderboardEntry;

// Bridges the game to Unity Gaming Services (Authentication + Leaderboards) so every
// player - Android and iOS alike - shows up on one shared, cross-platform leaderboard.
// Google Play Games' own leaderboard can't do this: it's Android-only and doesn't share
// data with Apple's Game Center, so a backend-driven leaderboard is the only way to get a
// single ranked list that includes everyone regardless of platform. The player's chosen
// nickname (NicknameManager) becomes their UGS player name, which is what shows up here.
public class CloudLeaderboardManager : MonoBehaviour
{
    public static CloudLeaderboardManager instance;

    [Tooltip("Must match the Leaderboard ID created in the Unity Dashboard.")]
    public string leaderboardId = "DodgeIt_Leaderboard";

    public bool IsReady { get; private set; }

    private Task _initTask;

#if UNITY_EDITOR
    // Editor-only safety net: in this project, the Leaderboards package's own
    // [RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] registration has been observed to
    // silently not fire on some Editor Play sessions (verified via the Unity Console -
    // LeaderboardsService.Instance throws "has not been initialized" even though
    // Authentication signs in fine). Manually re-triggering that same registration call,
    // at the same BeforeSceneLoad timing, reliably fixes it. This never runs in a real
    // build (#if UNITY_EDITOR), where a fresh compile shouldn't hit this at all.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureLeaderboardsPackageRegistered()
    {
        try
        {
            var asm = typeof(LeaderboardsService).Assembly;
            var initializerType = asm.GetType("Unity.Services.Leaderboards.LeaderboardsInitializer");
            var method = initializerType?.GetMethod("InitializeOnLoad",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            method?.Invoke(null, null);
        }
        catch
        {
            // Already registered (or a future SDK version no longer needs this) - ignore.
        }
    }
#endif

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Waiting a frame before calling UnityServices.InitializeAsync() matters: called
        // synchronously from Awake (before Unity's own AfterSceneLoad services bootstrap
        // has run), the Leaderboards package can end up registered too late to be included
        // in that pass, leaving LeaderboardsService.Instance permanently unset for the rest
        // of the session even though Authentication initializes and signs in fine.
        StartCoroutine(DelayedInit());
    }

    private System.Collections.IEnumerator DelayedInit()
    {
        yield return null;
        _initTask = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsReady = true;

            if (NicknameManager.HasNickname())
            {
                await PushPlayerNameInternal(NicknameManager.GetNickname());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("CloudLeaderboardManager: initialization failed - " + e.Message);
        }
    }

    public async Task UpdatePlayerNameAsync(string nickname)
    {
        await _initTask;
        if (!IsReady) return;
        await PushPlayerNameInternal(nickname);
    }

    private async Task PushPlayerNameInternal(string nickname)
    {
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(nickname);
        }
        catch (Exception e)
        {
            Debug.LogWarning("CloudLeaderboardManager: failed to update player name - " + e.Message);
        }
    }

    public async Task SubmitScoreAsync(double score)
    {
        await _initTask;
        if (!IsReady) return;

        try
        {
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score);
        }
        catch (Exception e)
        {
            Debug.LogWarning("CloudLeaderboardManager: failed to submit score - " + e.Message);
        }
    }

    public async Task<List<CloudLeaderboardEntry>> GetTopScoresAsync(int limit = 50)
    {
        await _initTask;
        if (!IsReady) return new List<CloudLeaderboardEntry>();

        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, new GetScoresOptions { Limit = limit });
            return page.Results;
        }
        catch (Exception e)
        {
            Debug.LogWarning("CloudLeaderboardManager: failed to fetch leaderboard - " + e.Message);
            return new List<CloudLeaderboardEntry>();
        }
    }
}
