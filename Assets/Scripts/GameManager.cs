using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI - Skor & Para")]
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI gemText;

    [Header("UI - Hız Göstergesi")]
    public TextMeshProUGUI speedText;
    public float speedDisplayMultiplier = 10f;

    [Header("UI - Özellikler")]
    public TextMeshProUGUI invincibilityTimerText;

    [Header("Envanter (Özel Yetenekler)")]
    public int currentBoostAmount = 0;
    private const string BoostKey = "PlayerBoosts";

    [Header("Görsel Efektler")]
    public GameObject invincibilityEffectObject;
    public Image invincibilityOverlay;
    [Range(0f, 1f)] public float maxOverlayAlpha = 0.3f;
    public float effectFadeInDuration = 0.5f;
    public float effectFadeOutDuration = 2.0f;

    [Header("Bonus Efektleri")]
    public GameObject scorePopupPrefab;

    [Header("Çarpışma Efektleri")]
    public GameObject crashEffectPrefab;
    public GameObject invincibilityCrashEffect;
    public GameObject boostCrashEffect;

    [Header("Game Over Efektleri")]
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;
    public float panelDelay = 0.2f;

    [Header("Game Over Kaydırma Animasyonu")]
    public SlideDirection gameOverSlideDirection = SlideDirection.Top;
    public float slideSpeed = 4f;
    [SerializeField] private float slideDistance = 400f;

    [Header("High Score Animasyonu")]
    [SerializeField] private float highScorePunchScale = 1.35f;
    [SerializeField] private float highScoreAnimDuration = 0.45f;
    [SerializeField] private Color highScoreColor = Color.green;

    [Header("Near Miss Streak")]
    [SerializeField] public float streak1Duration = 6f;
    [SerializeField] public float streak2Duration = 4f;
    [SerializeField] public float streak3Duration = 2.5f;

    [Header("Restart Butonu Animasyonu")]
    [SerializeField] private GameObject restartButton;
    [SerializeField] private float restartPulseSpeed = 3f;
    [SerializeField] private float restartPulseAmount = 0.08f;

    public Image gameOverOverlay;
    public float gameOverFadeDuration = 0.5f;
    [Range(0f, 1f)] public float maxGameOverAlpha = 0.7f;

    [Header("Skor Ayarları")]
    public float baseScoreSpeed = 50f;
    public float multiplierIncreaseInterval = 10f;
    public float multiplierStep = 0.5f;
    private float currentScoreMultiplier = 1.0f;
    private float scoreTimer = 0f;

    public bool isBoosting = false;

    [Header("Ayarlar")]
    public bool enableVibration = true;
    private const string VibrationKey = "IsVibrationOn";

    public static int gems = 0;
    [HideInInspector] public int sessionGems = 0;
    private float score = 0f;
    public bool IsGameOver { get; private set; } = false;
    public bool isGameActive = false;

    private const string HighScoreKey = "HighScore";
    private const string GemsKey = "GemsCount";

    public bool IsInvincible { get; private set; } = false;
    private Coroutine invincibilityCoroutine;

    public int NearMissStreakCount => nearMissStreakCount;
    public float NearMissStreakTimer => nearMissStreakTimer;
    public float NearMissCurrentDuration => nearMissCurrentDuration;
    [HideInInspector] public float currentInvincibilityTimer = 0f;
    [HideInInspector] public float invincibilityMaxDuration = 0f;

    private SpriteRenderer playerRenderer;
    private SpriteRenderer shieldSpriteRenderer;
    private Vector3 originalPanelScale;
    private Vector2 originalPanelPosition;
    private bool _isNewHighScore;

    private int nearMissStreakCount = 0;
    private float nearMissStreakTimer = 0f;
    private float nearMissMultiplier = 1f;
    private float nearMissCurrentDuration = 6f;
    private Transform playerTransform;

    [Header("Kontrol UI")]
    public GameObject controlsPanel;
    private const string ControlModeKey = "ControlMode";

    void Awake()
    {
        if (instance == null) { instance = this; }
        else if (instance != this) { Destroy(gameObject); }
    }

    void Start()
    {
        LoadGems();
        UpdateGemUI();

        enableVibration = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
        currentBoostAmount = PlayerPrefs.GetInt(BoostKey, 0);

        score = 0f;
        sessionGems = 0;
        IsGameOver = false;
        isGameActive = false;
        isBoosting = false;

        nearMissStreakCount = 0;
        nearMissStreakTimer = 0f;
        nearMissMultiplier = 1f;
        nearMissCurrentDuration = streak1Duration;

        if (speedText != null) speedText.text = "60 km/h";

        if (gameOverPanel != null)
        {
            originalPanelScale = gameOverPanel.transform.localScale;
            originalPanelPosition = gameOverPanel.GetComponent<RectTransform>().anchoredPosition;
            gameOverPanel.SetActive(false);
        }

        if (gameOverOverlay != null)
        {
            Color c = gameOverOverlay.color;
            c.a = 0f;
            gameOverOverlay.color = c;
            gameOverOverlay.gameObject.SetActive(false);
        }

        if (invincibilityOverlay != null)
        {
            Color c = invincibilityOverlay.color;
            c.a = 0f;
            invincibilityOverlay.color = c;
            invincibilityOverlay.gameObject.SetActive(false);
        }

        StartCoroutine(GameStartSequence());
    }

    IEnumerator GameStartSequence()
    {
        // Wait one frame for PlayerSpawner to instantiate the car
        yield return null;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerRenderer = playerObj.GetComponent<SpriteRenderer>();

            Transform shieldChild = FindDeepChild(playerObj.transform, "Kalkan Efekti");
            if (shieldChild != null)
            {
                shieldChild.gameObject.SetActive(false);
                shieldSpriteRenderer = shieldChild.GetComponentInChildren<SpriteRenderer>(true);
                if (shieldSpriteRenderer != null)
                {
                    Color c = shieldSpriteRenderer.color;
                    c.a = 0f;
                    shieldSpriteRenderer.color = c;
                }
            }
        }

        if (controlsPanel != null) controlsPanel.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        if (controlsPanel != null) controlsPanel.SetActive(false);

        isGameActive = true;

        if (ObstacleManager.instance != null)
            ObstacleManager.instance.StartGameLoop();
    }

    void Update()
    {
        if (isGameActive && !IsGameOver)
        {
            if (nearMissStreakCount > 0)
            {
                nearMissStreakTimer -= Time.deltaTime;
                if (nearMissStreakTimer <= 0f)
                {
                    nearMissStreakCount = 0;
                    nearMissMultiplier = 1f;
                }
            }

            scoreTimer += Time.deltaTime;
            if (scoreTimer >= multiplierIncreaseInterval) { currentScoreMultiplier += multiplierStep; scoreTimer = 0f; }
            score += baseScoreSpeed * currentScoreMultiplier * (isBoosting ? 2.0f : 1.0f) * nearMissMultiplier * Time.deltaTime;
            if (scoreText != null) scoreText.text = Mathf.FloorToInt(score).ToString();

            if (speedText != null && ObstacleManager.instance != null)
                speedText.text = Mathf.FloorToInt(ObstacleManager.scrollSpeed * speedDisplayMultiplier).ToString() + " km/h";
        }
    }

    // --- DOKUNULMAZLIK ---
    public void ActivateInvincibility(float duration)
    {
        invincibilityMaxDuration = duration;
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        IsInvincible = true;
        currentInvincibilityTimer = duration;

        // Activate effect and shield immediately at full visibility
        if (invincibilityEffectObject != null) invincibilityEffectObject.SetActive(true);

        if (shieldSpriteRenderer != null)
        {
            shieldSpriteRenderer.gameObject.transform.parent.gameObject.SetActive(true);
            Color c = shieldSpriteRenderer.color; c.a = 1f; shieldSpriteRenderer.color = c;
        }

        // Overlay fades in from 0
        if (invincibilityOverlay != null)
        {
            invincibilityOverlay.gameObject.SetActive(true);
            Color c = invincibilityOverlay.color; c.a = 0f; invincibilityOverlay.color = c;
        }

        // Fade-in phase
        float fadeInT = 0f;
        while (fadeInT < 1f && currentInvincibilityTimer > 0)
        {
            fadeInT += Time.deltaTime / effectFadeInDuration;
            currentInvincibilityTimer -= Time.deltaTime;

            if (invincibilityOverlay != null)
            { Color c = invincibilityOverlay.color; c.a = Mathf.Lerp(0f, maxOverlayAlpha, Mathf.Clamp01(fadeInT)); invincibilityOverlay.color = c; }

            if (invincibilityTimerText != null)
                invincibilityTimerText.text = "Kalkan: " + Mathf.CeilToInt(currentInvincibilityTimer).ToString();

            yield return null;
        }

        if (invincibilityOverlay != null)
        { Color c = invincibilityOverlay.color; c.a = maxOverlayAlpha; invincibilityOverlay.color = c; }

        // Single countdown loop — fade out during the last effectFadeOutDuration seconds
        while (currentInvincibilityTimer > 0)
        {
            currentInvincibilityTimer -= Time.deltaTime;

            if (invincibilityTimerText != null)
                invincibilityTimerText.text = "Kalkan: " + Mathf.CeilToInt(currentInvincibilityTimer).ToString();

            if (currentInvincibilityTimer < effectFadeOutDuration)
            {
                float alpha = Mathf.Clamp01(currentInvincibilityTimer / effectFadeOutDuration);

                if (shieldSpriteRenderer != null)
                { Color c = shieldSpriteRenderer.color; c.a = alpha; shieldSpriteRenderer.color = c; }

                if (invincibilityOverlay != null)
                { Color c = invincibilityOverlay.color; c.a = maxOverlayAlpha * alpha; invincibilityOverlay.color = c; }
            }

            yield return null;
        }

        // Timer hit 0 — everything off simultaneously
        IsInvincible = false;
        currentInvincibilityTimer = 0f;

        if (invincibilityTimerText != null) invincibilityTimerText.text = "";
        if (invincibilityEffectObject != null) invincibilityEffectObject.SetActive(false);

        if (shieldSpriteRenderer != null)
        {
            Color c = shieldSpriteRenderer.color; c.a = 0f; shieldSpriteRenderer.color = c;
            shieldSpriteRenderer.gameObject.transform.parent.gameObject.SetActive(false);
        }
        if (invincibilityOverlay != null)
        {
            Color c = invincibilityOverlay.color; c.a = 0f; invincibilityOverlay.color = c;
            invincibilityOverlay.gameObject.SetActive(false);
        }
    }

    // --- OYUN BİTİŞİ ---
    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        isGameActive = false;

        SaveHighScore();
        MissionsManager.AddGameplayProgress(MissionType.ReachScore, Mathf.FloorToInt(score));

        StartCoroutine(ShowGameOverPanelRoutine());
    }

    public void TriggerCrash(Vector3 pos)
    {
        if (IsGameOver || IsInvincible) return;
        IsGameOver = true;
        isGameActive = false;

        SaveHighScore();
        MissionsManager.AddGameplayProgress(MissionType.ReachScore, Mathf.FloorToInt(score));

        if (crashEffectPrefab != null) Instantiate(crashEffectPrefab, pos, Quaternion.identity);

        if (AudioManager.instance != null && AudioManager.instance.crashSound != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.crashSound);

        if (CameraShake.instance != null)
            CameraShake.instance.Shake(shakeDuration, shakeMagnitude);

        StartCoroutine(ShowGameOverPanelRoutine());
    }

    public void TriggerBoostCrash(Vector3 pos) { if (boostCrashEffect != null) Instantiate(boostCrashEffect, pos, Quaternion.identity); }
    public void TriggerInvincibleCrash(Vector3 pos) { if (invincibilityCrashEffect != null) Instantiate(invincibilityCrashEffect, pos, Quaternion.identity); }

    public void ShowFloatingText(string text, Vector3 pos)
    {
        if (scorePopupPrefab != null)
        {
            GameObject go = Instantiate(scorePopupPrefab, pos, Quaternion.identity);
            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.SetText(text);
        }
    }

    public void TriggerNearMissStreak()
    {
        if (!isGameActive || IsGameOver) return;

        if (nearMissStreakCount >= 3)
        {
            nearMissStreakTimer = nearMissCurrentDuration;
            return;
        }

        nearMissStreakCount++;

        switch (nearMissStreakCount)
        {
            case 1: nearMissMultiplier = 1.5f; nearMissCurrentDuration = streak1Duration; break;
            case 2: nearMissMultiplier = 2f;   nearMissCurrentDuration = streak2Duration; break;
            case 3: nearMissMultiplier = 4f;   nearMissCurrentDuration = streak3Duration; break;
        }

        nearMissStreakTimer = nearMissCurrentDuration;
    }

    public bool UseBoostItem()
    {
        int currentBoosts = PlayerPrefs.GetInt(BoostKey, 0);
        if (currentBoosts > 0) { currentBoosts--; PlayerPrefs.SetInt(BoostKey, currentBoosts); PlayerPrefs.Save(); return true; }
        return false;
    }

    IEnumerator ShowGameOverPanelRoutine()
    {
        yield return new WaitForSecondsRealtime(panelDelay);

        Time.timeScale = 0f;

        // --- Set up panel before animation starts ---
        RectTransform rect = gameOverPanel != null ? gameOverPanel.GetComponent<RectTransform>() : null;
        Vector2 startPos = Vector2.zero;

        if (gameOverPanel != null)
        {
            if (finalScoreText != null) finalScoreText.text = Mathf.FloorToInt(score).ToString();
            UpdateHighScoreUI();
            if (_isNewHighScore) StartCoroutine(AnimateHighScoreText());

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.localScale = originalPanelScale;

            if (rect != null)
            {
                switch (gameOverSlideDirection)
                {
                    case SlideDirection.Bottom: startPos = new Vector2(0, -slideDistance); break;
                    case SlideDirection.Left:   startPos = new Vector2(-slideDistance, 0); break;
                    case SlideDirection.Right:  startPos = new Vector2(slideDistance, 0);  break;
                    default:                    startPos = new Vector2(0, slideDistance);   break;
                }
                rect.anchoredPosition = startPos;
            }
        }

        if (gameOverOverlay != null) gameOverOverlay.gameObject.SetActive(true);

        // --- Overlay fade + panel slide run at the same time ---
        float overlayT = 0f;
        float slideT   = 0f;
        float panelSlideSpeed = slideSpeed * 0.55f; // panel slides a bit slower than overlay

        while (overlayT < 1f || slideT < 1f)
        {
            overlayT += Time.unscaledDeltaTime / gameOverFadeDuration;

            if (gameOverOverlay != null)
            {
                Color c = gameOverOverlay.color;
                c.a = Mathf.Lerp(0f, maxGameOverAlpha, Mathf.Clamp01(overlayT));
                gameOverOverlay.color = c;
            }

            if (rect != null)
            {
                slideT += Time.unscaledDeltaTime * panelSlideSpeed;
                float easeT = slideT * slideT * (3f - 2f * slideT);
                rect.anchoredPosition = Vector2.Lerp(startPos, originalPanelPosition, Mathf.Clamp01(easeT));
            }
            else
            {
                slideT = 1f;
            }

            yield return null;
        }

        // Snap to final values
        if (gameOverOverlay != null)
        { Color c = gameOverOverlay.color; c.a = maxGameOverAlpha; gameOverOverlay.color = c; }
        if (rect != null)
            rect.anchoredPosition = originalPanelPosition;

        StartCoroutine(PulseRestartButtonRoutine());
    }

    private void SaveHighScore()
    {
        int currentHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        int currentScore = Mathf.FloorToInt(score);
        _isNewHighScore = currentScore > currentHighScore;
        if (_isNewHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, currentScore);
            PlayerPrefs.Save();
        }
    }

    IEnumerator AnimateHighScoreText()
    {
        if (highScoreText == null) yield break;

        highScoreText.color = highScoreColor;
        Vector3 originalScale = highScoreText.transform.localScale;
        float half = highScoreAnimDuration * 0.5f;

        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / half;
                float s = Mathf.Lerp(1f, highScorePunchScale, Mathf.Clamp01(t));
                highScoreText.transform.localScale = originalScale * s;
                yield return null;
            }

            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / half;
                float s = Mathf.Lerp(highScorePunchScale, 1f, Mathf.Clamp01(t));
                highScoreText.transform.localScale = originalScale * s;
                yield return null;
            }
        }
    }

    IEnumerator PulseRestartButtonRoutine()
    {
        if (restartButton == null) yield break;

        Vector3 baseScale = restartButton.transform.localScale;
        float elapsed = 0f;

        while (true)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(elapsed * restartPulseSpeed) * restartPulseAmount;
            restartButton.transform.localScale = baseScale * pulse;
            yield return null;
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindDeepChild(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private void LoadGems() => gems = PlayerPrefs.GetInt(GemsKey, 0);
    private void SaveGems() { PlayerPrefs.SetInt(GemsKey, gems); PlayerPrefs.Save(); }
    public void UpdateGemUI() { if (gemText != null) gemText.text = gems.ToString(); }
    private void UpdateHighScoreUI() { if (highScoreText != null) highScoreText.text = PlayerPrefs.GetInt(HighScoreKey, 0).ToString(); }
    public void AddGems(int amount) { gems += amount; sessionGems += amount; SaveGems(); UpdateGemUI(); }
}
