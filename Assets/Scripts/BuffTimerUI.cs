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

    private CanvasGroup canvasGroup;
    private CarController2D playerCar;

    // Yavaşlatıcı için özel sayaç
    private float internalReducerTimer = 0f;
    private bool wasReducerActiveLastFrame = false;

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

        if (fillImage != null)
        {
            ratio = Mathf.Clamp01(ratio);

            if (fillFromCenter)
            {
                fillImage.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
            }
            else
            {
                fillImage.fillAmount = ratio;
            }
        }
    }
}