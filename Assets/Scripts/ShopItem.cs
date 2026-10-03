using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    [Header("Ürün Ayarları (Manuel Gir)")]
    public MarketItemType itemType; // Araba mı Yol mu?
    public int itemIndex;     // Bu kaçıncı araba?
    public int price;         // Fiyatı kaç?
    [Tooltip("İşaretliyse coin ile satın alınamaz; markette kilitli görünür ve yalnızca bir başarım ödülüyle açılır.")]
    public bool achievementOnly;
    [Tooltip("Kilitli başarım arabasına tıklanınca kısa süre gösterilen yazı (gizli görev arabası için örn. \"? ? ?\").")]
    public string lockedHint = "Achievement";
    [Tooltip("Atanırsa (gizli görev arabası): araba kilitliyken bu görsel siyah silüet olarak gösterilir, kilit açılınca gerçek renklerine döner.")]
    public Image silhouetteImage;
    [Tooltip("Silüetin üstündeki soru işareti. Araba kilitliyken görünür.")]
    public GameObject secretMark;

    [Header("UI Bağlantıları (İçine Sürükle)")]
    public TextMeshProUGUI priceText;
    public GameObject lockIcon;
    public Button myButton;
    public Image backgroundImage;

    [Header("Yeni Açılan Gizli Araba Vurgusu")]
    [Tooltip("Gizli araba başarımla açıldıktan sonra, ilk kez seçilene kadar butonun yanıp söneceği renk.")]
    public Color unlockedGlowColor = new Color(1f, 0.84f, 0.1f, 1f);
    public float unlockedGlowSpeed = 5f;

    private bool glowActive;
    private Graphic glowGraphic;
    private Color glowBaseColor = Color.white;

    private string UsedKey => itemType.ToString() + "_Used_" + itemIndex;

    void Start()
    {
        if (myButton != null)
        {
            myButton.onClick.RemoveAllListeners();
            myButton.onClick.AddListener(OnClicked);
        }
        else
        {
            myButton = GetComponent<Button>();
            if (myButton != null) myButton.onClick.AddListener(OnClicked);
        }

        UpdateItemUI();
    }

    public void UpdateItemUI()
    {
        string purchaseKey = itemType.ToString() + "_Purchased_" + itemIndex;
        string selectKey = "Selected_" + itemType.ToString();

        bool isPurchased = PlayerPrefs.GetInt(purchaseKey, 0) == 1 || itemIndex == 0;
        int selectedIndex = PlayerPrefs.GetInt(selectKey, 0);
        bool isSelected = (selectedIndex == itemIndex);

        // --- GÖRSEL AYARLAMALAR ---

        if (lockIcon != null) lockIcon.SetActive(!isPurchased);

        // Gizli araba: kilitliyken siyah silüet
        bool showSilhouette = achievementOnly && !isPurchased;
        if (silhouetteImage != null) silhouetteImage.color = showSilhouette ? Color.black : Color.white;
        if (secretMark != null) secretMark.SetActive(showSilhouette);

        // 🔥 METİNLER İNGİLİZCE YAPILDI
        if (priceText != null)
        {
            if (isSelected) priceText.text = "Selected ";
            else if (isPurchased) priceText.text = "Use";
            else if (achievementOnly) priceText.text = "Locked";
            else priceText.text = price.ToString();
        }

        if (backgroundImage != null)
        {
            if (isSelected) backgroundImage.color = Color.green;
            else if (isPurchased) backgroundImage.color = Color.white;
            else backgroundImage.color = Color.gray;
        }

        // Başarımla açılan gizli araba: oyuncu ilk kez seçene kadar "Use" butonu yanıp söner
        if (achievementOnly && isSelected && PlayerPrefs.GetInt(UsedKey, 0) == 0)
        {
            PlayerPrefs.SetInt(UsedKey, 1);
            PlayerPrefs.Save();
        }

        bool shouldGlow = achievementOnly && isPurchased && !isSelected && PlayerPrefs.GetInt(UsedKey, 0) == 0;
        if (shouldGlow && !glowActive)
        {
            if (glowGraphic == null)
            {
                if (backgroundImage != null) glowGraphic = backgroundImage;
                else if (myButton != null && myButton.targetGraphic != null) glowGraphic = myButton.targetGraphic;
                else glowGraphic = GetComponent<Graphic>();
            }
            if (glowGraphic != null) glowBaseColor = glowGraphic.color;
        }
        else if (!shouldGlow && glowActive)
        {
            transform.localScale = Vector3.one;
            // backgroundImage ise rengi yukarıda zaten ayarlandı
            if (glowGraphic != null && glowGraphic != backgroundImage) glowGraphic.color = glowBaseColor;
        }
        glowActive = shouldGlow && glowGraphic != null;
    }

    void Update()
    {
        if (!glowActive) return;
        // unscaledTime: menüde timeScale ne olursa olsun yanıp sönsün
        float p = (Mathf.Sin(Time.unscaledTime * unlockedGlowSpeed) + 1f) * 0.5f;
        glowGraphic.color = Color.Lerp(glowBaseColor, unlockedGlowColor, p);
        transform.localScale = Vector3.one * (1f + 0.04f * p);
    }

    void OnDisable()
    {
        if (glowActive) transform.localScale = Vector3.one;
    }

    void OnClicked()
    {
        // Başarımla açılan araba henüz kilitliyse satın alma akışına girmez; nereden açılacağını gösterir
        bool isPurchased = PlayerPrefs.GetInt(itemType.ToString() + "_Purchased_" + itemIndex, 0) == 1 || itemIndex == 0;
        if (achievementOnly && !isPurchased)
        {
            StopAllCoroutines();
            StartCoroutine(ShowAchievementHint());
            return;
        }

        if (MarketManager.instance != null)
        {
            MarketManager.instance.ProcessClick(this);
        }
    }

    // Kilitli başarım arabasına tıklanınca kısa süreliğine nereden açılacağını yazar
    System.Collections.IEnumerator ShowAchievementHint()
    {
        if (priceText != null) priceText.text = lockedHint;
        yield return new WaitForSecondsRealtime(1.5f);
        UpdateItemUI();
    }
}