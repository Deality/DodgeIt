using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Ürün Tipleri (Global Enum)
public enum MarketItemType
{
    Car,
    Road
}

[System.Serializable]
public class MarketItemData
{
    public string itemName;
    public int price;
    public Sprite icon;

    [Header("Ürün Türüne Göre Doldur")]
    public GameObject carPrefab;
    public Sprite roadSprite;
}

public class MarketManager : MonoBehaviour
{
    public static MarketManager instance;

    [Header("Market Paneli (Buraya Paneli Sürükle)")]
    public GameObject marketPanel;

    [Header("Diğer UI Referansları")]
    public GameObject marketItemPrefab;
    public TextMeshProUGUI totalGemText;

    // 🔥 BURASI ÇOK ÖNEMLİ: Bu kutucuklara panelleri sürüklemezseniz açılmaz!
    [Header("Pop-Up Panelleri")]
    public GameObject confirmationPanel;
    public GameObject insufficientFundsPanel;
    public TextMeshProUGUI confirmationMessageText;

    [Header("Liste Alanları (Scroll Content)")]
    public Transform carContentParent;
    public Transform roadContentParent;

    [Header("Market Verileri")]
    public List<MarketItemData> carItems;
    public List<MarketItemData> roadItems;

    // Geçici Hafıza
    private int pendingIndex;
    private MarketItemType pendingType;
    private int pendingPrice;
    private ShopItem pendingShopItem;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUI();

        // Manuel butonları güncelle
        RefreshAllButtons();

        // Başlangıçta panelleri kapat
        if (marketPanel != null) marketPanel.SetActive(false);
        ClosePopups();
    }

    // --- MARKET AÇ / KAPA ---
    public void OpenMarket()
    {
        if (marketPanel != null)
        {
            marketPanel.SetActive(true);

            // Eğer UIManager paneli küçülttüyse (Scale 0), biz tekrar büyütelim
            marketPanel.transform.localScale = Vector3.one;

            UpdateUI();
            RefreshAllButtons();
        }
    }

    public void CloseMarket()
    {
        if (marketPanel != null) marketPanel.SetActive(false);
        ClosePopups();
    }

    public void ClosePopups()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (insufficientFundsPanel != null) insufficientFundsPanel.SetActive(false);
        pendingShopItem = null;
    }

    public void UpdateUI()
    {
        if (totalGemText != null)
        {
            int gems = PlayerPrefs.GetInt("GemsCount", 0);
            totalGemText.text = gems.ToString();
        }
    }

    // --- MANUEL BUTONLAR (ShopItem) İÇİN TIKLAMA YÖNETİMİ ---

    // ShopItem scripti bu fonksiyonu çağırır
    public void ProcessClick(ShopItem item)
    {
        Debug.Log("Butona Tıklandı: " + item.name); // Tıklamayı test etmek için

        string purchaseKey = item.itemType.ToString() + "_Purchased_" + item.itemIndex;

        // 0. indeks her zaman satın alınmıştır
        bool isPurchased = PlayerPrefs.GetInt(purchaseKey, 0) == 1 || item.itemIndex == 0;

        if (isPurchased)
        {
            // Zaten alınmış -> SEÇ
            SelectItem(item.itemIndex, item.itemType);
        }
        else
        {
            // Alınmamış -> Para Kontrolü
            int currentGems = PlayerPrefs.GetInt("GemsCount", 0);

            if (currentGems >= item.price)
            {
                OpenConfirmationForShopItem(item);
            }
            else
            {
                OpenInsufficientFundsPanel();
            }
        }
    }

    void OpenConfirmationForShopItem(ShopItem item)
    {
        pendingShopItem = item;

        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
            // 🔥 Panel açıldığında boyutunu düzelt (Görünmez kalmasını önler)
            confirmationPanel.transform.localScale = Vector3.one;

            if (confirmationMessageText != null)
            {
                confirmationMessageText.text = $"Bu ürünü {item.price} altına almak istiyor musun?";
            }
        }
        else
        {
            Debug.LogError("Confirmation Panel atanmamış! Direkt satın alınıyor.");
            ConfirmPurchase(); // Panel yoksa direkt al
        }
    }

    void OpenInsufficientFundsPanel()
    {
        if (insufficientFundsPanel != null)
        {
            insufficientFundsPanel.SetActive(true);
            // 🔥 Panel açıldığında boyutunu düzelt
            insufficientFundsPanel.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.Log("Para Yetersiz (Panel atanmamış)!");
        }
    }

    public void RefreshAllButtons()
    {
        // Sahnedeki tüm manuel butonları bul ve güncelle
        ShopItem[] allItems = FindObjectsOfType<ShopItem>();

        foreach (ShopItem btn in allItems)
        {
            btn.UpdateItemUI();
        }
    }

    // --- SATIN ALMA / SEÇME ---

    public void ConfirmPurchase()
    {
        if (pendingShopItem != null)
        {
            BuyShopItem(pendingShopItem);
        }
        else
        {
            // Kodla üretilenler için (Eğer kullanılırsa)
            BuyItem(pendingIndex, pendingPrice, pendingType);
        }

        ClosePopups();
    }

    public void CancelPurchase()
    {
        ClosePopups();
    }

    void BuyShopItem(ShopItem item)
    {
        int currentGems = PlayerPrefs.GetInt("GemsCount", 0);
        currentGems -= item.price;
        PlayerPrefs.SetInt("GemsCount", currentGems);

        string purchaseKey = item.itemType.ToString() + "_Purchased_" + item.itemIndex;
        PlayerPrefs.SetInt(purchaseKey, 1);
        PlayerPrefs.Save();

        // 🔥 GÖREV ENTEGRASYONU: Marketten araba veya yol satın alındığında görevi tetikle!
        if (MissionsManager.Instance != null)
        {
            if (item.itemType == MarketItemType.Car)
                MissionsManager.Instance.AddProgress(MissionType.BuyCar, 1);
            else
                MissionsManager.Instance.AddProgress(MissionType.BuyRoad, 1);
        }

        UpdateUI();
        RefreshAllButtons();
        SelectItem(item.itemIndex, item.itemType);

        Debug.Log("Satın Alma Başarılı!");
    }

    void BuyItem(int index, int price, MarketItemType type)
    {
        int currentGems = PlayerPrefs.GetInt("GemsCount", 0);
        currentGems -= price;
        PlayerPrefs.SetInt("GemsCount", currentGems);

        string purchaseKey = type.ToString() + "_Purchased_" + index;
        PlayerPrefs.SetInt(purchaseKey, 1);
        PlayerPrefs.Save();

        // 🔥 GÖREV ENTEGRASYONU: Manuel alımlarda görevi tetikle!
        if (MissionsManager.Instance != null)
        {
            if (type == MarketItemType.Car)
                MissionsManager.Instance.AddProgress(MissionType.BuyCar, 1);
            else
                MissionsManager.Instance.AddProgress(MissionType.BuyRoad, 1);
        }

        UpdateUI();
        GenerateAllItems();
        SelectItem(index, type);
    }

    void SelectItem(int index, MarketItemType type)
    {
        string selectedKey = "Selected_" + type.ToString();
        PlayerPrefs.SetInt(selectedKey, index);
        PlayerPrefs.Save();

        RefreshAllButtons();
        GenerateAllItems();

        Debug.Log(type.ToString() + " Seçildi: " + index);
    }

    // Dinamik üretilen listeler için kullanılan GenerateAllItems gövdesi
    public void GenerateAllItems()
    {
        // Manuel buton (ShopItem) tasarımı kullandığınız için şu an içi boş kalabilir, 
        // ama hata vermemesi için SelectItem içinden çağrılıyor.
    }
}