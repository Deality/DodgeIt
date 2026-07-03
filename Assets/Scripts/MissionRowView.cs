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

    [Header("Renkler (Buton Durumları)")]
    public Color readyColor = Color.green;
    public Color notReadyColor = Color.gray;
    public Color claimedColor = Color.black;

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

        if (mission.isClaimed)
        {
            if (descriptionText != null) descriptionText.text = mission.description;
            if (rewardText != null) rewardText.text = $"+{mission.rewardAmount} {mission.rewardType}";
            if (progressText != null) progressText.text = $"{mission.currentValue} / {mission.targetValue}";
            canvasGroup.alpha = 0.4f;
            takeButton.interactable = false;
            takeButton.onClick.RemoveAllListeners();
            if (takeButtonText != null) takeButtonText.text = "DONE";
            return;
        }

        // Metinleri Doldur
        if (descriptionText != null) descriptionText.text = mission.description;

        // Ödülü yazdır
        if (rewardText != null) rewardText.text = $"+{mission.rewardAmount} {mission.rewardType}";

        // İlerlemeyi göster
        if (progressText != null) progressText.text = $"{mission.currentValue} / {mission.targetValue}";

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

    void OnTakeClicked()
    {
        if (isClaiming) return;
        isClaiming = true;

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