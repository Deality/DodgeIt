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

    private bool running;
    private bool hudPrepared;
    private bool leaving; // butona iki kez basılınca iki hak harcanmasın
    private TextMeshProUGUI coinCounterText;

    private static readonly Color Navy = new Color32(3, 0, 92, 255);
    private static readonly Color CardColor = new Color32(67, 67, 175, 255);
    private static readonly Color Green = new Color32(138, 221, 84, 255);
    private static readonly Color Gold = new Color32(246, 198, 62, 255);
    private static readonly Color Gray = new Color32(120, 120, 140, 255);

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
        StartCoroutine(ShowResultRoutine(coins));
    }

    IEnumerator ShowResultRoutine(int coins)
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
        card.rectTransform.sizeDelta = new Vector2(780f, 720f);
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black; outline.effectDistance = new Vector2(6f, -6f);

        NewText("Title", card.rectTransform, font, "DRIFT MODE", 76f, Color.white, new Vector2(0f, 250f), new Vector2(700f, 100f));

        // Kazanılan coin
        Sprite coinSprite = GetCoinSprite();
        if (coinSprite != null)
        {
            var coin = NewImage("Coin", card.rectTransform, Color.white);
            coin.sprite = coinSprite; coin.preserveAspect = true;
            coin.rectTransform.sizeDelta = new Vector2(110f, 110f);
            coin.rectTransform.anchoredPosition = new Vector2(-170f, 95f);
        }
        NewText("Coins", card.rectTransform, font, "+" + coins.Dotted(), 96f, Gold, new Vector2(60f, 95f), new Vector2(420f, 120f));

        int left = GameModeState.DriftPlaysLeft;

        // Tekrar oyna (kalan hakla)
        var again = NewImage("PlayAgainButton", card.rectTransform, left > 0 ? Green : Gray);
        again.rectTransform.sizeDelta = new Vector2(600f, 150f);
        again.rectTransform.anchoredPosition = new Vector2(0f, -85f);
        NewText("Text", again.rectTransform, font, left > 0 ? "PLAY AGAIN" : "NO PLAYS LEFT", 54f, Navy, new Vector2(0f, 18f), new Vector2(560f, 70f));
        string leftText = left > 0 ? $"{left}/{GameModeState.DriftDailyPlays} LEFT TODAY" : "NEW PLAYS TOMORROW";
        NewText("Left", again.rectTransform, font, leftText, 30f, Navy, new Vector2(0f, -38f), new Vector2(560f, 44f));
        if (left > 0)
        {
            again.raycastTarget = true; // butona dokunulabilsin
            var b = again.gameObject.AddComponent<Button>();
            b.targetGraphic = again;
            b.onClick.AddListener(PlayAgain);
        }

        // Ana menü
        var menu = NewImage("MainMenuButton", card.rectTransform, Gold);
        menu.rectTransform.sizeDelta = new Vector2(600f, 120f);
        menu.rectTransform.anchoredPosition = new Vector2(0f, -255f);
        NewText("Text", menu.rectTransform, font, "MAIN MENU", 50f, Navy, Vector2.zero, new Vector2(560f, 80f));
        menu.raycastTarget = true; // butona dokunulabilsin
        var mb = menu.gameObject.AddComponent<Button>();
        mb.targetGraphic = menu;
        mb.onClick.AddListener(BackToMenu);

        return root.rectTransform;
    }

    void PlayAgain()
    {
        if (leaving) return;
        leaving = true;
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
