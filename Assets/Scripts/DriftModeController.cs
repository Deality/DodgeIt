using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// DRIFT MODU: trafik hiç gelmez; oyun başlar başlamaz uzun bir Coin Rush (varsayılan 20 sn) açılır.
// Süre bitince küçük bir sonuç paneli çıkar: kazanılan coin, kalan günlük hakla tekrar oynama ve ana menü.
// GameManager, mod Drift ise oyun başlarken bu bileşeni ekler. Panel taslak olarak kodla kurulur.
public class DriftModeController : MonoBehaviour
{
    public float driftDuration = 20f;
    [Tooltip("Drift başlarken ve biterken yolun akış hızı. Süre boyunca yavaşça artar.")]
    public float startSpeed = 100f;
    public float endSpeed = 150f;

    [Header("Kademeli Ödül (toplanan coin / çıkan coin)")]
    [Tooltip("Bu oranın altı BRONZE, üstü sırasıyla SILVER, GOLD, DIAMOND (0-1).")]
    public float silverThreshold = 0.40f;
    public float goldThreshold = 0.75f;
    public float diamondThreshold = 0.90f;
    [Tooltip("Kademe bonusu: toplanan coinin yüzdesi olarak eklenir (Bronze, Silver, Gold, Diamond).")]
    public float[] tierBonusPercents = { 10f, 25f, 50f, 100f };
    [Tooltip("Sonuç panelinde barın dolma süresi (saniye).")]
    public float tierBarFillDuration = 1.6f;

    private bool running;
    private bool hudPrepared;
    private bool leaving; // butona iki kez basılınca iki hak harcanmasın
    private TextMeshProUGUI coinCounterText;

    private static readonly Color Navy = new Color32(3, 0, 92, 255);
    private static readonly Color CardColor = new Color32(67, 67, 175, 255);
    private static readonly Color Green = new Color32(138, 221, 84, 255);
    private static readonly Color Gold = new Color32(246, 198, 62, 255);
    private static readonly Color Gray = new Color32(120, 120, 140, 255);
    private static readonly Color DoubleColor = new Color32(40, 205, 255, 255);

    private static readonly string[] TierNames = { "BRONZE", "SILVER", "GOLD", "DIAMOND" };
    private static readonly Color[] TierColors =
    {
        new Color32(205, 127, 50, 255),
        new Color32(200, 205, 215, 255),
        new Color32(246, 198, 62, 255),
        new Color32(110, 225, 255, 255)
    };

    public void Begin()
    {
        // Trafik ve güçlendirmeler hiç başlamasın
        if (ObstacleManager.instance != null)
        {
            ObstacleManager.instance.LockTraffic();
            ObstacleManager.instance.SetLockedSpeed(startSpeed);
        }
        TutorialManager.instance?.HideAllLiveUI();
        PrepareHud();
        running = true;

        if (DriftBoostManager.instance == null || !DriftBoostManager.instance.TryActivateDriftMode(driftDuration, OnDriftFinished))
        {
            Debug.LogWarning("[DriftMode] Drift başlatılamadı, ana menüye dönülüyor.");
            BackToMenu();
        }
    }

    void Update()
    {
        if (!running) return;

        // Yol hızı drift boyunca yavaşça artar
        var dbm = DriftBoostManager.instance;
        if (dbm != null && dbm.IsActive && ObstacleManager.instance != null)
        {
            float progress = 1f - Mathf.Clamp01(dbm.TimeRemaining / Mathf.Max(dbm.Duration, 0.01f));
            ObstacleManager.instance.SetLockedSpeed(Mathf.Lerp(startSpeed, endSpeed, progress));
        }

        if (coinCounterText != null && GameManager.instance != null)
            coinCounterText.text = GameManager.instance.sessionGems.Dotted();
    }

    // Skor, hız ve boost göstergeleri gizlenir; skor kutusunun yerine coin sayacı gelir.
    // GameManager sahne açılırken çağırır; Begin de çağırır ama ikinci kez kurmaz.
    public void PrepareHud()
    {
        if (hudPrepared) return;
        hudPrepared = true;

        var gm = GameManager.instance != null ? GameManager.instance : GetComponent<GameManager>();
        if (gm == null) return;

        if (gm.driftModeHiddenHud != null)
            foreach (var go in gm.driftModeHiddenHud)
                if (go != null) go.SetActive(false);

        if (gm.speedText != null)
        {
            var bar = gm.speedText.transform.parent != null ? gm.speedText.transform.parent.GetComponent<Image>() : null;
            if (bar != null && gm.driftModeTopBar != null) bar.sprite = gm.driftModeTopBar;
            gm.speedText.gameObject.SetActive(false);
        }
        if (gm.scoreText == null) return;
        gm.scoreText.gameObject.SetActive(false);

        // Coin sayacı: skor kutusunun olduğu yerde, coin simgesi + toplanan coin
        Canvas canvas = FindCanvas();
        var box = NewImage("DriftCoinCounter", canvas.transform, Navy);
        var brt = box.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(440f, 110f);
        brt.anchoredPosition = new Vector2(0f, -100f);
        var boxOutline = box.gameObject.AddComponent<Outline>();
        boxOutline.effectColor = Gold; boxOutline.effectDistance = new Vector2(5f, -5f);
        box.transform.SetSiblingIndex(gm.scoreText.transform.GetSiblingIndex());

        Sprite coinSprite = GetCoinSprite();
        if (coinSprite != null)
        {
            var coin = NewImage("Coin", brt, Color.white);
            coin.sprite = coinSprite; coin.preserveAspect = true;
            coin.rectTransform.sizeDelta = new Vector2(80f, 80f);
            coin.rectTransform.anchoredPosition = new Vector2(-150f, 0f);
        }
        coinCounterText = NewText("Coins", brt, gm.scoreText.font, "0", 70f, Gold, new Vector2(40f, 0f), new Vector2(300f, 90f));
    }

    void OnDriftFinished(int coins)
    {
        running = false;
        if (GameManager.instance != null) GameManager.instance.isGameActive = false;

        // Kademe: çıkan altınların ne kadarı toplandı (sayı olarak; altın değerleri rastgele olduğu için)
        var dbm = DriftBoostManager.instance;
        float ratio = dbm != null && dbm.SpawnedCount > 0 ? Mathf.Clamp01((float)dbm.CollectedCount / dbm.SpawnedCount) : 0f;
        int tier = TierOf(ratio);
        int bonus = Mathf.RoundToInt(coins * tierBonusPercents[Mathf.Min(tier, tierBonusPercents.Length - 1)] / 100f);

        // Bonus, panel animasyonu beklenmeden eklenir; oyuncu hemen çıksa da kaybolmaz
        if (bonus > 0 && GameManager.instance != null) GameManager.instance.AddGems(bonus);

        StartCoroutine(ShowResultRoutine(coins, ratio, tier, bonus));
    }

    int TierOf(float ratio)
    {
        if (ratio >= diamondThreshold) return 3;
        if (ratio >= goldThreshold) return 2;
        if (ratio >= silverThreshold) return 1;
        return 0;
    }

    IEnumerator ShowResultRoutine(int coins, float ratio, int tier, int bonus)
    {
        // Sonuç yazısı ve son altınların toplanması için kısa bekleme
        yield return new WaitForSeconds(0.6f);

        RectTransform panel = BuildResultPanel(coins);
        var card = (RectTransform)panel.Find("Card");

        // Kart yukarıdan iner (second chance paneli gibi, hızlı)
        Vector2 rest = card.anchoredPosition;
        Vector2 start = rest + new Vector2(0f, 1400f);
        var backdrop = panel.GetComponent<Image>();
        Color bd = backdrop.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.35f;
            float p = Mathf.Clamp01(t);
            card.anchoredPosition = Vector2.LerpUnclamped(start, rest, 1f - Mathf.Pow(1f - p, 3f));
            backdrop.color = new Color(bd.r, bd.g, bd.b, bd.a * p);
            yield return null;
        }
        card.anchoredPosition = rest;

        yield return TierBarRoutine(card, coins, ratio, tier, bonus);
        OfferDoubleCoins(card, coins + bonus);
    }

    // Bu turda kazanılan coin kadar daha: reklam sonuna kadar izlenirse eklenir, buton bir kez kullanılır
    void OfferDoubleCoins(RectTransform card, int earned)
    {
        var dbl = card.Find("DoubleCoinsButton");
        if (dbl == null || earned <= 0) return;
        var button = dbl.GetComponent<Button>();
        var label = dbl.Find("Text").GetComponent<TextMeshProUGUI>();
        var coinsText = card.Find("Coins").GetComponent<TextMeshProUGUI>();
        dbl.gameObject.SetActive(true);
        StartCoroutine(Punch((RectTransform)dbl, 1.15f));

        bool used = false;
        button.onClick.AddListener(() =>
        {
            if (used || leaving) return;
            used = true;
            RewardedAd.Show(this, () =>
            {
                if (GameManager.instance != null) GameManager.instance.AddGems(earned);
                coinsText.text = "+" + (earned * 2).Dotted();
                StartCoroutine(Punch(coinsText.rectTransform, 1.3f));
                if (AudioManager.instance != null) AudioManager.instance.PlayCoinRushTotal();
                label.text = "COINS DOUBLED!";
                button.interactable = false;
            }, () => used = false);
        });
    }

    // --- KADEME BARI ---
    // Bar 0'dan toplanan orana kadar dolar; çizgiler kademe sınırları. Dolgu geçtiği kademenin rengini alır,
    // bitince kazanılan kademe ve bonus yazılır, coin sayısı bonusla birlikte yükselir.
    IEnumerator TierBarRoutine(RectTransform card, int coins, float ratio, int tier, int bonus)
    {
        var fill = (RectTransform)card.Find("TierBar/Fill");
        var fillImg = fill.GetComponent<Image>();
        var percent = card.Find("TierBar/Percent").GetComponent<TextMeshProUGUI>();
        var result = card.Find("TierResult").GetComponent<TextMeshProUGUI>();
        var coinsText = card.Find("Coins").GetComponent<TextMeshProUGUI>();
        var labels = new Graphic[TierNames.Length];
        for (int i = 0; i < labels.Length; i++) labels[i] = card.Find("TierBar/Label" + i).GetComponent<Graphic>();
        var resultBadge = card.Find("TierResultBadge")?.GetComponent<Image>();

        yield return new WaitForSecondsRealtime(0.2f);

        float duration = Mathf.Max(0.3f, tierBarFillDuration * Mathf.Max(ratio, 0.25f));
        float t = 0f;
        int shownTier = -1;
        float lastTick = -1f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float p = Mathf.Clamp01(t);
            float r = ratio * (1f - (1f - p) * (1f - p)); // yavaşlayarak dolar
            fill.anchorMax = new Vector2(r, 1f);
            percent.text = Mathf.RoundToInt(r * 100f) + "%";

            int cur = TierOf(r);
            if (cur != shownTier)
            {
                shownTier = cur;
                fillImg.color = TierColors[cur];
                for (int i = 0; i < labels.Length; i++)
                    labels[i].color = LabelColor(labels[i], i, i == cur);
                StartCoroutine(Punch(labels[cur].rectTransform, 1.35f));
                if (cur > 0 && AudioManager.instance != null) AudioManager.instance.PlayMissionClaimSound();
            }
            if (p - lastTick >= 0.06f && AudioManager.instance != null)
            {
                lastTick = p;
                AudioManager.instance.PlayCoinRushCountTick();
            }
            yield return null;
        }
        fill.anchorMax = new Vector2(ratio, 1f);
        percent.text = Mathf.RoundToInt(ratio * 100f) + "%";

        // Kazanılan kademe + bonus
        result.color = TierColors[tier];
        result.text = bonus > 0 ? $"{TierNames[tier]}!  +{bonus.Dotted()} BONUS" : TierNames[tier] + "!";
        result.gameObject.SetActive(true);
        StartCoroutine(Punch(result.rectTransform, 1.3f));
        if (resultBadge != null)
        {
            resultBadge.sprite = TierBadge(tier);
            resultBadge.gameObject.SetActive(resultBadge.sprite != null);
            StartCoroutine(Punch(resultBadge.rectTransform, 1.5f));
        }
        if (AudioManager.instance != null) AudioManager.instance.PlayCoinRushTotal();

        // Coin sayısı bonusla birlikte yükselir
        if (bonus > 0)
        {
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.6f;
                coinsText.text = "+" + Mathf.RoundToInt(Mathf.Lerp(coins, coins + bonus, Mathf.Clamp01(t))).Dotted();
                yield return null;
            }
        }
    }

    // Rozet varsa: aktif kademe tam renkli, diğerleri sönük. Rozet yoksa yazı kademe rengini alır.
    static Color LabelColor(Graphic g, int tier, bool active)
    {
        if (g is Image) return active ? Color.white : new Color(0.55f, 0.55f, 0.7f, 0.6f);
        return active ? TierColors[tier] : new Color(1f, 1f, 1f, 0.35f);
    }

    static Sprite TierBadge(int tier)
    {
        var badges = GameManager.instance != null ? GameManager.instance.coinRushTierBadges : null;
        return badges != null && tier < badges.Length ? badges[tier] : null;
    }

    IEnumerator Punch(RectTransform rt, float peak)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.3f;
            float p = Mathf.Clamp01(t);
            rt.localScale = Vector3.one * Mathf.Lerp(peak, 1f, p * p);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // --- SONUÇ PANELİ (taslak) ---

    RectTransform BuildResultPanel(int coins)
    {
        Canvas canvas = FindCanvas();
        TMP_FontAsset font = GameManager.instance != null && GameManager.instance.scoreText != null ? GameManager.instance.scoreText.font : null;

        var root = NewImage("DriftResultPanel", canvas.transform, new Color(0f, 0f, 0.1f, 0.7f));
        Stretch(root.rectTransform);
        root.raycastTarget = true; // oyuna dokunulmasın
        root.transform.SetAsLastSibling();

        var card = NewImage("Card", root.rectTransform, CardColor);
        card.rectTransform.sizeDelta = new Vector2(780f, 1000f);
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black; outline.effectDistance = new Vector2(6f, -6f);

        NewText("Title", card.rectTransform, font, "DRIFT MODE", 76f, Color.white, new Vector2(0f, 410f), new Vector2(700f, 100f));

        // Kazanılan coin
        Sprite coinSprite = GetCoinSprite();
        if (coinSprite != null)
        {
            var coin = NewImage("Coin", card.rectTransform, Color.white);
            coin.sprite = coinSprite; coin.preserveAspect = true;
            coin.rectTransform.sizeDelta = new Vector2(110f, 110f);
            coin.rectTransform.anchoredPosition = new Vector2(-170f, 280f);
        }
        NewText("Coins", card.rectTransform, font, "+" + coins.Dotted(), 96f, Gold, new Vector2(60f, 280f), new Vector2(420f, 120f));

        BuildTierBar(card.rectTransform, font);

        int left = GameModeState.DriftPlaysLeft;
        bool needsAd = GameModeState.NextDriftPlayNeedsAd;

        // Tekrar oyna: ücretsiz hak bittiyse reklam izleyerek
        var again = NewImage("PlayAgainButton", card.rectTransform, left > 0 ? Green : Gray);
        again.rectTransform.sizeDelta = new Vector2(600f, 130f);
        again.rectTransform.anchoredPosition = new Vector2(0f, -290f);
        string againText = left <= 0 ? "NO PLAYS LEFT" : needsAd ? "WATCH AD" : "PLAY AGAIN";
        NewText("Text", again.rectTransform, font, againText, 50f, Navy, new Vector2(0f, 16f), new Vector2(560f, 64f));
        string leftText = left <= 0 ? "NEW PLAYS TOMORROW" : needsAd ? "+1 PLAY TODAY" : $"{left} FREE PLAY LEFT";
        NewText("Left", again.rectTransform, font, leftText, 28f, Navy, new Vector2(0f, -34f), new Vector2(560f, 40f));
        if (left > 0)
        {
            again.raycastTarget = true; // butona dokunulabilsin
            var b = again.gameObject.AddComponent<Button>();
            b.targetGraphic = again;
            b.onClick.AddListener(PlayAgain);
        }

        // Ana menü
        var menu = NewImage("MainMenuButton", card.rectTransform, Gold);
        menu.rectTransform.sizeDelta = new Vector2(600f, 100f);
        menu.rectTransform.anchoredPosition = new Vector2(0f, -430f);
        NewText("Text", menu.rectTransform, font, "MAIN MENU", 46f, Navy, Vector2.zero, new Vector2(560f, 76f));
        menu.raycastTarget = true; // butona dokunulabilsin
        var mb = menu.gameObject.AddComponent<Button>();
        mb.targetGraphic = menu;
        mb.onClick.AddListener(BackToMenu);

        // Reklam izleyerek bu turun coinlerini 2 katına çıkar (kademe barı bitince görünür)
        var dbl = NewImage("DoubleCoinsButton", card.rectTransform, DoubleColor);
        dbl.rectTransform.sizeDelta = new Vector2(600f, 100f);
        dbl.rectTransform.anchoredPosition = new Vector2(0f, -150f);
        NewText("Text", dbl.rectTransform, font, "WATCH AN AD FOR 2X COINS", 36f, Navy, Vector2.zero, new Vector2(570f, 76f));
        dbl.raycastTarget = true;
        var db = dbl.gameObject.AddComponent<Button>();
        db.targetGraphic = dbl;
        dbl.gameObject.SetActive(false);

        return root.rectTransform;
    }

    // Kademe barı: boş çerçeve, kademe sınırlarında çizgiler, altında kademe adları, üstünde yüzde
    void BuildTierBar(RectTransform card, TMP_FontAsset font)
    {
        const float width = 640f, height = 64f;
        var bar = NewImage("TierBar", card, Navy);
        bar.rectTransform.sizeDelta = new Vector2(width, height);
        bar.rectTransform.anchoredPosition = new Vector2(0f, 140f);
        var o = bar.gameObject.AddComponent<Outline>();
        o.effectColor = Color.black; o.effectDistance = new Vector2(4f, -4f);

        var fill = NewImage("Fill", bar.rectTransform, TierColors[0]);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.pivot = new Vector2(0f, 0.5f);
        fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;

        float[] bounds = { 0f, silverThreshold, goldThreshold, diamondThreshold, 1f };
        for (int i = 1; i <= 3; i++)
        {
            var line = NewImage("Divider" + i, bar.rectTransform, Color.white);
            line.rectTransform.anchorMin = new Vector2(bounds[i], 0f);
            line.rectTransform.anchorMax = new Vector2(bounds[i], 1f);
            line.rectTransform.sizeDelta = new Vector2(6f, 16f); // barın biraz dışına taşar
        }

        // Her dilimin altında kademe rozeti (rozet atanmamışsa kademe adı yazılır)
        for (int i = 0; i < TierNames.Length; i++)
        {
            float x = ((bounds[i] + bounds[i + 1]) * 0.5f - 0.5f) * width;
            Sprite badge = TierBadge(i);
            if (badge != null)
            {
                var icon = NewImage("Label" + i, bar.rectTransform, Color.white);
                icon.sprite = badge; icon.preserveAspect = true;
                icon.rectTransform.sizeDelta = new Vector2(78f, 78f);
                icon.rectTransform.anchoredPosition = new Vector2(x, -82f);
                icon.color = LabelColor(icon, i, false);
            }
            else
            {
                float segment = (bounds[i + 1] - bounds[i]) * width;
                float y = i % 2 == 0 ? -55f : -95f; // dar dilimlerde adlar üst üste binmesin
                var label = NewText("Label" + i, bar.rectTransform, font, TierNames[i], 30f, new Color(1f, 1f, 1f, 0.35f),
                    new Vector2(x, y), new Vector2(Mathf.Max(segment - 10f, 150f), 40f));
                label.fontSizeMin = 16f;
            }
        }

        NewText("Percent", bar.rectTransform, font, "0%", 40f, Color.white, new Vector2(0f, 62f), new Vector2(300f, 50f));

        var result = NewText("TierResult", card, font, "", 60f, TierColors[0], new Vector2(50f, -50f), new Vector2(600f, 90f));
        result.gameObject.SetActive(false);

        // Kazanılan kademenin büyük rozeti, sonuç yazısının solunda
        var resultBadge = NewImage("TierResultBadge", card, Color.white);
        resultBadge.preserveAspect = true;
        resultBadge.rectTransform.sizeDelta = new Vector2(110f, 110f);
        resultBadge.rectTransform.anchoredPosition = new Vector2(-300f, -50f);
        resultBadge.gameObject.SetActive(false);
    }

    void PlayAgain()
    {
        if (leaving) return;
        leaving = true;

        // Ücretsiz hak bittiyse ikinci hak reklam izlenince açılır
        if (GameModeState.NextDriftPlayNeedsAd)
        {
            RewardedAd.Show(this, RestartDrift, () => leaving = false);
            return;
        }
        RestartDrift();
    }

    void RestartDrift()
    {
        GameModeState.Current = GameMode.Drift;
        // UIManager.RestartGame drift modunda bir hak harcar (hak yoksa ana menüye döner)
        if (UIManager.instance != null) { UIManager.instance.RestartGame(); return; }

        if (!GameModeState.TryConsumeDriftPlay()) { BackToMenu(); return; }
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void BackToMenu()
    {
        if (leaving && UIManager.instance != null) return;
        leaving = true;
        GameModeState.Current = GameMode.Classic;
        if (UIManager.instance != null) UIManager.instance.LoadMainMenu();
        else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    Canvas FindCanvas()
    {
        // Skor yazısı gizlenmiş olabilir; kanvası hiyerarşiden bul
        if (GameManager.instance != null && GameManager.instance.scoreText != null)
        {
            var c = GameManager.instance.scoreText.GetComponentInParent<Canvas>(true);
            if (c != null) return c.rootCanvas;
        }
        return FindAnyObjectByType<Canvas>();
    }

    static Sprite GetCoinSprite()
    {
        var om = ObstacleManager.instance;
        if (om == null || om.coinPrefab == null) return null;
        var sr = om.coinPrefab.GetComponentInChildren<SpriteRenderer>(true);
        return sr != null ? sr.sprite : null;
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        var img = go.GetComponent<Image>();
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

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
