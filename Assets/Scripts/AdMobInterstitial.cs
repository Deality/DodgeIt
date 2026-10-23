using System;
using UnityEngine;
using GoogleMobileAds.Api;

// Klasik modda birkaç oyunda bir, oyun bitince sonuç ekranından önce çıkan geçiş reklamı.
// Kurallar: ilk birkaç oyunda yok, 3 oyunda bir, iki reklam arası en az 90 sn,
// o turda revive reklamı izlendiyse yok, Remove Ads ya da herhangi bir satın alma yapıldıysa hiç yok.
public static class AdMobInterstitial
{
    // AdMob'daki Geçiş (Interstitial) reklam biriminin kimliği. Boş kaldıkça Google'ın test birimi kullanılır.
    private const string ReleaseAdUnitId = "";
    private const string TestAdUnitId = "ca-app-pub-3940256099942544/1033173712";

    private const int RunsBetweenAds = 3;
    private const int FreeRunsForNewPlayers = 3;
    private const float MinSecondsBetweenAds = 90f;
    private const string TotalRunsKey = "Ads_TotalClassicRuns";

    private static string AdUnitId =>
        (Debug.isDebugBuild || string.IsNullOrEmpty(ReleaseAdUnitId)) ? TestAdUnitId : ReleaseAdUnitId;

    private static InterstitialAd loadedAd;
    private static bool initialized;
    private static bool loading;
    private static int runsSinceLastAd;
    private static float lastAdTime = -999f;

    public static bool IsReady => loadedAd != null && loadedAd.CanShowAd();

    // AdMobRewarded, AdMob hazır olunca çağırır
    public static void OnAdsInitialized()
    {
        initialized = true;
        Load();
    }

    static void Load()
    {
        if (!initialized || loading || IsReady || CoinStore.NoForcedAds) return;
        loading = true;
        InterstitialAd.Load(AdUnitId, new AdRequest(), (ad, error) =>
        {
            loading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("Interstitial ad failed to load: " + error);
                return;
            }
            loadedAd = ad;
        });
    }

    // Klasik oyun her bittiğinde bir kez çağrılır. Reklam gösterilmesi gerekiyorsa true döner.
    public static bool RegisterRunAndCheck(bool watchedRewardedThisRun)
    {
        int totalRuns = PlayerPrefs.GetInt(TotalRunsKey, 0) + 1;
        PlayerPrefs.SetInt(TotalRunsKey, totalRuns);
        runsSinceLastAd++;

        if (CoinStore.NoForcedAds) return false;
        if (totalRuns <= FreeRunsForNewPlayers) return false;
        if (watchedRewardedThisRun) return false;
        if (runsSinceLastAd < RunsBetweenAds) return false;
        if (Time.realtimeSinceStartup - lastAdTime < MinSecondsBetweenAds) return false;
        if (!IsReady) { Load(); return false; }
        return true;
    }

    // onClosed reklam kapanınca (ya da gösterilemezse hemen) çağrılır.
    public static void Show(Action onClosed)
    {
        if (!IsReady) { onClosed?.Invoke(); Load(); return; }

        var ad = loadedAd;
        loadedAd = null;
        runsSinceLastAd = 0;
        lastAdTime = Time.realtimeSinceStartup;
        bool done = false;

        void Finish()
        {
            if (done) return;
            done = true;
            ad.Destroy();
            onClosed?.Invoke();
            Load();
        }

        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Interstitial ad failed to show: " + error);
            Finish();
        };
        ad.Show();
    }
}
