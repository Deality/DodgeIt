using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

// AdMob ödüllü reklam bağlantısı. Oyun kodu bunu doğrudan çağırmaz; tek giriş noktası RewardedAd.Show.
// Oyun açılınca kendiliğinden başlar: (AB/İngiltere'deki oyuncular için) izin formu → AdMob → ilk reklamı önceden yükle.
public static class AdMobRewarded
{
    // AdMob > Uygulamalar > DodgeIt!! > Reklam birimleri'ndeki ödüllü reklam biriminin kimliği.
    // Boş kaldıkça Google'ın test reklam birimi kullanılır (gerçek gelir gelmez).
    private const string ReleaseAdUnitId = "";
    private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    private static string AdUnitId =>
        (Debug.isDebugBuild || string.IsNullOrEmpty(ReleaseAdUnitId)) ? TestAdUnitId : ReleaseAdUnitId;

    private static GoogleMobileAds.Api.RewardedAd loadedAd;
    private static bool initialized;
    private static bool loading;

    public static bool IsReady => loadedAd != null && loadedAd.CanShowAd();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // AdMob geri çağrıları Unity ana iş parçacığında gelsin (eski ama 11.x'te çalışan ayar)
#pragma warning disable CS0618
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
#pragma warning restore CS0618

        // GDPR izni: gerekmeyen ülkelerde form hiç açılmaz
        ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
        {
            if (updateError != null) Debug.LogWarning("Consent update failed: " + updateError.Message);
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null) Debug.LogWarning("Consent form failed: " + formError.Message);
                if (ConsentInformation.CanRequestAds()) InitializeAds();
            });
        });

        // Önceki oturumdan izin varsa formu beklemeden başla
        if (ConsentInformation.CanRequestAds()) InitializeAds();
    }

    static void InitializeAds()
    {
        if (initialized) return;
        initialized = true;
        MobileAds.Initialize(_ =>
        {
            Load();
            AdMobInterstitial.OnAdsInitialized();
        });
    }

    public static void Load()
    {
        if (!initialized || loading || IsReady) return;
        loading = true;
        GoogleMobileAds.Api.RewardedAd.Load(AdUnitId, new AdRequest(), (ad, error) =>
        {
            loading = false;
            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded ad failed to load: " + error);
                return;
            }
            loadedAd = ad;
        });
    }

    // onFinished(true) yalnızca oyuncu reklamı ödül alacak kadar izlediyse çağrılır.
    public static void Show(Action<bool> onFinished)
    {
        if (!IsReady) { onFinished?.Invoke(false); Load(); return; }

        var ad = loadedAd;
        loadedAd = null;
        bool earned = false;
        bool done = false;

        void Finish()
        {
            if (done) return;
            done = true;
            ad.Destroy();
            onFinished?.Invoke(earned);
            Load();
        }

        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Rewarded ad failed to show: " + error);
            Finish();
        };
        ad.Show(_ => earned = true);
    }
}
