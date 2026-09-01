using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// Wraps Google Play Games Services sign-in, score submission, and the native
// leaderboard UI (which has a built-in "Friends" tab once a player has Play
// Games friends - no custom friends UI needed on our side).
//
// SETUP REQUIRED BEFORE THIS WORKS (see chat for the full walkthrough):
//   1. Create the app listing in Google Play Console (package: com.DealitGames.DodgeIt).
//   2. Enable Play Games Services for it and link a Google Cloud project.
//   3. Create a Leaderboard in Play Console and paste its ID into LeaderboardId below.
//   4. Run Window > Google Play Games > Setup > Android Setup in the Editor with
//      the resources.xml Google gives you on the Play Games Services setup page.
public class GooglePlayGamesManager : MonoBehaviour
{
    public static GooglePlayGamesManager instance;

    [Tooltip("Leaderboard ID from Play Console > Play Games Services > Leaderboards. Leave empty until you've created it.")]
    public string LeaderboardId = "";

    public bool IsAuthenticated { get; private set; }

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

#if UNITY_ANDROID
        PlayGamesPlatform.Activate();
#endif
    }

    void Start()
    {
        SignIn();
    }

    public void SignIn()
    {
#if UNITY_ANDROID
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            IsAuthenticated = status == SignInStatus.Success;
            if (!IsAuthenticated)
            {
                Debug.Log("Play Games sign-in failed or was cancelled: " + status);
            }
        });
#endif
    }

    public void SubmitScore(int score)
    {
#if UNITY_ANDROID
        if (string.IsNullOrEmpty(LeaderboardId))
        {
            Debug.LogWarning("GooglePlayGamesManager.LeaderboardId is not set - skipping score submit. Create a leaderboard in Play Console first.");
            return;
        }

        if (!IsAuthenticated)
        {
            // Try to sign in, then submit once we know the result.
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                IsAuthenticated = status == SignInStatus.Success;
                if (IsAuthenticated)
                {
                    PlayGamesPlatform.Instance.ReportScore(score, LeaderboardId, success => { });
                }
            });
            return;
        }

        PlayGamesPlatform.Instance.ReportScore(score, LeaderboardId, success =>
        {
            if (!success) Debug.LogWarning("Failed to submit score to Play Games leaderboard.");
        });
#endif
    }

    // Shows Google's native leaderboard UI - includes an "All players" / "Friends"
    // toggle automatically once the signed-in player has Play Games friends.
    public void ShowLeaderboard()
    {
#if UNITY_ANDROID
        if (string.IsNullOrEmpty(LeaderboardId))
        {
            Debug.LogWarning("GooglePlayGamesManager.LeaderboardId is not set - create a leaderboard in Play Console first.");
            return;
        }

        if (!IsAuthenticated)
        {
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                IsAuthenticated = status == SignInStatus.Success;
                if (IsAuthenticated)
                {
                    PlayGamesPlatform.Instance.ShowLeaderboardUI(LeaderboardId, null);
                }
            });
            return;
        }

        PlayGamesPlatform.Instance.ShowLeaderboardUI(LeaderboardId, null);
#else
        Debug.Log("Play Games leaderboard UI is only available on Android.");
#endif
    }
}
