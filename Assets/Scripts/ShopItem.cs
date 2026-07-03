using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItem : MonoBehaviour
{
    [Header("Ürün Ayarları (Manuel Gir)")]
    public MarketItemType itemType; // Araba mı Yol mu?
    public int itemIndex;     // Bu kaçıncı araba?
    public int price;         // Fiyatı kaç?

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

        // 🔥 METİNLER İNGİLİZCE YAPILDI
        if (priceText != null)
        {
            if (isSelected) priceText.text = "Selected ";
            else if (isPurchased) priceText.text = "Use";
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
        if (MarketManager.instance != null)
        {
            MarketManager.instance.ProcessClick(this);
        }
    }
}