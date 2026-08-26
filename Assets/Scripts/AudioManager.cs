using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    public AudioSource musicSource; // Arka plan müziği
    public AudioSource sfxSource;   // Efektler (Coin, Kaza vb.)
    public AudioSource engineSource;// Araba motor sesi (Loop)

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

        LoadSettings();
        ApplyMuteStates();
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
        if (engineSource != null) engineSource.mute = !isSfxOn;
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

    // --- EFEKTLER (SFX) ---
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Şerit değiştirirken aynı temalı birkaç varyasyondan rastgele birini çalar (tekdüzelik olmasın diye).
    public void PlayRandomSwipeSound()
    {
        if (swipeSounds == null || swipeSounds.Length == 0) return;
        AudioClip clip = swipeSounds[Random.Range(0, swipeSounds.Length)];
        PlaySFX(clip);
    }

    // 🔥 EKSİK OLAN FONKSİYON EKLENDİ
    public void PlayButtonSound()
    {
        PlaySFX(buttonClickSound);
    }

    // --- AYARLARI GÜNCELLEME ---
    public void UpdateSettings()
    {
        LoadSettings();

        // Anlık tepki ver
        if (musicSource != null) musicSource.mute = !isMusicOn;
        if (sfxSource != null) sfxSource.mute = !isSfxOn;
        if (engineSource != null) engineSource.mute = !isSfxOn;

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