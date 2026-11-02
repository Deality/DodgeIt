using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ana menüdeki yuvarlak "reklamları kaldır" butonu. Basınca kampanya penceresi açılır: Remove Ads satın alınırsa
// CoinStore.RemoveAdsBonusCoins kadar coin de verilir. Satın alındıktan sonra buton gizlenir.
[RequireComponent(typeof(Button))]
public class RemoveAdsButton : MonoBehaviour
{
    [Tooltip("Kampanya penceresindeki coin simgesi.")]
    public Sprite coinSprite;
    public TMP_FontAsset font;

    private static readonly Color Navy = new Color(0f, 0f, 0.31f);
    private static readonly Color CardColor = new Color(0.11f, 0.13f, 0.36f, 1f);
    private static readonly Color Gold = new Color(0.98f, 0.78f, 0.15f);
    private static readonly Color Green = new Color(0.55f, 0.88f, 0.35f);
    private static readonly Color CloseColor = new Color(0.86f, 0.15f, 0.25f);

    private Button button;
    private GameObject offerPanel;
    private Button buyButton;
    private TMP_Text buyLabel;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OpenOffer);
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
        CloseOffer();
    }

    void Refresh()
    {
        if (CoinStore.AdsRemoved) { gameObject.SetActive(false); return; }
        if (buyLabel == null) return;
        string price = CoinStore.GetPrice(CoinStore.RemoveAdsId);
        buyLabel.text = price != null ? "BUY  " + price : "BUY";
    }

    void Buy()
    {
        if (CoinStore.AdsRemoved) return;
        if (!RewardedAd.IsAvailable) { RewardedAd.ShowNoInternetNotice(); return; }
        if (!CoinStore.IsReady) { RewardedAd.ShowNotice("STORE NOT AVAILABLE, TRY AGAIN"); return; }

        buyButton.interactable = false;
        CoinStore.Buy(CoinStore.RemoveAdsId, success =>
        {
            if (success) CloseOffer();
            else if (buyButton != null) buyButton.interactable = true;
        });
    }

    void CloseOffer()
    {
        if (offerPanel != null) Destroy(offerPanel);
        offerPanel = null;
        buyButton = null;
        buyLabel = null;
    }

    void OpenOffer()
    {
        if (offerPanel != null) return;
        Canvas canvas = GetComponentInParent<Canvas>();

        var dim = NewImage("NoAdsOfferPanel", canvas.transform, new Color(0f, 0f, 0.1f, 0.8f));
        dim.rectTransform.anchorMin = Vector2.zero;
        dim.rectTransform.anchorMax = Vector2.one;
        dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        dim.raycastTarget = true;
        dim.gameObject.AddComponent<Button>().onClick.AddListener(CloseOffer); // boşluğa dokununca kapanır
        offerPanel = dim.gameObject;
        offerPanel.transform.SetAsLastSibling();

        var card = NewImage("Card", dim.rectTransform, CardColor);
        card.rectTransform.sizeDelta = new Vector2(820f, 820f);
        card.raycastTarget = true; // karta dokununca pencere kapanmasın
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Green; outline.effectDistance = new Vector2(8f, -8f);
        RectTransform c = card.rectTransform;

        NewText("Title", c, font, "SPECIAL OFFER", 72f, Gold, new Vector2(0f, 320f), new Vector2(660f, 100f));

        var icon = NewImage("Icon", c, Color.white);
        icon.sprite = GetComponent<Image>().sprite; icon.preserveAspect = true;
        icon.rectTransform.sizeDelta = new Vector2(170f, 170f);
        icon.rectTransform.anchoredPosition = new Vector2(0f, 165f);

        NewText("Subtitle", c, font, "REMOVE ADS AND CLAIM", 46f, Color.white, new Vector2(0f, 30f), new Vector2(740f, 70f));

        // Bonus coin: sayı ve simge birlikte ortalı
        const float coinSize = 104f, gap = 16f, rowY = -70f;
        var amount = NewText("Coins", c, font, "+" + CoinStore.RemoveAdsBonusCoins.Dotted(), 92f, Gold, new Vector2(0f, rowY), new Vector2(600f, 120f));
        amount.enableAutoSizing = false;
        amount.textWrappingMode = TextWrappingModes.NoWrap;
        amount.gameObject.AddComponent<TextShine>();
        float width = amount.GetPreferredValues(amount.text).x;
        float total = coinSprite != null ? width + gap + coinSize : width;
        amount.rectTransform.sizeDelta = new Vector2(width + 10f, 120f);
        amount.rectTransform.anchoredPosition = new Vector2(-total * 0.5f + width * 0.5f, rowY);
        if (coinSprite != null)
        {
            var coin = NewImage("Coin", c, Color.white);
            coin.sprite = coinSprite; coin.preserveAspect = true;
            coin.rectTransform.sizeDelta = new Vector2(coinSize, coinSize);
            coin.rectTransform.anchoredPosition = new Vector2(total * 0.5f - coinSize * 0.5f, rowY);
        }

        NewText("Note", c, font, "OPTIONAL REWARD ADS STAY AVAILABLE", 26f, new Color(1f, 1f, 1f, 0.65f), new Vector2(0f, -165f), new Vector2(720f, 44f));

        var buy = NewImage("BuyButton", c, Green);
        buy.rectTransform.sizeDelta = new Vector2(580f, 130f);
        buy.rectTransform.anchoredPosition = new Vector2(0f, -285f);
        buy.raycastTarget = true;
        buyButton = buy.gameObject.AddComponent<Button>();
        buyButton.targetGraphic = buy;
        buyButton.onClick.AddListener(Buy);
        buyLabel = NewText("Text", buy.rectTransform, font, "BUY", 56f, Navy, Vector2.zero, new Vector2(540f, 100f));
        Refresh();

        var close = NewImage("CloseButton", c, CloseColor);
        close.rectTransform.sizeDelta = new Vector2(84f, 84f);
        close.rectTransform.anchoredPosition = new Vector2(375f, 375f);
        close.raycastTarget = true;
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = close;
        closeButton.onClick.AddListener(CloseOffer);
        NewText("Text", close.rectTransform, font, "X", 50f, Color.white, Vector2.zero, new Vector2(70f, 70f));

        StartCoroutine(SlideIn(c));
    }

    // Kart yukarıdan iner (second chance penceresi gibi)
    IEnumerator SlideIn(RectTransform card)
    {
        Vector2 start = new Vector2(0f, 1400f);
        float t = 0f;
        while (t < 1f && card != null)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            float p = Mathf.Clamp01(t);
            card.anchoredPosition = Vector2.LerpUnclamped(start, Vector2.zero, 1f - Mathf.Pow(1f - p, 3f));
            yield return null;
        }
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        var img = go.GetComponent<Image>();
        img.rectTransform.SetParent(parent, false);
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font, string text, float size, Color color, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer;
        var t = go.GetComponent<TextMeshProUGUI>();
        t.rectTransform.SetParent(parent, false);
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta = box;
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.enableAutoSizing = true; t.fontSizeMin = 12f; t.fontSizeMax = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }
}
