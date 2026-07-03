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
    public AudioClip powerUpSound; // Hız düşürücü / Kalkan
    public AudioClip boostSound;   // Boost power-up
    public AudioClip nearMissSound; // Near miss geçiş sesi
    public AudioClip truckHornSound; // Kamyon kornası
    public AudioClip buttonClickSound; // Buton tıklama sesi
    public AudioClip countdownBeep;
    public AudioClip countdownGo;

    // Ayarlar
    private bool isMusicOn = true;
    private bool isSfxOn = true;

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