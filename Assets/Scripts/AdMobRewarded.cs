using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using Unity.Advertisement.IosSupport;
#endif
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

// AdMob ödüllü reklam bağlantısı. Oyun kodu bunu doğrudan çağırmaz; tek giriş noktası RewardedAd.Show.
// Oyun açılınca kendiliğinden başlar: (AB/İngiltere'deki oyuncular için) izin formu → AdMob → ilk reklamı önceden yükle.
public static class AdMobRewarded
{
    // AdMob > Uygulamalar > DodgeIt!! > Reklam birimleri'ndeki ödüllü reklam biriminin kimliği.
    // Boş kaldıkça Google'ın test reklam birimi kullanılır (gerçek gelir gelmez).
#if UNITY_IOS
    // iOS reklam birimi (AdMob > DodgeIt iOS uygulaması). Boşken test reklamı gösterilir.
    private const string ReleaseAdUnitId = "ca-app-pub-5764970843009095/5625562779";
    private const string TestAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string ReleaseAdUnitId = "ca-app-pub-5764970843009095/9038168527";
    private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#endif

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
        Debug.Log("[Ads] Consent update starting");
        ConsentInformation.Update(new ConsentRequestParameters(), updateError =>
        {
            if (updateError != null) Debug.LogWarning("[Ads] Consent update failed: " + updateError.Message);
            Debug.Log("[Ads] Consent status: " + ConsentInformation.ConsentStatus + ", canRequestAds: " + ConsentInformation.CanRequestAds());
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null) Debug.LogWarning("[Ads] Consent form failed: " + formError.Message);
                Debug.Log("[Ads] Consent form done, canRequestAds: " + ConsentInformation.CanRequestAds());
                if (ConsentInformation.CanRequestAds()) InitializeAds();
            });
        });

        // Önceki oturumdan izin varsa formu beklemeden başla
        if (ConsentInformation.CanRequestAds()) InitializeAds();

        // İzin servisi cevap vermezse (iOS'ta görüldü) reklamlar hiç başlamasın diye yedek yol
        ConsentTimeoutFallback();
    }

    static async void ConsentTimeoutFallback()
    {
        await Task.Delay(6000);
        if (initialized) return;
        Debug.Log("[Ads] Consent timeout, using fallback (ATT + init)");
        await RequestTrackingIfNeeded();
        InitializeAds();
    }

    // iOS takip izni (ATT): normalde UMP'nin IDFA mesajı açar; açılmadıysa burada istenir
    static async Task RequestTrackingIfNeeded()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED) return;
        ATTrackingStatusBinding.RequestAuthorizationTracking();
        for (int i = 0; i < 120; i++) // kullanıcı cevap verene kadar en fazla ~60 sn bekle
        {
            await Task.Delay(500);
            if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() !=
                ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED) break;
        }
        Debug.Log("[Ads] ATT status: " + ATTrackingStatusBinding.GetAuthorizationTrackingStatus());
#else
        await Task.CompletedTask;
#endif
    }

    static void InitializeAds()
    {
        if (initialized) return;
        initialized = true;
        Debug.Log("[Ads] MobileAds.Initialize starting");
        MobileAds.Initialize(_ =>
        {
            Debug.Log("[Ads] MobileAds initialized");
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
            Debug.Log("[Ads] Rewarded ad loaded");
        });
    }

    // onFinished(true) yalnızca oyuncu reklamı ödül alacak kadar izlediyse çağrılır.
    public static void Show(Action<bool> onFinished)
    {
        if (!IsReady) { Debug.Log("[Ads] Rewarded not ready (initialized=" + initialized + ", loading=" + loading + ")"); onFinished?.Invoke(false); Load(); return; }

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
