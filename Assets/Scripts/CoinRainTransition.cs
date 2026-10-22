using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Coin Rush moduna geçiş: ekranın üstünden coinler yağar, arkada lacivert bir perde dolar ve ekran tamamen
// kapanınca sahne yüklenir. Yeni sahnede coinler aşağı akıp ekrandan çıkarken perde açılır.
// Kendi kanvasında çalışır ve sahne değişiminde yok olmaz (iş bitince kendini siler).
public class CoinRainTransition : MonoBehaviour
{
    public static bool IsRunning => instance != null;
    private static CoinRainTransition instance;

    private const float RefWidth = 1080f, RefHeight = 1920f;
    private const float CoverDuration = 1.1f;   // ekranın coinlerle kaplanma süresi
    private const float RevealDuration = 1.0f;  // yeni sahnede perdenin açılma süresi
    private const float MaxExitWait = 3f;       // güvenlik: coinler bu sürede çıkmadıysa geçiş yine biter
    private const float Gravity = 3200f;        // coinler yavaş başlar, düştükçe hızlanır
    private const int CoinCount = 90;
    private static readonly Color CurtainColor = new Color32(3, 0, 92, 255);

    private class Coin
    {
        public RectTransform rt;
        public float x, y, speed, startSpeed, spin, delay;
        public bool done;
    }

    private readonly List<Coin> coins = new List<Coin>();
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
        instance.Build(coinSprite);
        instance.StartCoroutine(instance.Run(sceneName));
    }

    void Build(Sprite coinSprite)
    {
        area = (RectTransform)transform;

        // Perde: dokunuşları da keser (geçiş sırasında butonlara basılmasın)
        curtain = NewImage("Curtain", area, new Color(CurtainColor.r, CurtainColor.g, CurtainColor.b, 0f));
        curtain.raycastTarget = true;
        var crt = curtain.rectTransform;
        crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
        crt.offsetMin = crt.offsetMax = Vector2.zero;

        for (int i = 0; i < CoinCount; i++)
        {
            var img = NewImage("Coin", area, Color.white);
            img.sprite = coinSprite;
            img.preserveAspect = true;
            float size = Random.Range(130f, 250f);
            img.rectTransform.sizeDelta = new Vector2(size, size);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            coins.Add(new Coin
            {
                rt = img.rectTransform,
                x = Random.Range(-0.5f, 0.5f),
                y = 0f,
                startSpeed = Random.Range(500f, 1000f),
                spin = Random.Range(-360f, 360f),
                delay = (float)i / CoinCount * CoverDuration * 0.8f + Random.Range(0f, 0.08f)
            });
            img.gameObject.SetActive(false);
        }
        // Büyük coinler önde dursun
        coins.Sort((a, b) => a.rt.sizeDelta.x.CompareTo(b.rt.sizeDelta.x));
        for (int i = 0; i < coins.Count; i++) coins[i].rt.SetSiblingIndex(i + 1);
    }

    IEnumerator Run(string sceneName)
    {
        float halfH = area.rect.height * 0.5f;
        float width = area.rect.width;

        // 1) Coinler yağar, perde dolar
        float t = 0f;
        while (t < CoverDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / CoverDuration);
            SetCurtain(Smooth(p));
            UpdateCoins(t, halfH, width, false);
            yield return null;
        }
        SetCurtain(1f);

        // 2) Sahne perdenin arkasında yüklenir
        Time.timeScale = 1f;
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone)
        {
            t += Time.unscaledDeltaTime;
            UpdateCoins(t, halfH, width, false);
            yield return null;
        }

        // 3) Perde yumuşakça açılır; ekrandaki coinler aşağı düşüp ekrandan kendileri çıkar (yeni coin doğmaz).
        // Geçiş, son coin ekranın altından çıkınca biter; hiçbir coin ekranın ortasında kaybolmaz.
        float r = 0f;
        while (r < RevealDuration || (AnyCoinOnScreen() && r < MaxExitWait))
        {
            r += Time.unscaledDeltaTime;
            t += Time.unscaledDeltaTime;
            SetCurtain(1f - Smooth(Mathf.Clamp01(r / RevealDuration)));
            UpdateCoins(t, halfH, width, true);
            yield return null;
        }

        instance = null;
        Destroy(gameObject);
    }

    // Her coin kendi gecikmesiyle üstten girer, düşer; ekranın altından çıkınca (kapanış sırasında) tekrar üste döner
    void UpdateCoins(float time, float halfH, float width, bool revealing)
    {
        foreach (var c in coins)
        {
            if (time < c.delay) continue;
            if (!c.rt.gameObject.activeSelf)
            {
                if (revealing || c.done) continue; // açılırken yeni coin girmez
                c.rt.gameObject.SetActive(true);
                c.y = halfH + c.rt.sizeDelta.y;
                c.speed = c.startSpeed;
            }
            c.speed += Gravity * Time.unscaledDeltaTime;
            c.y -= c.speed * Time.unscaledDeltaTime;
            if (c.y < -halfH - c.rt.sizeDelta.y)
            {
                if (revealing) { c.rt.gameObject.SetActive(false); c.done = true; continue; }
                c.y = halfH + c.rt.sizeDelta.y;
                c.x = Random.Range(-0.5f, 0.5f);
                c.speed = c.startSpeed;
            }
            c.rt.anchoredPosition = new Vector2(c.x * width, c.y);
            c.rt.localRotation = Quaternion.Euler(0f, 0f, c.spin * time);
        }
    }

    bool AnyCoinOnScreen()
    {
        foreach (var c in coins) if (c.rt.gameObject.activeSelf) return true;
        return false;
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
