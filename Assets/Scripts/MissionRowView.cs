using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MissionRowView : MonoBehaviour
{
    [Header("Bağlantılar")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI rewardText; // 🔥 DÜZELTME: Hata yaratan özel sınıf yerine standart TextMeshProUGUI yapıldı.
    public TextMeshProUGUI progressText;
    public Button takeButton;
    public TextMeshProUGUI takeButtonText;
    public Image takeButtonImage;
    [Tooltip("Ödül ikonu. Sadece gizli görev satırında gerekir: görev tamamlanana kadar siyah silüet olarak gösterilir.")]
    public Image rewardIcon;
    [Tooltip("Silüetin üstündeki soru işareti. Görev gizliyken görünür, tamamlanınca kaybolur.")]
    public GameObject secretMark;

    [Header("İlerleme Çubuğu (TAKE butonunun altında)")]
    [Tooltip("Çubuğun tamamı. Gizli görev açığa çıkana kadar gizlenir.")]
    public GameObject progressBarRoot;
    [Tooltip("Dolan kısım. Sol kenara sabit, sağ kenarı ilerleme oranına göre açılır.")]
    public RectTransform progressFill;

    [Header("Gizli Görev Görünümü")]
    public string secretDescription = "? ? ?";
    public string secretReward = "?";
    [Tooltip("İpucu yazısının, \"? ? ?\" yazısına göre boyutu (yüzde).")]
    public int secretHintSizePercent = 55;
    public Color secretIconColor = Color.black;

    [Header("Renkler (Buton Durumları)")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.gray;
    public Color claimedColor = Color.black;

    [Header("Tamamlanan Görev Vurgusu")]
    [Tooltip("Görev tamamlanıp ödül alınmayı beklerken yanıp sönen çerçeve. Boşsa satırın kendi Image'ı kullanılır.")]
    public Image frameImage;
    public Color completedBlinkColor = new Color(1f, 0.84f, 0.1f, 1f);
    public float completedBlinkSpeed = 5f;

    private bool isBlinking = false;
    private Color frameBaseColor = Color.white;
    private bool frameBaseColorSaved = false;

    private Mission currentMission;
    private bool isClaiming = false; // Çift tıklama kilidi
    private float originalHeight = -1f; // Animasyon sonrası boyut kurtarma için hafıza

    void Awake()
    {
        // İlk uyanışta satırın orijinal dikey boyutunu kaydet
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            originalHeight = rect.rect.height;
        }
    }

    // MissionsManager'ın bu satırdaki görevi okuyabilmesi için gereken fonksiyon
    public Mission GetCurrentMission()
    {
        return currentMission;
    }

    public void Setup(Mission mission)
    {
        currentMission = mission;
        isClaiming = false;

        // 🔥 GÜVENLİK VE SIFIRLAMA: Animasyondan kalma değerleri tamamen sıfırla!
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            // Eğer daha önceden dikey daralmışsa, orijinal yüksekliğine geri döndür
            if (originalHeight > 10f)
            {
                layoutElement.preferredHeight = originalHeight;
            }
            else
            {
                layoutElement.preferredHeight = 120f; // Standart yedek boyut
            }
        }

        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Animasyonla sağa kayan pozisyonu tam sıfır noktasına geri getir
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = 0f;
            rectTransform.anchoredPosition = pos;
        }

        gameObject.SetActive(true);

        ApplyTexts(mission);

        // Tamamlanmış ama ödülü alınmamış görev: çerçeve yanıp söner
        SetBlinking(mission.isCompleted && !mission.isClaimed);

        if (mission.isClaimed)
        {
            canvasGroup.alpha = 0.4f;
            takeButton.interactable = false;
            takeButton.onClick.RemoveAllListeners();
            if (takeButtonText != null) takeButtonText.text = "DONE";
            return;
        }

        // Buton Durumunu Ayarla
        takeButton.onClick.RemoveAllListeners();

        // Buton renk bloğunu ayarla
        ColorBlock buttonColors = takeButton.colors;

        if (mission.isCompleted)
        {
            takeButton.interactable = true;

            buttonColors.normalColor = readyColor;
            buttonColors.highlightedColor = readyColor;
            buttonColors.pressedColor = readyColor;
            buttonColors.selectedColor = readyColor;

            if (takeButtonText != null) takeButtonText.text = "TAKE";

            takeButton.onClick.AddListener(OnTakeClicked);
        }
        else
        {
            takeButton.interactable = false;
            buttonColors.disabledColor = notReadyColor;
            if (takeButtonText != null) takeButtonText.text = "TAKE";
        }

        takeButton.colors = buttonColors;
    }

    // Açıklama / ödül / ilerleme yazıları. Gizli görev tamamlanana kadar "? ? ?" ve siyah silüet gösterir;
    // tamamlanınca (TAKE'e hazır ya da alınmış) gerçek açıklama ve ödül ortaya çıkar.
    void ApplyTexts(Mission mission)
    {
        bool hidden = mission.isSecret && !mission.isCompleted && !mission.isClaimed;

        if (descriptionText != null)
        {
            if (!hidden) descriptionText.text = mission.description;
            else if (string.IsNullOrEmpty(mission.secretHint)) descriptionText.text = secretDescription;
            // "? ? ?" ve altında, göreve özel küçük bir ipucu
            else descriptionText.text = $"{secretDescription}\n<size={secretHintSizePercent}%>{mission.secretHint}</size>";
        }

        if (rewardText != null)
        {
            if (hidden) rewardText.text = secretReward;
            // Araba ödülünde ikon (arabanın kendisi) kutuyu dolduruyorsa ayrıca yazı gösterme
            else if (mission.rewardType == RewardType.Car) rewardText.text = rewardIcon != null ? "" : "CAR";
            else rewardText.text = $"+{mission.rewardAmount} {mission.rewardType}";
        }

        int shownValue = Mathf.Clamp(mission.currentValue, 0, Mathf.Max(mission.targetValue, 0));
        if (mission.isCompleted || mission.isClaimed) shownValue = mission.targetValue;
        if (progressText != null) progressText.text = $"{shownValue} / {mission.targetValue}";

        if (progressBarRoot != null) progressBarRoot.SetActive(!hidden); // gizli görevin hedefi belli olmasın
        if (progressFill != null)
        {
            float ratio = mission.targetValue > 0 ? Mathf.Clamp01((float)shownValue / mission.targetValue) : 0f;
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(ratio, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
        }

        if (rewardIcon != null) rewardIcon.color = hidden ? secretIconColor : Color.white;
        if (secretMark != null) secretMark.SetActive(hidden);
    }

    void SetBlinking(bool blink)
    {
        if (frameImage == null) frameImage = GetComponent<Image>();
        if (frameImage == null) { isBlinking = false; return; }

        if (!frameBaseColorSaved)
        {
            frameBaseColor = frameImage.color;
            frameBaseColorSaved = true;
        }

        isBlinking = blink;
        if (!blink) frameImage.color = frameBaseColor;
    }

    void Update()
    {
        if (!isBlinking) return;
        // unscaledTime: menüde timeScale ne olursa olsun yanıp sönsün
        float p = (Mathf.Sin(Time.unscaledTime * completedBlinkSpeed) + 1f) * 0.5f;
        frameImage.color = Color.Lerp(frameBaseColor, completedBlinkColor, p);
    }

    void OnTakeClicked()
    {
        if (isClaiming) return;
        isClaiming = true;
        SetBlinking(false);
        if (AudioManager.instance != null) AudioManager.instance.PlayMissionClaimSound();

        // Tıklamayı engelle ve animasyonu başlat!
        if (takeButton != null) takeButton.interactable = false;
        StartCoroutine(ClaimAnimationRoutine());
    }

    // Görevi Alırken Çalışan Özel Kayma ve Dikey Daralma Animasyonu
    IEnumerator ClaimAnimationRoutine()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();

        RectTransform rectTransform = GetComponent<RectTransform>();

        // Orijinal yükseklik değerini kaydet
        float startHeight = rectTransform.rect.height;
        layoutElement.preferredHeight = startHeight;

        Vector2 startPos = rectTransform.anchoredPosition;
        float slideDistance = rectTransform.rect.width > 0 ? rectTransform.rect.width + 150f : 800f;
        Vector2 targetPos = startPos + new Vector2(slideDistance, 0f);

        // --- 1. AŞAMA: Sağa Kayma ve Solma (Slide & Fade Out) ---
        float slideDuration = 0.35f;
        float timer = 0f;
        while (timer < slideDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / slideDuration;
            float easeT = t * t * t;

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, easeT);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        canvasGroup.alpha = 0f;

        // --- 2. AŞAMA: Yükseklik Daralması (Height Collapse) ---
        float collapseDuration = 0.25f;
        timer = 0f;
        while (timer < collapseDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / collapseDuration;
            float easeT = t * (2f - t);

            layoutElement.preferredHeight = Mathf.Lerp(startHeight, 0f, easeT);

            yield return null;
        }

        layoutElement.preferredHeight = 0f;

        // --- 3. AŞAMA: Ödülü Ver ve Listeyi Tazeleyerek Gizle ---
        if (MissionsManager.Instance != null)
        {
            MissionsManager.Instance.ClaimReward(currentMission);
        }
    }
}