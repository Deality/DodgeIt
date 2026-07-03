using UnityEngine;
using TMPro;

public class PowerUpShopManager : MonoBehaviour
{
    [Header("UI Bağlantıları (Sahip Olunanlar)")]
    [Tooltip("Cüzdandaki parayı gösterecek yazı (Eğer bu panelde para varsa)")]
    public TextMeshProUGUI walletText;

    [Tooltip("BEYAZ YUVARLAĞIN İÇİNDEKİ YAZI (Sahip olunan Boost sayısını gösterir)")]
    public TextMeshProUGUI boostCountText;

    [Header("UI Bağlantıları (Satın Alma Paneli)")]
    [Tooltip("EKSİ (-) VE ARTI (+) BUTONLARININ ORTASINDAKİ YAZI (Kaç adet alınacağı)")]
    public TextMeshProUGUI selectedQuantityText;

    [Tooltip("SARI KUTU İÇİNDEKİ YAZI (Toplam fiyatı gösterir, örn: 200)")]
    public TextMeshProUGUI totalPriceText;

    // 🔥 YENİ EKLENDİ: Fiyat Yazısı Rengi
    [Header("Görsel Ayarlar")]
    [Tooltip("Toplam fiyat yazısının rengi (Sarı kutu içindeki metin)")]
    public Color priceTextColor = Color.black; // Varsayılan olarak Siyah yapıldı

    [Header("Fiyat ve Limit Ayarları")]
    public int pricePerBoost = 10;
    public int maxPurchaseLimit = 100;
    public int quantityStep = 1;

    [Header("Pop-Up (Opsiyonel)")]
    public GameObject insufficientFundsPanel;

    private int currentSelectedQuantity;

    void OnEnable()
    {
        currentSelectedQuantity = quantityStep;
        UpdateUI();
    }

    public void CloseShop()
    {
        gameObject.SetActive(false);
    }

    public void CloseNoMoneyPanel()
    {
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
    }

    public void IncreaseQuantity()
    {
        if (currentSelectedQuantity + quantityStep <= maxPurchaseLimit)
            currentSelectedQuantity += quantityStep;
        else
            currentSelectedQuantity = maxPurchaseLimit;

        UpdateUI();
    }

    public void DecreaseQuantity()
    {
        if (currentSelectedQuantity - quantityStep >= quantityStep)
            currentSelectedQuantity -= quantityStep;
        else
            currentSelectedQuantity = quantityStep;

        UpdateUI();
    }

    public void BuySelectedAmount()
    {
        int totalPrice = currentSelectedQuantity * pricePerBoost;
        int currentGems = PlayerPrefs.GetInt("GemsCount", 0);

        if (currentGems >= totalPrice)
        {
            currentGems -= totalPrice;
            PlayerPrefs.SetInt("GemsCount", currentGems);

            int currentBoosts = PlayerPrefs.GetInt("PlayerBoosts", 0);
            currentBoosts += currentSelectedQuantity;
            PlayerPrefs.SetInt("PlayerBoosts", currentBoosts);

            PlayerPrefs.Save();

            if (GameManager.instance != null)
            {
                GameManager.instance.currentBoostAmount = currentBoosts;
                GameManager.gems = currentGems;
            }

            // Satın alımdan sonra miktarı tekrar 1'e çek
            currentSelectedQuantity = quantityStep;
            UpdateUI();

            if (AudioManager.instance != null && AudioManager.instance.coinSound != null)
                AudioManager.instance.PlaySFX(AudioManager.instance.coinSound);

            Debug.Log($"✅ Başarılı! {currentSelectedQuantity}x Boost alındı.");
        }
        else
        {
            Debug.Log("❌ Yetersiz Bakiye!");
            if (insufficientFundsPanel != null)
                insufficientFundsPanel.SetActive(true);
        }
    }

    public void UpdateUI()
    {
        // 1. Ortadaki Miktar Yazısı (+ ve - arası)
        if (selectedQuantityText != null)
            selectedQuantityText.text = currentSelectedQuantity.ToString();
        else
            Debug.LogError("⚠️ HATA: Eksi ve Artı butonlarının ortasındaki yazı 'Selected Quantity Text' boş!");

        // 2. Sarı Toplam Fiyat Yazısı
        if (totalPriceText != null)
        {
            int totalCost = currentSelectedQuantity * pricePerBoost;
            totalPriceText.text = totalCost.ToString();

            // 🔥 YENİ EKLENDİ: Rengi kod üzerinden zorla ayarla
            totalPriceText.color = priceTextColor;
        }
        else
            Debug.LogError("⚠️ HATA: Sarı 'Total Price Text' boş!");

        // 3. Cüzdan Yazısı
        if (walletText != null)
            walletText.text = PlayerPrefs.GetInt("GemsCount", 0).ToString();

        // 4. Beyaz Yuvarlak İçindeki Sahip Olunan Boost Sayısı
        if (boostCountText != null)
            boostCountText.text = PlayerPrefs.GetInt("PlayerBoosts", 0).ToString();
        else
            Debug.LogError("⚠️ HATA: Beyaz yuvarlak içindeki 'Boost Count Text' boş!");
    }
}