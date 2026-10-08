using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NearMissStreakUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI multiplierText;
    public Image timerBarFill;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color color1 = new Color(1f, 0.85f, 0f, 1f);
    public Color color2 = new Color(1f, 0.50f, 0f, 1f);
    public Color color3 = new Color(1f, 0.15f, 0f, 1f);

    [Header("Fade")]
    public float fadeSpeed = 8f;

    [Header("x5 Alev Efekti (en yüksek seri)")]
    public bool enableMaxStreakFlames = true;
    [Tooltip("Bar ve yazı, color3 ile bu renk arasında alev gibi titreşir.")]
    public Color flameHotColor = new Color(1f, 0.95f, 0.35f, 1f);
    public float flameFlickerSpeed = 14f;
    [Tooltip("Saniyede bardan yükselen alev parçası sayısı.")]
    public float flameRate = 26f;
    public float flameLifetime = 0.45f;
    [Tooltip("Alev parçasının yükseldiği mesafe (piksel).")]
    public float flameRise = 46f;
    public float flameSize = 26f;
    [Tooltip("x5 yazısının nabız büyüklüğü.")]
    public float textPulseAmount = 0.12f;

    // Yalnızca ekranda görünen etiketler; asıl skor çarpanları (x1.5 / x2 / x4) GameManager'da ve değişmedi
    private static readonly string[] Labels = { "", "x2", "x3", "x5" };

    private class Flame
    {
        public RectTransform rt;
        public Image img;
        public float age;
        public float life;
        public Vector2 start;
        public float drift;
        public float size;
    }

    private readonly List<Flame> liveFlames = new List<Flame>();
    private readonly Stack<Flame> flamePool = new Stack<Flame>();
    private float flameEmitAccumulator;
    private static Sprite flameSprite;

    void Update()
    {
        if (GameManager.instance == null) return;

        bool active = GameManager.instance.NearMissStreakCount > 0
                   && GameManager.instance.isGameActive
                   && !GameManager.instance.IsGameOver;

        float targetAlpha = active ? 1f : 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        int count = active ? GameManager.instance.NearMissStreakCount : 0;
        bool onFire = enableMaxStreakFlames && count >= 3;

        UpdateFlames(onFire);
        if (!onFire && multiplierText != null) multiplierText.transform.localScale = Vector3.one;

        if (!active) return;

        float timer  = GameManager.instance.NearMissStreakTimer;
        float total  = GameManager.instance.NearMissCurrentDuration;

        Color c = count == 1 ? color1 : count == 2 ? color2 : color3;
        if (onFire)
        {
            // Alev gibi düzensiz titreşim
            c = Color.Lerp(color3, flameHotColor, Mathf.PerlinNoise(Time.time * flameFlickerSpeed, 0.37f));
        }

        if (multiplierText != null)
        {
            multiplierText.text  = count <= 3 ? Labels[count] : Labels[3];
            multiplierText.color = c;
            if (onFire)
                multiplierText.transform.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(Time.time * 8f)) * textPulseAmount);
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = Mathf.Clamp01(timer / total);
            timerBarFill.color      = c;
        }
    }

    // --- x5 ALEVLERİ: barın dolu kısmından yükselip sönen küçük parçalar ---

    void UpdateFlames(bool emit)
    {
        if (emit && timerBarFill != null && timerBarFill.fillAmount > 0.02f)
        {
            flameEmitAccumulator += flameRate * Time.deltaTime;
            while (flameEmitAccumulator >= 1f)
            {
                flameEmitAccumulator -= 1f;
                EmitFlame();
            }
        }
        else
        {
            flameEmitAccumulator = 0f;
        }

        for (int i = liveFlames.Count - 1; i >= 0; i--)
        {
            Flame f = liveFlames[i];
            f.age += Time.deltaTime;
            float p = f.age / f.life;
            if (p >= 1f)
            {
                f.rt.gameObject.SetActive(false);
                liveFlames.RemoveAt(i);
                flamePool.Push(f);
                continue;
            }

            f.rt.anchoredPosition = f.start + new Vector2(f.drift * p, flameRise * p);
            f.rt.sizeDelta = Vector2.one * f.size * (1f - 0.6f * p);

            Color c = Color.Lerp(flameHotColor, color3, p);
            c.a = 1f - p;
            f.img.color = c;
        }
    }

    void EmitFlame()
    {
        RectTransform bar = timerBarFill.rectTransform;
        Rect r = bar.rect;

        // Yalnızca barın dolu olan kısmından çıksın
        float filled = r.width * timerBarFill.fillAmount;
        bool fromRight = timerBarFill.type == Image.Type.Filled
                      && timerBarFill.fillMethod == Image.FillMethod.Horizontal
                      && timerBarFill.fillOrigin == (int)Image.OriginHorizontal.Right;
        float x = fromRight ? r.xMax - Random.value * filled : r.xMin + Random.value * filled;

        Flame f = flamePool.Count > 0 ? flamePool.Pop() : CreateFlame(bar);
        f.age = 0f;
        f.life = flameLifetime * Random.Range(0.7f, 1.2f);
        f.start = new Vector2(x, r.center.y + r.height * Random.Range(0f, 0.5f));
        f.drift = Random.Range(-10f, 10f);
        f.size = flameSize * Random.Range(0.6f, 1.15f);
        f.rt.anchoredPosition = f.start;
        f.rt.gameObject.SetActive(true);
        liveFlames.Add(f);
    }

    Flame CreateFlame(RectTransform bar)
    {
        var go = new GameObject("StreakFlame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = bar.gameObject.layer;

        var rt = (RectTransform)go.transform;
        rt.SetParent(bar, false);
        // Konum, barın pivotuna göre (rect koordinatlarıyla aynı uzay)
        rt.anchorMin = rt.anchorMax = bar.pivot;
        rt.pivot = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.sprite = GetFlameSprite();
        img.raycastTarget = false;

        return new Flame { rt = rt, img = img };
    }

    static Sprite GetFlameSprite()
    {
        if (flameSprite != null) return flameSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "StreakFlame",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var pixels = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(falloff * falloff * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        flameSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return flameSprite;
    }
}
