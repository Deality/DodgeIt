using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // Arka plan müziği (-> Ambient bus)
    public AudioSource sfxSource;   // Efektler / Action sesleri (Coin, Swipe, vb.) (-> Action bus)
    public AudioSource engineSource;// Araba motor sesi (Loop) (-> Gameplay Loop bus)
    public AudioSource criticalSource; // Kaza/Boost gibi öncelikli sesler (-> Critical bus)
    public AudioSource uiSource;       // Buton/menü sesleri (-> UI bus)

    [Header("Audio Mixer (Bus Yönlendirme & Ducking)")]
    [Tooltip("Assets/Audio klasöründeki AudioMixer asset'i. Bkz. AudioManager üstündeki kurulum notu.")]
    public AudioMixer audioMixer;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup gameplayLoopGroup;
    public AudioMixerGroup actionGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup criticalGroup;

    [Header("Ducking (Action/Critical çalınca Ambient+Gameplay Loop kısılır)")]
    [Tooltip("Ducking sırasında Ambient/Gameplay Loop bus'larının ineceği seviye (dB). -80 = tamamen sessiz.")]
    public float duckedVolumeDb = -14f;
    [Tooltip("Kısma/geri açma geçişinin süresi (saniye).")]
    public float duckTransitionDuration = 0.15f;

    private const string AmbientVolumeParam = "AmbientVolume";
    private const string GameplayLoopVolumeParam = "GameplayLoopVolume";

    private int activeDuckCount = 0;
    private float ambientBaseDb = 0f;
    private float gameplayLoopBaseDb = 0f;
    private Coroutine duckFadeRoutine;

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
    public AudioClip countdownBeep;
    public AudioClip countdownGo;

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

        RouteMixerGroups();
        LoadSettings();
        ApplyMuteStates();
    }

    // Her AudioSource'u ilgili mixer grubuna bağlar - Inspector'da tek tek Output alanı
    // ayarlamayı unutmaya karşı güvenlik: sadece bu 5 grup referansını AudioManager'da
    // bir kez atamak yeterli olur.
    private void RouteMixerGroups()
    {
        if (musicSource != null && ambientGroup != null) musicSource.outputAudioMixerGroup = ambientGroup;
        if (engineSource != null && gameplayLoopGroup != null) engineSource.outputAudioMixerGroup = gameplayLoopGroup;
        if (sfxSource != null && actionGroup != null) sfxSource.outputAudioMixerGroup = actionGroup;
        if (criticalSource != null && criticalGroup != null) criticalSource.outputAudioMixerGroup = criticalGroup;
        if (uiSource != null && uiGroup != null) uiSource.outputAudioMixerGroup = uiGroup;
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
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
    }

    // --- MÜZİK ---
    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null && isMusicOn)
        {
            if (musicSource.isPlaying) return; // Zaten çalıyorsa tekrar başlatma
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
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

    // --- EFEKTLER (SFX) --- kept as an alias so every existing call site (coin, crash,
    // shield, boost, near-miss, truck horn...) keeps working unchanged and still gets
    // routed through the Action bus + ducking below.
    public void PlaySFX(AudioClip clip) => PlayAction(clip);

    // Şerit değiştirirken aynı temalı birkaç varyasyondan rastgele birini çalar (tekdüzelik olmasın diye).
    public void PlayRandomSwipeSound()
    {
        if (swipeSounds == null || swipeSounds.Length == 0) return;
        AudioClip clip = swipeSounds[Random.Range(0, swipeSounds.Length)];
        PlayAction(clip);
    }

    public void PlayButtonSound()
    {
        PlayUI(buttonClickSound);
    }

    // --- BUS'A GÖRE OYNATMA & DUCKING ---
    // Action: genel oynanış sesleri (coin, swipe, near miss, kamyon kornası...). Gameplay
    // Loop + Ambient'i kısar.
    public void PlayAction(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
        RequestDuck(clip.length);
    }

    // Critical: kaza, boost aktivasyonu gibi öne çıkması gereken anlar. Kendi bus'ında
    // çalar (Action ile aynı anda çakışmaz) ve aynı şekilde Gameplay Loop + Ambient'i kısar.
    public void PlayCritical(AudioClip clip)
    {
        if (criticalSource == null || clip == null) return;
        criticalSource.PlayOneShot(clip);
        RequestDuck(clip.length);
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
            ambientBaseDb = db;
            if (activeDuckCount == 0) audioMixer.SetFloat(AmbientVolumeParam, db);
        }
        else if (bus == "GameplayLoop")
        {
            gameplayLoopBaseDb = db;
            if (activeDuckCount == 0) audioMixer.SetFloat(GameplayLoopVolumeParam, db);
        }
    }

    private static float LinearToDb(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }

    // clip.length'lik bir "tutma" süresi ister; sayaç 0'a dönene kadar kısık kalır, böylece
    // üst üste binen Action/Critical sesleri sesi erken geri açmaz. Fade geçişleri dışında
    // hiçbir per-frame Update maliyeti yok - coroutine sadece geçiş sırasında çalışır.
    private void RequestDuck(float holdDuration)
    {
        if (audioMixer == null) return;

        activeDuckCount++;
        StartCoroutine(ReleaseDuckAfter(Mathf.Max(holdDuration, 0.05f)));

        if (duckFadeRoutine != null) StopCoroutine(duckFadeRoutine);
        duckFadeRoutine = StartCoroutine(DuckFadeRoutine(true));
    }

    private IEnumerator ReleaseDuckAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        activeDuckCount = Mathf.Max(0, activeDuckCount - 1);
        if (activeDuckCount == 0)
        {
            if (duckFadeRoutine != null) StopCoroutine(duckFadeRoutine);
            duckFadeRoutine = StartCoroutine(DuckFadeRoutine(false));
        }
    }

    // Gameplay Loop (motor) burada kasıtlı olarak yok - motor sabit bir arkaplan sesi olarak
    // kalmalı, Action/Critical sesleri çalınca kısılmamalı. Sadece Ambient (müzik) kısılır.
    private IEnumerator DuckFadeRoutine(bool duckIn)
    {
        audioMixer.GetFloat(AmbientVolumeParam, out float startAmbient);
        float targetAmbient = duckIn ? duckedVolumeDb : ambientBaseDb;

        float t = 0f;
        while (t < duckTransitionDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duckTransitionDuration);
            audioMixer.SetFloat(AmbientVolumeParam, Mathf.Lerp(startAmbient, targetAmbient, p));
            yield return null;
        }

        audioMixer.SetFloat(AmbientVolumeParam, targetAmbient);
        duckFadeRoutine = null;
    }

    // --- AYARLARI GÜNCELLEME ---
    public void UpdateSettings()
    {
        LoadSettings();

        // Anlık tepki ver
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
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