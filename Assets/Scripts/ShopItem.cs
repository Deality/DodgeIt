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