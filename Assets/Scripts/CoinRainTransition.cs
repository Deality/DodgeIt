using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Coin Rush moduna geçiş: coinler ekranın üstünden yağıp alttan yukarı doğru yığılır ve ekranı tamamen kaplar.
// Ekran kaplanınca hareket durur ve sahne coinlerin arkasında yüklenir (yüklemenin takılması bu yüzden görünmez).
// Yeni sahnede coinler aşağı dökülüp ekrandan çıkarken oyun açılır.
// Kendi kanvasında çalışır ve sahne değişiminde yok olmaz (iş bitince kendini siler).
public class CoinRainTransition : MonoBehaviour
{
    public static bool IsRunning => instance != null;
    private static CoinRainTransition instance;

    private const float RefWidth = 1080f, RefHeight = 1920f;
    private const float FillDuration = 0.75f;   // alt sıradan üst sıraya coinlerin yağmaya başlama aralığı
    private const float RevealDuration = 0.45f; // açılışta alt sıradan üst sıraya coinlerin dökülmeye başlama aralığı
    private const float MaxPhaseTime = 4f;      // güvenlik: bir aşama bu süreyi geçerse yine biter
    private const float MaxStep = 1f / 30f;     // takılan karede coinler ileri sıçramasın
    private const float Gravity = 6000f;        // coinler yavaş başlar, düştükçe hızlanır
    private const float SpacingX = 150f;        // yan yana coin aralığı (coin çapından küçük: arada boşluk kalmaz)
    private const float SpacingY = 128f;        // sıra aralığı (sıralar yarım aralık kaydırılmış, petek düzeni)
    private static readonly Color CurtainColor = new Color32(3, 0, 92, 255);

    private class Coin
    {
        public RectTransform rt;
        public float x, y, targetY, speed, spin, angle, delay;
        public bool landed, gone;
    }

    private readonly List<Coin> coins = new List<Coin>();
    private Sprite sprite;
    private Image curtain;
    private RectTransform area;

    public static void Play(Sprite coinSprite, string sceneName)
    {
        if (instance != null || coinSprite == null) return;

        var go = new GameObject("CoinRainTransition", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // her şeyin önünde
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        scaler.matchWidthOrHeight = 0f;

        instance = go.AddComponent<CoinRainTransition>();
        instance.sprite = coinSprite;
        instance.area = (RectTransform)go.transform;
        instance.StartCoroutine(instance.Run(sceneName));
    }

    // Ekranı petek düzeninde, kenarlardan taşacak şekilde kaplayan coin yuvaları oluşturur
    void Build(float width, float height)
    {
        // Perde: coinlerin arkasında yavaşça dolar ve dokunuşları keser (geçiş sırasında butonlara basılmasın)
        curtain = NewImage("Curtain", area, new Color(CurtainColor.r, CurtainColor.g, CurtainColor.b, 0f));
        curtain.raycastTarget = true;
        var crt = curtain.rectTransform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        float halfW = width * 0.5f, halfH = height * 0.5f;
        int rows = Mathf.CeilToInt(height / SpacingY) + 2;
        int cols = Mathf.CeilToInt(width / SpacingX) + 2;
        for (int r = 0; r < rows; r++)
        {
            float rowY = -halfH + r * SpacingY;
            float offset = (r % 2 == 0) ? 0f : SpacingX * 0.5f;
            float rowT = (float)r / (rows - 1); // 0 = en alt sıra
            for (int c = 0; c < cols; c++)
            {
                var img = NewImage("Coin", area, Color.white);
                img.sprite = sprite;
                img.preserveAspect = true;
                float size = Random.Range(205f, 240f);
                img.rectTransform.sizeDelta = new Vector2(size, size);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                img.gameObject.SetActive(false);
                coins.Add(new Coin
                {
                    rt = img.rectTransform,
                    x = -halfW - SpacingX * 0.5f + c * SpacingX + offset + Random.Range(-8f, 8f),
                    targetY = rowY + Random.Range(-8f, 8f),
                    spin = Random.Range(-540f, 540f),
                    angle = Random.Range(0f, 360f),
                    delay = rowT * FillDuration + Random.Range(0f, 0.12f)
                });
            }
        }
        // Üst sıralar alttakilerin önünde dursun (yağan coin yığının üstüne düşüyormuş gibi görünür)
        for (int i = 0; i < coins.Count; i++) coins[i].rt.SetSiblingIndex(i + 1);
    }

    IEnumerator Run(string sceneName)
    {
        yield return null; // kanvas ölçeklensin, ekran boyutu doğru okunsun
        float width = area.rect.width;
        float halfH = area.rect.height * 0.5f;
        Build(width, area.rect.height);

        // 1) Coinler yağar ve alttan yukarı yığılarak ekranı tamamen kaplar
        float t = 0f;
        while (!AllLanded() && t < MaxPhaseTime)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            t += dt;
            FillCoins(t, dt, halfH);
            SetCurtain(Smooth(Mathf.Clamp01(t / (FillDuration + 0.3f))));
            yield return null;
        }
        foreach (var c in coins) Land(c);
        SetCurtain(1f);
        yield return null; // tam kaplı kare ekrana çıksın

        // 2) Sahne, hareketsiz coin duvarının arkasında yüklenir
        Time.timeScale = 1f;
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone) yield return null;
        // Yeni sahnenin ilk ağır kareleri de duvarın arkasında geçsin
        for (int i = 0; i < 3; i++) yield return null;

        // 3) Coinler alt sıradan başlayarak aşağı dökülür, perde açılır ve oyun görünür
        foreach (var c in coins)
        {
            float rowT = Mathf.InverseLerp(-halfH, halfH, c.targetY);
            c.delay = rowT * RevealDuration + Random.Range(0f, 0.1f);
            c.speed = Random.Range(0f, 300f);
        }
        float r = 0f;
        while (!AllGone() && r < MaxPhaseTime)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            r += dt;
            DropCoins(r, dt, halfH);
            SetCurtain(1f - Smooth(Mathf.Clamp01(r / (RevealDuration + 0.25f))));
            yield return null;
        }

        instance = null;
        Destroy(gameObject);
    }

    // Kapanış: her coin kendi gecikmesiyle üstten girer, düşer ve yuvasına oturunca durur
    void FillCoins(float time, float dt, float halfH)
    {
        foreach (var c in coins)
        {
            if (c.landed || time < c.delay) continue;
            if (!c.rt.gameObject.activeSelf)
            {
                c.rt.gameObject.SetActive(true);
                c.y = halfH + c.rt.sizeDelta.y;
                c.speed = Random.Range(900f, 1300f);
            }
            c.speed += Gravity * dt;
            c.y -= c.speed * dt;
            c.angle += c.spin * dt;
            if (c.y <= c.targetY) { Land(c); continue; }
            Place(c);
        }
    }

    // Açılış: coinler yuvalarından aşağı düşer, ekranın altından çıkınca kaybolur
    void DropCoins(float time, float dt, float halfH)
    {
        foreach (var c in coins)
        {
            if (c.gone || time < c.delay) continue;
            c.speed += Gravity * dt;
            c.y -= c.speed * dt;
            c.angle += c.spin * dt;
            if (c.y < -halfH - c.rt.sizeDelta.y)
            {
                c.rt.gameObject.SetActive(false);
                c.gone = true;
                continue;
            }
            Place(c);
        }
    }

    void Land(Coin c)
    {
        c.landed = true;
        c.y = c.targetY;
        c.speed = 0f;
        c.rt.gameObject.SetActive(true);
        Place(c);
    }

    static void Place(Coin c)
    {
        c.rt.anchoredPosition = new Vector2(c.x, c.y);
        c.rt.localRotation = Quaternion.Euler(0f, 0f, c.angle);
    }

    bool AllLanded()
    {
        foreach (var c in coins) if (!c.landed) return false;
        return true;
    }

    bool AllGone()
    {
        foreach (var c in coins) if (!c.gone) return false;
        return true;
    }

    static float Smooth(float p) => p * p * (3f - 2f * p);

    void SetCurtain(float a)
    {
        var col = curtain.color;
        col.a = a;
        curtain.color = col;
    }

    static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
