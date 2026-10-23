using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

// Google Play satın alımları (Unity IAP 5): coin paketleri ve Remove Ads. Oyun açılınca mağazaya bağlanır,
// yarım kalmış satın alımları teslim eder, daha önce alınmış Remove Ads'i geri yükler.
// Butonlar CoinPackButton / RemoveAdsButton üzerinden Buy çağırır.
public static class CoinStore
{
    // Play Console > Uygulama içi ürünler'deki ürün kimlikleri ve verdikleri coin miktarı.
    // Kimlikler Play Console'dakilerle birebir aynı olmalı.
    public static readonly Dictionary<string, int> Packs = new Dictionary<string, int>
    {
        { "coins_5000", 5000 },
        { "coins_17500", 17500 },
        { "coins_32500", 32500 },
        { "coins_75000", 75000 },
    };

    // Kalıcı ürün: geçiş reklamlarını kapatır (ödüllü reklamlar isteğe bağlı olduğu için kalır)
    public const string RemoveAdsId = "remove_ads";

    private const string GemsKey = "GemsCount";
    private const string AdsRemovedKey = "IAP_AdsRemoved";
    private const string PurchaserKey = "IAP_HasPurchased"; // bir kez bile satın alan oyuncuya geçiş reklamı yok
    private const string GrantedOrdersKey = "IAP_GrantedOrders"; // aynı siparişin coini iki kez verilmesin

    public static event Action PricesChanged;
    public static event Action AdsRemovedChanged;

    private static StoreController store;
    private static bool connected;
    private static Action<bool> pendingCallback;

    public static bool IsReady => connected;
    public static bool AdsRemoved => PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;
    public static bool NoForcedAds => AdsRemoved || PlayerPrefs.GetInt(PurchaserKey, 0) == 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static async void Bootstrap()
    {
        store = UnityIAPServices.StoreController();
        store.OnProductsFetched += _ => { connected = true; PricesChanged?.Invoke(); store.FetchPurchases(); };
        store.OnProductsFetchFailed += f => Debug.LogWarning("IAP products fetch failed: " + f.FailureReason);
        store.OnPurchasePending += OnPurchasePending;
        store.OnPurchaseFailed += OnPurchaseFailed;
        store.OnStoreDisconnected += d => { connected = false; Debug.LogWarning("IAP store disconnected: " + d.message); };
        store.ProcessPendingOrdersOnPurchasesFetched(true);
        // Ödemesi sonradan tamamlanacak siparişler (ör. mağazada nakit) bitince OnPurchasePending'e düşer
        store.OnPurchaseDeferred += _ => Finish(false);
        store.OnStoreConnected += () => { };
        store.OnPurchasesFetched += RestoreRemoveAds;
        store.OnPurchasesFetchFailed += f => Debug.LogWarning("IAP purchases fetch failed: " + f.FailureReason);

        try
        {
            await store.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning("IAP connect failed: " + e.Message);
            return;
        }

        var definitions = new List<ProductDefinition>();
        foreach (var id in Packs.Keys) definitions.Add(new ProductDefinition(id, ProductType.Consumable));
        definitions.Add(new ProductDefinition(RemoveAdsId, ProductType.NonConsumable));
        store.FetchProducts(definitions);
    }

    // Mağazanın yerel para birimindeki fiyatı (ör. "₺34,99"); ürün gelmediyse null.
    public static string GetPrice(string productId)
    {
        if (!connected) return null;
        var product = store.GetProductById(productId);
        return product != null && product.availableToPurchase ? product.metadata.localizedPriceString : null;
    }

    // onDone(true): ürün teslim edildi. false: iptal edildi ya da hata oluştu.
    public static void Buy(string productId, Action<bool> onDone)
    {
        if (!connected || pendingCallback != null) { onDone?.Invoke(false); return; }
        var product = store.GetProductById(productId);
        if (product == null || !product.availableToPurchase) { onDone?.Invoke(false); return; }

        pendingCallback = onDone;
        store.PurchaseProduct(product);
    }

    static void OnPurchasePending(PendingOrder order)
    {
        string transactionId = order.Info.TransactionID;
        string granted = PlayerPrefs.GetString(GrantedOrdersKey, "");

        if (string.IsNullOrEmpty(transactionId) || !granted.Contains(transactionId))
        {
            int coins = 0;
            bool removeAds = false;
            foreach (var item in order.CartOrdered.Items())
            {
                string id = item.Product.definition.id;
                if (Packs.TryGetValue(id, out int amount)) coins += amount * item.Quantity;
                if (id == RemoveAdsId) removeAds = true;
            }

            if (coins > 0)
            {
                if (GameManager.instance != null) GameManager.instance.AddGems(coins);
                else PlayerPrefs.SetInt(GemsKey, PlayerPrefs.GetInt(GemsKey, 0) + coins);
            }
            PlayerPrefs.SetInt(PurchaserKey, 1);
            if (removeAds) SetAdsRemoved();

            if (!string.IsNullOrEmpty(transactionId)) PlayerPrefs.SetString(GrantedOrdersKey, granted + transactionId + ";");
            PlayerPrefs.Save();
        }

        // Coinler kaydedildikten sonra onayla; onaylanmayan satın alım Google tarafından iade edilir
        store.ConfirmPurchase(order);
        if (AudioManager.instance != null) AudioManager.instance.PlayPurchaseSound();
        Finish(true);
    }

    // Uygulama yeniden yüklense de Google Play'deki eski Remove Ads satın alımı geri gelir
    static void RestoreRemoveAds(Orders orders)
    {
        foreach (var order in orders.ConfirmedOrders)
            foreach (var item in order.CartOrdered.Items())
                if (item.Product.definition.id == RemoveAdsId) { SetAdsRemoved(); PlayerPrefs.Save(); return; }
    }

    static void SetAdsRemoved()
    {
        if (AdsRemoved) return;
        PlayerPrefs.SetInt(AdsRemovedKey, 1);
        AdsRemovedChanged?.Invoke();
    }

    static void OnPurchaseFailed(FailedOrder order)
    {
        if (order.FailureReason != PurchaseFailureReason.UserCancelled)
            Debug.LogWarning("IAP purchase failed: " + order.FailureReason + " " + order.Details);
        Finish(false);
    }

    static void Finish(bool success)
    {
        var callback = pendingCallback;
        pendingCallback = null;
        callback?.Invoke(success);
    }
}
