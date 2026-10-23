using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Coin marketteki REMOVE ADS butonu. Fiyatı Google Play'den alır; satın alındıktan sonra "ADS REMOVED" yazar ve kapanır.
[RequireComponent(typeof(Button))]
public class RemoveAdsButton : MonoBehaviour
{
    public TMP_Text label;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    void OnEnable()
    {
        CoinStore.PricesChanged += Refresh;
        CoinStore.AdsRemovedChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        CoinStore.PricesChanged -= Refresh;
        CoinStore.AdsRemovedChanged -= Refresh;
    }

    void Refresh()
    {
        bool removed = CoinStore.AdsRemoved;
        button.interactable = !removed;
        if (label == null) return;
        string price = CoinStore.GetPrice(CoinStore.RemoveAdsId);
        label.text = removed ? "ADS REMOVED" : price != null ? "REMOVE ADS  " + price : "REMOVE ADS";
    }

    void OnClick()
    {
        if (CoinStore.AdsRemoved) return;
        if (!RewardedAd.IsAvailable) { RewardedAd.ShowNoInternetNotice(); return; }
        if (!CoinStore.IsReady) { RewardedAd.ShowNotice("STORE NOT AVAILABLE, TRY AGAIN"); return; }

        button.interactable = false;
        CoinStore.Buy(CoinStore.RemoveAdsId, _ => { if (button != null) Refresh(); });
    }
}
