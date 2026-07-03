using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public enum SlideDirection { Top, Bottom, Left, Right }

public class UIManager : MonoBehaviour
{
    // 🔥 Oyunun her yerinden UIManager'a ulaşabilmek için
    public static UIManager instance;

    [Header("Ayarlar Paneli UI")]
    public GameObject settingsPanel;
    public Toggle musicToggle;
    public Toggle effectsToggle;
    public Toggle controlModeToggle; // True: Button, False: Swipe
    public Toggle vibrationToggle;

    [Header("Market UI")]
    public GameObject marketPanel;

    // 🔥 Leaderboard Paneli
    [Header("Leaderboard UI")]
    public GameObject leaderboardPanel;

    // 🔥 Coin Shop Paneli
    [Header("Coin Shop UI")]
    public GameObject coinShopPanel;

    // 🔥 YENİ: Görevler ve Başarılar Paneli
    [Header("Missions UI")]
    public GameObject missionsPanel;

    // 🔥 YENİ: Power-Up Shop (Boost Marketi) Paneli
    [Header("Power-Up Shop UI")]
    public GameObject powerUpShopPanel;

    [Header("Pause UI")]
    public GameObject pausePanel;

    [Header("Geri Sayım")]
    public TextMeshProUGUI countdownText;

    // 🔥 Kaydırma (Slide) Animasyon Ayarları
    [Header("Kaydırma (Slide) Animasyon Ayarları")]
    [Tooltip("Marketin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection marketSlideDirection = SlideDirection.Right;

    [Tooltip("Leaderboard panelinin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection leaderboardSlideDirection = SlideDirection.Bottom;

    [Tooltip("Coin Shop panelinin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection coinShopSlideDirection = SlideDirection.Left;

    [Tooltip("Görevler (Missions) panelinin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection missionsSlideDirection = SlideDirection.Right;

    [Tooltip("Power-Up Shop panelinin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection powerUpShopSlideDirection = SlideDirection.Bottom;

    [Tooltip("Game Over panelinin hangi yönden kayarak geleceğini seçin.")]
    public SlideDirection gameOverSlideDirection = SlideDirection.Top;

    public float slideSpeed = 4f;

    // 🔥 Sahne Geçiş Paneli (Siyah Ekran)
    [Header("Sahne Geçişi")]
    public Image fadeOverlay;
    [Tooltip("Geçiş süresi (Saniye).")]
    public float sceneTransitionDuration = 0.5f;

    [Header("Animasyon Ayarları")]
    public float panelPopUpSpeed = 10f;

    public bool isPaused = false;
    private bool isCountDownActive = false; // Geri sayım kilidi

    // Kayıt Anahtarları
    private const string MusicKey = "IsMusicOn";
    private const string EffectsKey = "IsEffectsOn";
    private const string ControlModeKey = "ControlMode"; // 0: Swipe, 1: Button
    private const string VibrationKey = "IsVibrationOn";

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    void Start()
    {
        LoadSettings();

        if (musicToggle != null) musicToggle.onValueChanged.AddListener(SetMusic);
        if (effectsToggle != null) effectsToggle.onValueChanged.AddListener(SetEffects);
        if (controlModeToggle != null) controlModeToggle.onValueChanged.AddListener(SetControlMode);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.AddListener(SetVibration);

        // Panelleri Başlangıçta Kapat ve Boyutlarını Sıfırla
        if (settingsPanel != null)
        {
            settingsPanel.transform.localScale = Vector3.zero;
            settingsPanel.SetActive(false);
        }
        if (marketPanel != null)
        {
            marketPanel.transform.localScale = Vector3.zero;
            marketPanel.SetActive(false);
        }
        if (leaderboardPanel != null)
        {
            leaderboardPanel.transform.localScale = Vector3.zero;
            leaderboardPanel.SetActive(false);
        }
        if (coinShopPanel != null)
        {
            coinShopPanel.transform.localScale = Vector3.zero;
            coinShopPanel.SetActive(false);
        }
        if (missionsPanel != null)
        {
            missionsPanel.transform.localScale = Vector3.zero;
            missionsPanel.SetActive(false);
        }

        // Başlangıçta Power-Up panelini de gizle ve küçült
        if (powerUpShopPanel != null)
        {
            powerUpShopPanel.transform.localScale = Vector3.zero;
            powerUpShopPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.transform.localScale = Vector3.zero;
            pausePanel.SetActive(false);
        }

        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // BAŞLANGIÇTA FADE IN (Siyah ekrandan açılış)
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.color = Color.black;
            StartCoroutine(FadeInRoutine());
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (pausePanel != null && !isPaused && !isCountDownActive)
            {
                if (GameManager.instance != null && GameManager.instance.isGameActive)
                {
                    PauseGame();
                }
            }
        }
        else
        {
            Application.targetFrameRate = 120;
        }
    }

    // --- OYUN DURDURMA VE DEVAM ETME ---
    public void PauseGame()
    {
        if (isCountDownActive || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            StartCoroutine(OpenPanelRoutine(pausePanel));
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            StartCoroutine(ClosePanelRoutine(pausePanel, () =>
            {
                StartCoroutine(ResumeCountdownRoutine());
            }));
        }
        else
        {
            StartCoroutine(ResumeCountdownRoutine());
        }
    }

    IEnumerator ResumeCountdownRoutine()
    {
        isCountDownActive = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            yield return StartCoroutine(AnimateCountdownStep("3", 0.5f));
            yield return StartCoroutine(AnimateCountdownStep("2", 0.5f));
            yield return StartCoroutine(AnimateCountdownStep("1", 0.5f));
            yield return StartCoroutine(AnimateCountdownStep("GO!", 0.5f));

            countdownText.gameObject.SetActive(false);
        }

        isPaused = false;
        Time.timeScale = 1f;
        isCountDownActive = false;
    }

    IEnumerator AnimateCountdownStep(string text, float duration)
    {
        countdownText.text = text;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            float scale = Mathf.Lerp(2.5f, 1.0f, t);
            countdownText.transform.localScale = Vector3.one * scale;

            float alpha = 0f;
            if (t < 0.2f) alpha = Mathf.Lerp(0f, 1f, t / 0.2f);
            else alpha = Mathf.Lerp(1f, 0f, (t - 0.2f) / 0.8f);

            countdownText.alpha = alpha;

            yield return null;
        }
    }

    // --- ANİMASYON RUTİNLERİ ---
    IEnumerator OpenPanelRoutine(GameObject panel)
    {
        if (panel == null) yield break;
        panel.SetActive(true);
        panel.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * panelPopUpSpeed;
            panel.transform.localScale = Vector3.one * Mathf.Lerp(0, 1, t);
            yield return null;
        }
        panel.transform.localScale = Vector3.one;
    }

    IEnumerator ClosePanelRoutine(GameObject panel, System.Action onComplete = null)
    {
        if (panel == null) yield break;

        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime * panelPopUpSpeed;
            panel.transform.localScale = Vector3.one * Mathf.Lerp(1, 0, t);
            yield return null;
        }

        panel.transform.localScale = Vector3.zero;
        panel.SetActive(false);
        onComplete?.Invoke();
    }

    // KAYDIRMA (SLIDE) İÇERİ
    IEnumerator SlidePanelInRoutine(GameObject panel, SlideDirection dir)
    {
        if (panel == null) yield break;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null)
        {
            yield return StartCoroutine(OpenPanelRoutine(panel));
            yield break;
        }

        panel.SetActive(true);
        panel.transform.localScale = Vector3.one;

        Vector2 startPos = Vector2.zero;
        Canvas parentCanvas = panel.GetComponentInParent<Canvas>();
        float w = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>().rect.width : Screen.width;
        float h = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>().rect.height : Screen.height;

        switch (dir)
        {
            case SlideDirection.Top: startPos = new Vector2(0, h); break;
            case SlideDirection.Bottom: startPos = new Vector2(0, -h); break;
            case SlideDirection.Left: startPos = new Vector2(-w, 0); break;
            case SlideDirection.Right: startPos = new Vector2(w, 0); break;
        }

        rect.anchoredPosition = startPos;
        float t = 0;

        while (t < 1)
        {
            t += Time.unscaledDeltaTime * slideSpeed;
            float easeT = t * t * (3f - 2f * t); // Smooth step
            rect.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, easeT);
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero;
    }

    // KAYDIRMA (SLIDE) DIŞARI
    IEnumerator SlidePanelOutRoutine(GameObject panel, SlideDirection dir)
    {
        if (panel == null) yield break;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null)
        {
            yield return StartCoroutine(ClosePanelRoutine(panel));
            yield break;
        }

        Vector2 endPos = Vector2.zero;
        Canvas parentCanvas = panel.GetComponentInParent<Canvas>();
        float w = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>().rect.width : Screen.width;
        float h = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>().rect.height : Screen.height;

        switch (dir)
        {
            case SlideDirection.Top: endPos = new Vector2(0, h); break;
            case SlideDirection.Bottom: endPos = new Vector2(0, -h); break;
            case SlideDirection.Left: endPos = new Vector2(-w, 0); break;
            case SlideDirection.Right: endPos = new Vector2(w, 0); break;
        }

        Vector2 startPos = Vector2.zero; // Merkezden dışarı
        float t = 0;

        while (t < 1)
        {
            t += Time.unscaledDeltaTime * slideSpeed;
            float easeT = t * t * (3f - 2f * t);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
            yield return null;
        }

        rect.anchoredPosition = endPos;
        panel.SetActive(false);
    }

    // --- SAHNE GEÇİŞLERİ ---
    IEnumerator SceneTransitionRoutine(string sceneName)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float t = 0;
            while (t < sceneTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float alpha = t / sceneTransitionDuration;
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlay.color = Color.black;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeInRoutine()
    {
        if (fadeOverlay != null)
        {
            float t = 0;
            while (t < sceneTransitionDuration)
            {
                t += Time.unscaledDeltaTime;
                float alpha = 1f - (t / sceneTransitionDuration);
                fadeOverlay.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    // --- BUTON FONKSİYONLARI ---
    public void StartGame() { StartCoroutine(SceneTransitionRoutine("GameScene")); }
    public void GoToShop() { Time.timeScale = 1f; OpenMarket(); }
    public void LoadMainMenu() { StartCoroutine(SceneTransitionRoutine("MainMenu")); }
    public void RestartGame() { StartCoroutine(SceneTransitionRoutine(SceneManager.GetActiveScene().name)); }
    public void QuitGame() { Debug.Log("Oyundan Çıkıldı."); Application.Quit(); }

    // --- MARKET KONTROLLERİ ---
    public void OpenMarket()
    {
        if (marketPanel != null)
        {
            StartCoroutine(SlidePanelInRoutine(marketPanel, marketSlideDirection));
            if (MarketManager.instance != null) MarketManager.instance.UpdateUI();
        }
    }
    public void CloseMarket()
    {
        if (marketPanel != null) StartCoroutine(SlidePanelOutRoutine(marketPanel, marketSlideDirection));
    }

    // --- COIN SHOP KONTROLLERİ ---
    public void OpenCoinShop()
    {
        if (coinShopPanel != null)
        {
            StartCoroutine(SlidePanelInRoutine(coinShopPanel, coinShopSlideDirection));
        }
    }

    public void CloseCoinShop()
    {
        if (coinShopPanel != null)
        {
            StartCoroutine(SlidePanelOutRoutine(coinShopPanel, coinShopSlideDirection));
        }
    }

    // --- YENİ: GÖREVLER KONTROLLERİ ---
    public void OpenMissions()
    {
        if (missionsPanel != null)
        {
            StartCoroutine(SlidePanelInRoutine(missionsPanel, missionsSlideDirection));

            // Eğer gelecekte bir MissionsManager script'i bağlarsan, açılırken listeyi günceller
            missionsPanel.BroadcastMessage("UpdateUI", SendMessageOptions.DontRequireReceiver);
        }
    }

    public void CloseMissions()
    {
        if (missionsPanel != null)
        {
            StartCoroutine(SlidePanelOutRoutine(missionsPanel, missionsSlideDirection));
        }
    }

    // --- YENİ: POWER-UP SHOP (BOOST MARKETİ) KONTROLLERİ ---
    public void OpenPowerUpShop()
    {
        if (powerUpShopPanel != null)
        {
            StartCoroutine(SlidePanelInRoutine(powerUpShopPanel, powerUpShopSlideDirection));
        }
    }

    public void ClosePowerUpShop()
    {
        if (powerUpShopPanel != null)
        {
            StartCoroutine(SlidePanelOutRoutine(powerUpShopPanel, powerUpShopSlideDirection));
        }
    }

    // --- LEADERBOARD KONTROLLERİ ---
    public void OpenLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            StartCoroutine(SlidePanelInRoutine(leaderboardPanel, leaderboardSlideDirection));
            leaderboardPanel.BroadcastMessage("UpdateUI", SendMessageOptions.DontRequireReceiver);
        }
    }
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            StartCoroutine(SlidePanelOutRoutine(leaderboardPanel, leaderboardSlideDirection));
        }
    }

    // --- AYARLAR KONTROLLERİ ---
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            StartCoroutine(OpenPanelRoutine(settingsPanel));
            LoadSettings();
        }
    }
    public void CloseSettings()
    {
        if (settingsPanel != null) StartCoroutine(ClosePanelRoutine(settingsPanel));
    }

    // --- TOGGLE FONKSİYONLARI ---
    public void SetMusic(bool isOn) { PlayerPrefs.SetInt(MusicKey, isOn ? 1 : 0); PlayerPrefs.Save(); }
    public void SetEffects(bool isOn) { PlayerPrefs.SetInt(EffectsKey, isOn ? 1 : 0); PlayerPrefs.Save(); }
    public void SetControlMode(bool isButtonMode) { PlayerPrefs.SetInt(ControlModeKey, isButtonMode ? 1 : 0); PlayerPrefs.Save(); }
    public void SetVibration(bool isOn)
    {
        if (GameManager.instance != null) GameManager.instance.enableVibration = isOn;
        PlayerPrefs.SetInt(VibrationKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        bool isMusicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        if (musicToggle != null) musicToggle.isOn = isMusicOn;

        bool isEffectsOn = PlayerPrefs.GetInt(EffectsKey, 1) == 1;
        if (effectsToggle != null) effectsToggle.isOn = isEffectsOn;

        bool isButtonMode = PlayerPrefs.GetInt(ControlModeKey, 0) == 1;
        if (controlModeToggle != null) controlModeToggle.isOn = isButtonMode;

        bool isVibrationOn = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        if (vibrationToggle != null) vibrationToggle.isOn = isVibrationOn;

        if (GameManager.instance != null) GameManager.instance.enableVibration = isVibrationOn;
    }
}