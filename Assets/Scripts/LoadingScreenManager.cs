using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("Sahne Yükleme Ayarları")]
    [Tooltip("Oyun ilk açıldığında yüklenilecek ana menü sahnesinin tam adı.")]
    public string targetSceneName = "MainMenu";

    [Tooltip("True ise yüklemeyi sahte olarak simüle eder (Test etmek için çok kullanışlıdır).")]
    public bool useSimulatedLoading = true;

    [Tooltip("Simüle yükleme hızı çarpanı (Yüksek değer daha hızlı yükler).")]
    public float simulatedSpeed = 0.5f;

    [Header("Yükleme Barı UI")]
    [Tooltip("Görseldeki yeşil yükleme barı (Kod bu görseli otomatik olarak 'Filled' tipine dönüştürecektir).")]
    public Image loadingFillImage;

    [Tooltip("Yükleme barının dış çerçevesi (Border/Frame) görseli.")]
    public Image loadingBorderImage;

    [Tooltip("Yükleme barı ve çerçevesini barındıran grubun CanvasGroup bileşeni (Birlikte yumuşakça sönmeleri için).")]
    public CanvasGroup loadingBarCanvasGroup;

    [Tooltip("İlerleme yüzdesini yazdırabileceğiniz isteğe bağlı metin alanı.")]
    public TextMeshProUGUI progressText;

    [Tooltip("Yükleme adımlarını (İnternet kontrolü vb.) oyuncuya Türkçe bildiren durum yazısı.")]
    public TextMeshProUGUI statusText;

    [Header("Sahne Geçiş Efekti (Siyah Ekran)")]
    [Tooltip("Yükleme bittikten sonra ekranı kaplayacak olan siyah geçiş görseli (Fade Overlay).")]
    public Image fadeOverlay;

    [Tooltip("Siyah ekranın tamamen belirme/kararma süresi (Saniye).")]
    public float fadeToBlackDuration = 0.5f;

    [Header("Araba Gösterim (Showcase) Ayarları")]
    [Tooltip("Sırayla gösterilecek arabaların 2D Sprite (Görsel) listesi.")]
    public Sprite[] carSprites;

    [Tooltip("Arabaların üzerinde gösterileceği UI Image bileşeni.")]
    public Image carDisplayImage;

    [Tooltip("Otomatik ekran boyut hesaplaması başarısız olursa kullanılacak yedek dikey mesafe değeri.")]
    public float slideDistance = 1000f;

    [Header("Rastgele Trafik Ayarları")]
    [Tooltip("Arabaların dikey şerit koordinatları (X ekseni). Oyundaki gibi 3 şerit için değerleri Inspector'dan ayarlayabilirsiniz. (Örn: -150, 0, 150)")]
    public float[] laneXPositions = new float[] { -150f, 0f, 150f };

    [Tooltip("Araba geçiş hızı için EN DÜŞÜK limit (Yüksek değer daha hızlı fırlatır).")]
    public float minSlideSpeed = 1.5f;

    [Tooltip("Araba geçiş hızı için EN YÜKSEK limit.")]
    public float maxSlideSpeed = 3.5f;

    [Tooltip("İki araba geçişi arasındaki EN AZ bekleme süresi (Saniye).")]
    public float minSpawnDelay = 0.2f;

    [Tooltip("İki araba geçişi arasındaki EN ÇOK bekleme süresi (Saniye).")]
    public float maxSpawnDelay = 1.0f;

    private RectTransform carRectTransform;
    private int currentCarIndex = 0;
    private float loadingProgress = 0f;

    void Start()
    {
        // 🔥 FPS VE PERFORMANS OPTİMİZASYONU (Sınırlamaları Kaldırma)
        // 1. Mobil cihazlarda varsayılan 30 FPS sınırını kaldırıyoruz.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120; // 90Hz/120Hz ekranlarda oyun yağ gibi akacaktır.

        // 2. Sahne yüklenirken CPU'nun kilitlenmesini engelliyoruz.
        // Bu ayar yükleme işlemini arka planda daha düşük öncelikli yaparak UI animasyonunun (arabaların) pürüzsüz akmasını sağlar.
        Application.backgroundLoadingPriority = ThreadPriority.Low;

        // Gerekli kontroller
        if (carDisplayImage != null)
        {
            carRectTransform = carDisplayImage.GetComponent<RectTransform>();
        }

        // Kurşun Geçirmez Dolgu Sistemi
        if (loadingFillImage != null)
        {
            loadingFillImage.type = Image.Type.Filled;
            loadingFillImage.fillMethod = Image.FillMethod.Horizontal;
            loadingFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            loadingFillImage.fillAmount = 0f; // Sıfırdan başlat
        }

        if (loadingBarCanvasGroup != null)
        {
            loadingBarCanvasGroup.alpha = 1f; // Başlangıçta görünür yap
        }

        // Siyah geçiş ekranını sıfırla
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
        }

        // Animasyonları ve sahne yükleme işlemlerini başlat
        if (carSprites != null && carSprites.Length > 0 && carDisplayImage != null)
        {
            currentCarIndex = Random.Range(0, carSprites.Length);
            StartCoroutine(CarShowcaseRoutine());
        }

        StartCoroutine(LoadSceneRoutine());
    }

    // ARABA GEÇİŞ ANİMASYONU (Aşağıdan Yukarıya Doğru Akış)
    IEnumerator CarShowcaseRoutine()
    {
        while (true)
        {
            carDisplayImage.sprite = carSprites[currentCarIndex];

            float chosenX = 0f;
            if (laneXPositions != null && laneXPositions.Length > 0)
            {
                chosenX = laneXPositions[Random.Range(0, laneXPositions.Length)];
            }

            // Dinamik Sınır Hesaplama
            float dynamicDistance = slideDistance;
            if (carRectTransform != null)
            {
                RectTransform parentRect = carRectTransform.parent as RectTransform;
                if (parentRect != null && parentRect.rect.height > 100f)
                {
                    dynamicDistance = (parentRect.rect.height * 0.5f) + 250f;
                }
                else
                {
                    dynamicDistance = (Screen.height * 0.5f) + 250f;
                }
            }

            Vector2 startPos = new Vector2(chosenX, -dynamicDistance);
            Vector2 targetPos = new Vector2(chosenX, dynamicDistance);
            carRectTransform.anchoredPosition = startPos;

            Color c = carDisplayImage.color;
            c.a = 1f;
            carDisplayImage.color = c;

            float speed = Random.Range(minSlideSpeed, maxSlideSpeed);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * speed;
                carRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            carRectTransform.anchoredPosition = targetPos;

            if (carSprites.Length > 1)
            {
                int nextCarIndex = currentCarIndex;
                while (nextCarIndex == currentCarIndex)
                {
                    nextCarIndex = Random.Range(0, carSprites.Length);
                }
                currentCarIndex = nextCarIndex;
            }
            else
            {
                currentCarIndex = 0;
            }

            float spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSecondsRealtime(spawnDelay);
        }
    }

    // MOBİL UYUMLU GERÇEKÇİ ÇOK AŞAMALI YÜKLEME AKIŞI
    IEnumerator LoadSceneRoutine()
    {
        float progress = 0f;

        // --- 1. AŞAMA: Dosyalar Hazırlanıyor ---
        UpdateStatus("Oyun dosyaları yükleniyor...");
        float stage1Speed = simulatedSpeed * 2.5f;
        while (progress < 0.35f)
        {
            progress += Time.unscaledDeltaTime * stage1Speed;
            UpdateLoadingUI(Mathf.Clamp(progress, 0f, 0.35f));
            yield return null;
        }
        progress = 0.35f;

        yield return new WaitForSecondsRealtime(0.25f);

        // --- 2. AŞAMA: Sunucu Senkronizasyonu ---
        UpdateStatus("Sunucu verileri senkronize ediliyor...");
        float stage2Speed = simulatedSpeed * 0.45f;
        while (progress < 0.75f)
        {
            progress += Time.unscaledDeltaTime * stage2Speed;
            UpdateLoadingUI(Mathf.Clamp(progress, 0.35f, 0.75f));
            yield return null;
        }
        progress = 0.75f;

        // --- 3. AŞAMA: %75 KİLİDİ VE MOBİL İNTERNET/BAĞLANTI KONTROLÜ ---
        UpdateStatus("İnternet bağlantısı kontrol ediliyor...");

        float checkTimer = 0f;
        float minimumCheckTime = 1.8f;
        Color originalBorderColor = loadingBorderImage != null ? loadingBorderImage.color : Color.white;

        while (checkTimer < minimumCheckTime || Application.internetReachability == NetworkReachability.NotReachable)
        {
            checkTimer += Time.unscaledDeltaTime;

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                UpdateStatus("İnternet bağlantısı bekleniyor...");
                if (loadingBorderImage != null)
                {
                    float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
                    loadingBorderImage.color = Color.Lerp(originalBorderColor, new Color(1f, 0.3f, 0.3f, 1f), pulse);
                }
            }
            else
            {
                UpdateStatus("Bağlantı kuruldu, profil doğrulanıyor...");
                if (loadingBorderImage != null)
                {
                    loadingBorderImage.color = Color.Lerp(loadingBorderImage.color, originalBorderColor, Time.unscaledDeltaTime * 5f);
                }
            }

            yield return null;
        }

        if (loadingBorderImage != null)
        {
            loadingBorderImage.color = originalBorderColor;
        }

        // --- 4. AŞAMA: Profil Yükleme ve Giriş Yapma ---
        UpdateStatus("Giriş yapılıyor...");
        float stage4Speed = simulatedSpeed * 1.8f;
        while (progress < 1f)
        {
            progress += Time.unscaledDeltaTime * stage4Speed;
            UpdateLoadingUI(Mathf.Clamp(progress, 0.75f, 1f));
            yield return null;
        }
        progress = 1f;
        UpdateLoadingUI(1f);

        yield return new WaitForSecondsRealtime(0.5f);

        // --- 5. AŞAMA: Siyah Ekran Kararma Geçişi ---
        UpdateStatus("Giriş doğrulanıyor...");
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float fadeTimer = 0f;
            Color startColor = new Color(0f, 0f, 0f, 0f);
            Color targetColor = new Color(0f, 0f, 0f, 1f);

            while (fadeTimer < fadeToBlackDuration)
            {
                fadeTimer += Time.unscaledDeltaTime;
                fadeOverlay.color = Color.Lerp(startColor, targetColor, fadeTimer / fadeToBlackDuration);
                yield return null;
            }
            fadeOverlay.color = targetColor;
        }
        else
        {
            if (loadingBarCanvasGroup != null)
            {
                float fadeTimer = 0f;
                float fadeDuration = 0.5f;
                while (fadeTimer < fadeDuration)
                {
                    fadeTimer += Time.unscaledDeltaTime;
                    loadingBarCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);
                    yield return null;
                }
                loadingBarCanvasGroup.alpha = 0f;
            }
        }

        // --- SAHNEYE GEÇİŞ YAPMA ---
        if (useSimulatedLoading)
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                yield return null;
            }

            operation.allowSceneActivation = true;
        }
    }

    void UpdateLoadingUI(float progress)
    {
        if (loadingFillImage != null)
        {
            loadingFillImage.fillAmount = progress;
        }

        if (progressText != null)
        {
            progressText.text = "%" + Mathf.RoundToInt(progress * 100f).ToString();
        }
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}