using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Coin marketteki bir paketin BUY butonu. Fiyatı Google Play'den alıp yazar, basılınca CoinStore ile satın alır.
[RequireComponent(typeof(Button))]
public class CoinPackButton : MonoBehaviour
{
    [Tooltip("CoinStore.Packs içindeki ürün kimliği (ör. coins_5000)")]
    public string productId;
    public TMP_Text priceText;

    private Button button;
    private string fallbackPrice; // mağaza bağlanana kadar sahnedeki yazı kalır

    void Awake()
    {
        button = GetComponent<Button>();
        if (priceText != null) fallbackPrice = priceText.text;
        button.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        CoinStore.PricesChanged += Refresh;
        Refresh();
    }

    void OnDisable() => CoinStore.PricesChanged -= Refresh;

    void Refresh()
    {
        if (priceText == null) return;
        priceText.text = CoinStore.GetPrice(productId) ?? fallbackPrice;
    }

    void OnClick()
    {
        if (!RewardedAd.IsAvailable) { RewardedAd.ShowNoInternetNotice(); return; }
        if (!CoinStore.IsReady) { RewardedAd.ShowNotice("STORE NOT AVAILABLE, TRY AGAIN"); return; }

        button.interactable = false;
        CoinStore.Buy(productId, _ => { if (button != null) button.interactable = true; });
    }
}
