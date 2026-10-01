using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// SES KATMANLARI (alttan üste). Üstteki katman çalarken alttakiler kısılır, böylece aynı anda
// çalan sesler birbirine girmez:
//   1) Müzik (Ambient)        - en altta; sadece Olay/Kritik seslerde kısılır, her coin'de değil
//   2) Motor (Gameplay Loop)  - sabit arka plan
//   3) Geri bildirim          - swipe, coin, near miss: sık ve küçük; biraz kısık, müziği kısmaz
//   4) Olay (Action)          - power-up'lar, kalkan/boost ile patlatma, korna, Coin Rush
//   5) Kritik                 - kaza: müziği ve 3-4. katmanları güçlü kısar
//   UI                        - buton/geri sayım; hiçbir şeyden etkilenmez
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // 1) Arka plan müziği (-> Ambient bus)
    public AudioSource sfxSource;   // 4) Olay sesleri: power-up, patlatma, korna... (-> Action bus)
    public AudioSource engineSource;// 2) Araba motor sesi (Loop) (-> Gameplay Loop bus)
    public AudioSource criticalSource; // 5) Kaza gibi öncelikli sesler (-> Critical bus)
    public AudioSource uiSource;       // Buton/menü sesleri (-> UI bus)
    private AudioSource feedbackSource; // 3) Swipe/coin/near miss - Awake'te oluşturulur (-> Action bus)
    private AudioSource driftSource;    // 3) Drift lastik sesi (loop) - Awake'te oluşturulur (-> Action bus)
    private float driftTarget;          // 0..1, araba kayarken her karede yeniler
    private float driftLevel;           // yumuşatılmış seviye
    private float lastDriftRequestTime = -10f;

    [Header("Audio Mixer (Bus Yönlendirme & Ducking)")]
    [Tooltip("Assets/Audio klasöründeki AudioMixer asset'i. Bkz. AudioManager üstündeki kurulum notu.")]
    public AudioMixer audioMixer;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup gameplayLoopGroup;
    public AudioMixerGroup actionGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup criticalGroup;

    [Header("Katmanlar & Ducking")]
    [Tooltip("Kritik ses (kaza) çalarken müziğin ineceği seviye (dB). -80 = tamamen sessiz.")]
    public float duckedVolumeDb = -14f;
    [Tooltip("Olay sesi (power-up, patlatma...) çalarken müziğin ineceği seviye (dB).")]
    public float eventDuckDb = -6f;
    [Tooltip("Geri bildirim seslerinin (swipe/coin/near miss) temel seviyesi. Olay seslerinin altında kalsın diye < 1.")]
    [Range(0f, 1f)] public float feedbackVolume = 0.7f;
    [Tooltip("Olay sesi çalarken geri bildirim seslerinin çarpanı.")]
    [Range(0f, 1f)] public float feedbackDuckedByEvent = 0.5f;
    [Tooltip("Kritik ses çalarken geri bildirim ve olay seslerinin çarpanı.")]
    [Range(0f, 1f)] public float lowerLayersDuckedByCritical = 0.2f;
    [Tooltip("Aynı ses bu süreden (saniye) daha sık tetiklenirse tekrar çalınmaz - üst üste yığılmayı önler.")]
    public float sameClipMinInterval = 0.05f;
    [Tooltip("Kısma/geri açma geçişinin süresi (saniye).")]
    public float duckTransitionDuration = 0.15f;

    private const string AmbientVolumeParam = "AmbientVolume";
    private const string GameplayLoopVolumeParam = "GameplayLoopVolume";

    private struct DuckRequest
    {
        public float endTime;
        public float musicDb;     // müziğin hedef seviyesi (dB, ambientBaseDb'ye göre)
        public float feedbackMul; // geri bildirim katmanı çarpanı
        public float actionMul;   // olay katmanı çarpanı
    }

    private readonly List<DuckRequest> duckRequests = new List<DuckRequest>();
    private readonly Dictionary<AudioClip, float> lastPlayTime = new Dictionary<AudioClip, float>();
    private float ambientBaseDb = 0f;
    private float gameplayLoopBaseDb = 0f;
    private float currentMusicDuckDb = 0f;
    private float currentFeedbackMul = 1f;
    private float currentActionMul = 1f;
    private float actionBaseVolume = 1f;

    [Header("Audio Clips (Ses Dosyaları)")]
    public AudioClip backgroundMusic;
    public AudioClip engineLoop;
    public AudioClip crashSound;
    public AudioClip coinSound;
    public AudioClip powerUpSound; // Hız düşürücü
    public AudioClip shieldPickupSound;  // Kalkan toplama sesi
    public AudioClip shieldDestroySound; // Kalkan ile engel yok etme sesi
    public AudioClip boostSound;   // Boost power-up (aktivasyon)
    public AudioClip boostDestroySound; // Öfke Modu ile araba patlatma sesi
    public AudioClip nearMissSound; // Near miss geçiş sesi
    public AudioClip truckHornSound; // Kamyon kornası
    public AudioClip buttonClickSound; // Buton tıklama sesi
    public AudioClip purchaseSound;    // Marketten araba / yol satın alınınca
    public AudioClip countdownBeep; // Devam ederken 3-2-1
    public AudioClip countdownGo;   // "GO!"
    public AudioClip coinRushSound;     // Drift Boost (Coin Rush) toplama
    [Tooltip("Coin Rush sonunda '+N' sayılırken her artışta çalan sayaç tıkı.")]
    public AudioClip coinRushCountTick;
    [Tooltip("Coin Rush sonunda '+N' sayısı son değere ulaşınca çalar.")]
    public AudioClip coinRushTotalSound;
    [Tooltip("Coin Rush'ta oyuncu basılı tutup kayarken dönen lastik cıyaklaması (loop).")]
    public AudioClip driftLoopSound;
    [Range(0f, 1f)] public float driftLoopVolume = 0.55f;
    public AudioClip gameOverSound;     // Game Over paneli açılırken
    public AudioClip newHighScoreSound; // Yeni rekorda Game Over sesi yerine çalar (boşsa Game Over sesi çalar)
    [Tooltip("Oyun başında araba sahneye girerken (intro animasyonu) çalan motor sesi.")]
    public AudioClip introEngineSound;

    [Header("Müzik (sahneye göre)")]
    [Tooltip("Ana menü ve yükleme ekranında çalan müzik. Boş bırakılırsa menüde müzik çalmaz.")]
    public AudioClip menuMusic;
    [Tooltip("Bu sahnede backgroundMusic, diğer sahnelerde menuMusic çalar.")]
    public string gameSceneName = "SampleScene";

    [Header("Şerit Değiştirme (Swipe) Sesleri")]
    [Tooltip("Şerit değiştirirken rastgele seçilip çalınacak swipe sesleri (aynı temada birkaç varyasyon).")]
    public AudioClip[] swipeSounds;

    [Header("Duraklat / Kaza Ses Kısma Ayarı")]
    [Tooltip("Duraklatıldığında veya kaza anında motor/müzik sesinin ne kadar hızlı kısılıp geri açılacağı")]
    public float duckFadeSpeed = 6f;

    // Ayarlar
    private bool isMusicOn = true;
    private bool isSfxOn = true;

    // 🔥 Motor ve müzik seslerinin Inspector'da ayarlanan orijinal seviyeleri (kısma/geri açma bunlara göre yapılır)
    private float engineBaseVolume = 0.6f;
    private float musicBaseVolume = 0.4f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Sahne değişince yok olmasın
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (engineSource != null) engineBaseVolume = engineSource.volume;
        if (musicSource != null) musicBaseVolume = musicSource.volume;
        if (sfxSource != null) actionBaseVolume = sfxSource.volume;

        // 3) Geri bildirim katmanı için ayrı kaynak: olay/kritik sesler çalarken tek başına kısılabilsin
        feedbackSource = gameObject.AddComponent<AudioSource>();
        feedbackSource.playOnAwake = false;
        feedbackSource.loop = false;
        feedbackSource.volume = feedbackVolume;

        driftSource = gameObject.AddComponent<AudioSource>();
        driftSource.playOnAwake = false;
        driftSource.loop = true;
        driftSource.volume = 0f;

        RouteMixerGroups();
        LoadSettings();
        ApplyMuteStates();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Sahne değişince o sahnenin müziğine geç (menü <-> oyun)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) PlayMusic();
    }

    // Her AudioSource'u ilgili mixer grubuna bağlar - Inspector'da tek tek Output alanı
    // ayarlamayı unutmaya karşı güvenlik: sadece bu 5 grup referansını AudioManager'da
    // bir kez atamak yeterli olur.
    private void RouteMixerGroups()
    {
        if (musicSource != null && ambientGroup != null) musicSource.outputAudioMixerGroup = ambientGroup;
        if (engineSource != null && gameplayLoopGroup != null) engineSource.outputAudioMixerGroup = gameplayLoopGroup;
        if (sfxSource != null && actionGroup != null) sfxSource.outputAudioMixerGroup = actionGroup;
        if (feedbackSource != null && actionGroup != null) feedbackSource.outputAudioMixerGroup = actionGroup;
        if (driftSource != null && actionGroup != null) driftSource.outputAudioMixerGroup = actionGroup;
        if (criticalSource != null && criticalGroup != null) criticalSource.outputAudioMixerGroup = criticalGroup;
        if (uiSource != null && uiGroup != null) uiSource.outputAudioMixerGroup = uiGroup;
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
        if (feedbackSource != null) feedbackSource.mute = !isSfxOn;
        if (driftSource != null) driftSource.mute = !isSfxOn;
        if (engineSource != null) engineSource.mute = !isSfxOn;
        if (criticalSource != null) criticalSource.mute = !isSfxOn;
        if (uiSource != null) uiSource.mute = !isSfxOn;
    }

    void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        StartCoroutine(DelayedAudioStart());
#else
        PlayMusic();
        PlayEngineSound();
#endif
    }

    private System.Collections.IEnumerator DelayedAudioStart()
    {
        // Give Android audio system a frame to finish initializing
        yield return null;
        PlayMusic();
        PlayEngineSound();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
            if (engineSource != null && engineSource.isPlaying) engineSource.Pause();
        }
        else
        {
            if (isMusicOn && musicSource != null && musicSource.clip != null && !musicSource.isPlaying)
                musicSource.UnPause();
            if (isSfxOn && engineSource != null && engineSource.clip != null && !engineSource.isPlaying)
                engineSource.UnPause();
        }
    }

    void Update()
    {
        // Motor sesinin perdesini (Pitch) hıza göre ayarla
        if (engineSource != null && ObstacleManager.instance != null)
        {
            // Hız 0 ise pitch 0.8, Hız 180 ise pitch 2.0 olsun
            float currentSpeed = ObstacleManager.scrollSpeed;
            float pitch = Mathf.Lerp(0.8f, 2.0f, currentSpeed / 180f);
            engineSource.pitch = pitch;
        }

        // 🔥 DURAKLATMA / KAZA SESİ KISMA: Oyun duraklatıldığında (Time.timeScale == 0)
        // veya kaza anında (GameOver / oyun aktif değil) motor ve müzik sesini yumuşakça
        // kısıyoruz; oyun devam ederken de orijinal seviyesine geri getiriyoruz.
        // unscaledDeltaTime kullanıyoruz ki Time.timeScale=0 iken bile geçiş animasyonlu olsun.
        bool shouldDuck = Time.timeScale == 0f ||
            (GameManager.instance != null && (GameManager.instance.IsGameOver || !GameManager.instance.isGameActive));

        // 🔥 Motor sesi SADECE gerçek oyun sırasında (SampleScene, ObstacleManager mevcutken)
        // duyulmalı. Main Menu'de ObstacleManager hiç var olmadığından, motor sesi orada
        // otomatik olarak kısılır (menüde araba önizlemesi olsa bile).
        bool shouldDuckEngine = shouldDuck || ObstacleManager.instance == null;

        float fadeStep = duckFadeSpeed * Time.unscaledDeltaTime;

        if (engineSource != null)
        {
            float targetVolume = shouldDuckEngine ? 0f : engineBaseVolume;
            engineSource.volume = Mathf.MoveTowards(engineSource.volume, targetVolume, fadeStep);
        }

        if (musicSource != null)
        {
            float targetVolume = shouldDuck ? 0f : musicBaseVolume;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, targetVolume, fadeStep);
        }

        UpdateLayerDucking();
        UpdateDriftLoop(shouldDuck);
    }

    // Oyun başında araba sahneye girerken
    public void PlayIntroEngine()
    {
        // Ana menü de seçili arabayı PlayerSpawner ile oluşturuyor; motor sesi yalnızca oyun sahnesinde çalsın
        if (SceneManager.GetActiveScene().name != gameSceneName) return;
        PlayAction(introEngineSound);
    }

    // Araba, oyuncu basılı tutup kayarken HER KAREDE çağırır (intensity: 0..1 kayma şiddeti).
    // Çağrı kesilince (parmak kalktı, drift bitti, araba yok oldu) ses kendiliğinden söner.
    public void SetDriftSound(float intensity)
    {
        driftTarget = Mathf.Clamp01(intensity);
        lastDriftRequestTime = Time.unscaledTime;
    }

    private void UpdateDriftLoop(bool gamePausedOrOver)
    {
        if (driftSource == null || driftLoopSound == null) return;

        bool requested = Time.unscaledTime - lastDriftRequestTime < 0.1f && !gamePausedOrOver;
        float target = requested ? driftTarget : 0f;
        // hızlı aç (~0.08 sn), biraz daha yumuşak kapat (~0.15 sn)
        float speed = target > driftLevel ? 12f : 7f;
        driftLevel = Mathf.MoveTowards(driftLevel, target, speed * Time.unscaledDeltaTime);

        if (driftLevel > 0.001f)
        {
            if (!driftSource.isPlaying)
            {
                driftSource.clip = driftLoopSound;
                driftSource.time = Random.Range(0f, driftLoopSound.length); // her kaymada aynı yerden başlamasın
                driftSource.Play();
            }
            // geri bildirim katmanıyla birlikte kısılır (olay/kritik ses çalarken geri çekilir)
            driftSource.volume = driftLoopVolume * driftLevel * currentFeedbackMul;
            driftSource.pitch = Mathf.Lerp(0.94f, 1.06f, driftLevel);
        }
        else if (driftSource.isPlaying)
        {
            driftSource.Stop();
        }
    }

    // --- MÜZİK ---
    public void PlayMusic()
    {
        // Oyun sahnesinde oyun müziği, diğer sahnelerde (menü, yükleme) menü müziği.
        // menuMusic boşsa menüde müzik çalmaz.
        bool inGame = SceneManager.GetActiveScene().name == gameSceneName;
        AudioClip clip = inGame ? backgroundMusic : menuMusic;

        if (musicSource == null) return;
        if (clip == null) { musicSource.Stop(); return; } // bu sahnenin müziği yok: öncekini sustur
        if (!isMusicOn) return;
        if (musicSource.isPlaying && musicSource.clip == clip) return; // Zaten bu müzik çalıyorsa tekrar başlatma

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // --- MOTOR SESİ ---
    public void PlayEngineSound()
    {
        if (engineSource != null && engineLoop != null)
        {
            if (engineSource.isPlaying) return;
            engineSource.clip = engineLoop;
            engineSource.loop = true;
            engineSource.Play();
        }
    }

    public void StopEngineSound()
    {
        if (engineSource != null) engineSource.Stop();
    }

    // --- EFEKTLER (SFX) --- eski çağrılar için takma ad: Olay katmanında (4) çalar.
    public void PlaySFX(AudioClip clip) => PlayAction(clip);

    // Şerit değiştirirken aynı temalı birkaç varyasyondan rastgele birini çalar (tekdüzelik olmasın diye).
    public void PlayRandomSwipeSound()
    {
        if (swipeSounds == null || swipeSounds.Length == 0) return;
        AudioClip clip = swipeSounds[Random.Range(0, swipeSounds.Length)];
        PlayFeedback(clip);
    }

    public void PlayButtonSound()
    {
        PlayUI(buttonClickSound);
    }

    public void PlayPurchaseSound()
    {
        PlayUI(purchaseSound);
    }

    public void PlayCoinRushCountTick() => PlayFeedback(coinRushCountTick);

    public void PlayCoinRushTotal() => PlayAction(coinRushTotalSound);

    // Geri sayım oyun duraklatılmışken çalışır; UI bus ducking'den etkilenmez.
    public void PlayCountdown(bool isGo)
    {
        PlayUI(isGo ? countdownGo : countdownBeep);
    }

    // --- KATMANA GÖRE OYNATMA & DUCKING ---
    // 3) Geri bildirim: swipe, coin, near miss. Sık çalar; kimseyi kısmaz, üst katmanlarca kısılır.
    public void PlayFeedback(AudioClip clip)
    {
        if (feedbackSource == null || !CanPlay(clip)) return;
        feedbackSource.PlayOneShot(clip);
    }

    // 4) Olay: power-up, kalkan/boost ile patlatma, korna, Coin Rush. Müziği hafif,
    // geri bildirim katmanını yarı yarıya kısar.
    public void PlayAction(AudioClip clip)
    {
        if (sfxSource == null || !CanPlay(clip)) return;
        sfxSource.PlayOneShot(clip);
        RequestDuck(clip.length, eventDuckDb, feedbackDuckedByEvent, 1f);
    }

    // 5) Kritik: kaza. Müziği güçlü kısar, geri bildirim ve olay katmanlarını geri çeker.
    public void PlayCritical(AudioClip clip)
    {
        if (criticalSource == null || !CanPlay(clip)) return;
        criticalSource.PlayOneShot(clip);
        RequestDuck(clip.length, duckedVolumeDb, lowerLayersDuckedByCritical, lowerLayersDuckedByCritical);
    }

    // Aynı klip çok kısa aralıkla tekrar tetiklenirse (Coin Rush'ta art arda coin'ler gibi) yığılmasın.
    private bool CanPlay(AudioClip clip)
    {
        if (clip == null) return false;
        float now = Time.unscaledTime;
        if (lastPlayTime.TryGetValue(clip, out float last) && now - last < sameClipMinInterval) return false;
        lastPlayTime[clip] = now;
        return true;
    }

    // UI: buton/menü sesleri. Ducking tetiklemez, ducking'den etkilenmez.
    public void PlayUI(AudioClip clip)
    {
        if (uiSource == null || clip == null) return;
        uiSource.PlayOneShot(clip);
    }

    // Ambient/Gameplay Loop bus'larının kalıcı (ducking dışı) taban seviyesini ayarlar -
    // örn. ayarlar menüsündeki bir ses kaydırıcısı. value: 0..1 lineer.
    public void SetLoopVolume(string bus, float value)
    {
        if (audioMixer == null) return;
        float db = LinearToDb(value);

        if (bus == "Ambient")
        {
            ambientBaseDb = db; // kısma varsa UpdateLayerDucking bunun üstüne uygular
            audioMixer.SetFloat(AmbientVolumeParam, ambientBaseDb + currentMusicDuckDb);
        }
        else if (bus == "GameplayLoop")
        {
            gameplayLoopBaseDb = db;
            audioMixer.SetFloat(GameplayLoopVolumeParam, db);
        }
    }

    private static float LinearToDb(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    // clip.length boyunca geçerli bir kısma isteği ekler. Aynı anda birden fazla istek varsa
    // en güçlüsü uygulanır; böylece üst üste binen sesler kısmayı erken bırakmaz.
    private void RequestDuck(float holdDuration, float musicDb, float feedbackMul, float actionMul)
    {
        duckRequests.Add(new DuckRequest
        {
            endTime = Time.unscaledTime + Mathf.Max(holdDuration, 0.05f),
            musicDb = musicDb,
            feedbackMul = feedbackMul,
            actionMul = actionMul
        });
    }

    // Her karede aktif isteklerden hedef seviyeleri hesaplar ve yumuşakça uygular.
    // Motor (Gameplay Loop) kasıtlı olarak burada yok - sabit arka plan sesi olarak kalır.
    private void UpdateLayerDucking()
    {
        float now = Time.unscaledTime;
        float targetMusicDb = 0f, targetFeedback = 1f, targetAction = 1f;

        for (int i = duckRequests.Count - 1; i >= 0; i--)
        {
            DuckRequest r = duckRequests[i];
            if (now >= r.endTime) { duckRequests.RemoveAt(i); continue; }
            targetMusicDb = Mathf.Min(targetMusicDb, r.musicDb);
            targetFeedback = Mathf.Min(targetFeedback, r.feedbackMul);
            targetAction = Mathf.Min(targetAction, r.actionMul);
        }

        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(duckTransitionDuration / 3f, 0.001f));
        float prevMusicDb = currentMusicDuckDb;
        currentMusicDuckDb = Mathf.Lerp(currentMusicDuckDb, targetMusicDb, k);
        currentFeedbackMul = Mathf.Lerp(currentFeedbackMul, targetFeedback, k);
        currentActionMul = Mathf.Lerp(currentActionMul, targetAction, k);

        if (audioMixer != null && Mathf.Abs(currentMusicDuckDb - prevMusicDb) > 0.01f)
            audioMixer.SetFloat(AmbientVolumeParam, ambientBaseDb + currentMusicDuckDb);
        if (feedbackSource != null) feedbackSource.volume = feedbackVolume * currentFeedbackMul;
        if (sfxSource != null) sfxSource.volume = actionBaseVolume * currentActionMul;
    }

    // --- AYARLARI GÜNCELLEME ---
    public void UpdateSettings()
    {
        LoadSettings();

        // Anlık tepki ver
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
        if (feedbackSource != null) feedbackSource.mute = !isSfxOn;
        if (driftSource != null) driftSource.mute = !isSfxOn;
        if (engineSource != null) engineSource.mute = !isSfxOn;
        if (criticalSource != null) criticalSource.mute = !isSfxOn;
        if (uiSource != null) uiSource.mute = !isSfxOn;

        // Ayarlar değişince müzik kapalıysa durdur, açıksa başlat
        if (!isMusicOn) StopMusic();
        else PlayMusic();

        if (!isSfxOn) StopEngineSound();
        else PlayEngineSound();
    }

    private void LoadSettings()
    {
        isMusicOn = PlayerPrefs.GetInt("IsMusicOn", 1) == 1;
        isSfxOn = PlayerPrefs.GetInt("IsEffectsOn", 1) == 1;
    }
}