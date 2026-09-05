using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class BuffTimerUI : MonoBehaviour
{
    public enum BuffType
    {
        Boost,          // Öfke Modu (Alev)
        Shield,         // Görünmezlik (Kalkan)
        SpeedReducer    // Yavaşlatıcı
    }

    public enum TimerMode
    {
        ActiveTime,     // Yetenek KULLANILIRKEN süreyi göster (Yuvarlaklar için)
        CooldownTime    // Yetenek BİTİNCE bekleme süresini göster (Alt Bar için)
    }

    [Header("Ne Takip Edilecek?")]
    [Tooltip("Hangi yeteneği takip edeceğiz?")]
    public BuffType buffType;

    [Tooltip("Aktiflik süresi mi (Yuvarlak) yoksa Bekleme Süresi mi (Alt Bar)?")]
    public TimerMode timerMode = TimerMode.ActiveTime;

    [Header("Görsel Ayarları")]
    [Tooltip("Dolup boşalan renk/resim objesi (Image Type KESİNLİKLE Filled olmalı)")]
    public Image fillImage;

    [Tooltip("TİK AÇIKSA: Bar merkezden sağa ve sola doğru dolar/boşalır. (Alt Bar İçin)\nTİK KAPALIYSA: Klasik Fill Amount (Yuvarlak) kullanır.")]
    public bool fillFromCenter = false;

    [Header("Dolunca Parlama (Glow) Efekti")]
    [Tooltip("Bar tamamen dolup (yetenek kullanıma hazır) olduğunda parlayacak görsel. Genelde çerçeve (Frame) buraya atanır. Boş bırakılırsa efekt uygulanmaz.")]
    public Image glowImage;

    [Tooltip("Hazır olduğunda döngünün başladığı/bittiği renk.")]
    public Color glowColorStart = Color.white;

    [Tooltip("Döngünün orta rengi.")]
    public Color glowColorMid = new Color(1f, 0.92f, 0.2f, 1f);

    [Tooltip("Döngünün en uç (dikkat çeken) rengi.")]
    public Color glowColorEnd = new Color(1f, 0.15f, 0.15f, 1f);

    [Tooltip("Beyaz -> Sarı -> Kırmızı -> Beyaz döngüsünün bir turunun kaç saniye sürdüğü. Bar dolu kaldığı sürece bu döngü, oyuncuyu yeteneği kullanmaya davet etmek için sürekli tekrar eder.")]
    public float glowCycleDuration = 1.5f;

    [Tooltip("Bar tam dolduğu anda oynatılan küçük 'pop' büyüme efektinin oranı.")]
    public float readyPunchScale = 1.2f;

    [Tooltip("Pop efektinin toplam süresi (saniye).")]
    public float readyPunchDuration = 0.2f;

    private CanvasGroup canvasGroup;
    private CarController2D playerCar;

    // Yavaşlatıcı için özel sayaç
    private float internalReducerTimer = 0f;
    private bool wasReducerActiveLastFrame = false;

    private Color glowBaseColor = Color.white;
    private bool glowBaseColorCached = false;
    private bool wasFull = false;
    private float punchTimer = 0f;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        canvasGroup.alpha = (timerMode == TimerMode.CooldownTime) ? 1f : 0f;
    }

    void Update()
    {
        float ratio = 0f;
        float targetAlpha = 0f;

        switch (buffType)
        {
            case BuffType.Boost:
                if (playerCar == null)
                {
                    GameObject p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) playerCar = p.GetComponent<CarController2D>();
                }

                if (playerCar != null)
                {
                    if (timerMode == TimerMode.ActiveTime)
                    {
                        if (playerCar.isBoostActive)
                        {
                            targetAlpha = 1f;
                            ratio = playerCar.currentBoostTimer / playerCar.boostDuration;
                        }
                        else
                        {
                            targetAlpha = 0f;
                            ratio = 0f;
                        }
                    }
                    else if (timerMode == TimerMode.CooldownTime)
                    {
                        targetAlpha = 1f;

                        if (playerCar.isBoostActive)
                        {
                            ratio = 0f;
                        }
                        else if (playerCar.isBoostOnCooldown)
                        {
                            ratio = 1f - (playerCar.currentCooldownTimer / playerCar.boostCooldown);
                        }
                        else
                        {
                            ratio = 1f;
                        }
                    }
                }
                break;

            case BuffType.Shield:
                if (GameManager.instance != null)
                {
                    if (timerMode == TimerMode.ActiveTime && GameManager.instance.IsInvincible)
                    {
                        targetAlpha = 1f;
                        ratio = GameManager.instance.currentInvincibilityTimer / GameManager.instance.invincibilityMaxDuration;
                    }
                    else if (timerMode == TimerMode.CooldownTime)
                    {
                        targetAlpha = 1f;
                        ratio = 1f;
                    }
                }
                break;

            case BuffType.SpeedReducer:
                if (ObstacleManager.instance != null)
                {
                    bool isReducerActiveNow = ObstacleManager.instance.isSpeedReduced;

                    if (timerMode == TimerMode.ActiveTime)
                    {
                        if (isReducerActiveNow)
                        {
                            targetAlpha = 1f;

                            // Yeni başladıysa sayacı fulle
                            if (!wasReducerActiveLastFrame)
                            {
                                internalReducerTimer = ObstacleManager.instance.reductionDuration;
                            }

                            // Sayacı azalt
                            internalReducerTimer -= Time.deltaTime;

                            // Oranı hesapla
                            ratio = internalReducerTimer / ObstacleManager.instance.reductionDuration;
                        }
                        else
                        {
                            targetAlpha = 0f;
                            ratio = 0f;
                            internalReducerTimer = 0f;
                        }
                    }
                    else if (timerMode == TimerMode.CooldownTime)
                    {
                        targetAlpha = 1f;
                        ratio = 1f;
                    }

                    wasReducerActiveLastFrame = isReducerActiveNow;
                }
                break;
        }

        // --- GÖRSELİ UYGULA (MILISANIYESINE KADAR SENKRONIZE) ---
        canvasGroup.alpha = targetAlpha;
        ratio = Mathf.Clamp01(ratio);

        // --- DOLUNCA PARLAMA (Yetenek kullanıma hazır olduğunu vurgular) ---
        bool isFullNow = timerMode == TimerMode.CooldownTime && targetAlpha > 0f && ratio >= 0.999f;
        if (isFullNow && !wasFull) punchTimer = readyPunchDuration;
        wasFull = isFullNow;

        float punchMultiplier = 1f;
        if (punchTimer > 0f)
        {
            punchTimer -= Time.unscaledDeltaTime;
            float half = readyPunchDuration * 0.5f;
            float elapsed = readyPunchDuration - Mathf.Max(punchTimer, 0f);
            float t = elapsed < half ? (elapsed / half) : (1f - (elapsed - half) / half);
            punchMultiplier = Mathf.Lerp(1f, readyPunchScale, Mathf.Clamp01(t));
        }

        // Dolum ve "pop" büyümesi AYNI transform üzerinde uygulanıyorsa (ör. glowImage,
        // fillImage'ın kendisiyse) ikisini tek seferde birleştiriyoruz; aksi halde ikinci
        // bir yazma aynı frame'de birincisini geçersiz kılıp titremeye yol açardı.
        bool glowSharesFillTransform = glowImage != null && fillImage != null && glowImage.rectTransform == fillImage.rectTransform;

        if (fillImage != null)
        {
            float sharedPunch = glowSharesFillTransform ? punchMultiplier : 1f;

            if (fillFromCenter)
            {
                fillImage.rectTransform.localScale = new Vector3(ratio * sharedPunch, sharedPunch, 1f);
            }
            else
            {
                fillImage.fillAmount = ratio;
                fillImage.rectTransform.localScale = Vector3.one * sharedPunch;
            }
        }

        if (glowImage != null)
        {
            if (!glowBaseColorCached)
            {
                glowBaseColor = glowImage.color;
                glowBaseColorCached = true;
            }

            if (isFullNow)
            {
                float cycleT = Mathf.Repeat(Time.unscaledTime, glowCycleDuration) / glowCycleDuration;
                float segment = cycleT * 3f; // 0-1: start->mid, 1-2: mid->end, 2-3: end->start

                if (segment < 1f)
                    glowImage.color = Color.Lerp(glowColorStart, glowColorMid, segment);
                else if (segment < 2f)
                    glowImage.color = Color.Lerp(glowColorMid, glowColorEnd, segment - 1f);
                else
                    glowImage.color = Color.Lerp(glowColorEnd, glowColorStart, segment - 2f);
            }
            else
            {
                glowImage.color = glowBaseColor;
            }

            if (!glowSharesFillTransform)
            {
                glowImage.rectTransform.localScale = Vector3.one * punchMultiplier;
            }
        }
    }
}