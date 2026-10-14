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

    [Header("Seviye Göstergesi (x2 / x3 / x5)")]
    [Tooltip("Barın sağında üç küçük etiket: hangi seviyede olunduğunu ve bir sonrakini gösterir.")]
    public bool showTierPips = true;
    public Vector2 tierPipsPosition = new Vector2(264f, 84f);
    public Vector2 tierPipSize = new Vector2(48f, 26f);
    public float tierPipSpacing = 54f;
    public Color tierOffColor = new Color(0f, 0f, 0.15f, 0.55f);

    [Header("Seviye Atlama Efekti")]
    [Tooltip("Yeni seviyeye geçince yazının ne kadar büyüyüp yerine oturacağı.")]
    public float tierUpPunch = 0.6f;
    public float tierUpPunchDuration = 0.35f;
    [Tooltip("Seviye atlayınca barın üstünde parlayan beyaz flaş süresi.")]
    public float tierUpFlashDuration = 0.25f;

    [Header("Yazının Arkasındaki Parıltı")]
    public bool showGlow = true;
    public float glowSize = 150f;
    [Range(0f, 1f)] public float glowAlpha = 0.45f;

    [Header("Süre Bitiyor Uyarısı")]
    [Tooltip("Bar bu oranın altına inince yazı hızlıca yanıp söner.")]
    [Range(0f, 1f)] public float lowTimeThreshold = 0.25f;

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

    private Image[] tierPips;
    private TextMeshProUGUI[] tierPipTexts;
    private Image glow;
    private int lastCount;
    private float punchTimer;
    private float flashTimer;

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

        if (!active)
        {
            lastCount = 0;
            if (multiplierText != null) multiplierText.transform.localScale = Vector3.one;
            return;
        }

        // Seviye atlandı: yazı büyüyüp yerine oturur, bar bir an parlar
        if (count > lastCount)
        {
            punchTimer = tierUpPunchDuration;
            flashTimer = tierUpFlashDuration;
        }
        lastCount = count;
        punchTimer = Mathf.Max(0f, punchTimer - Time.deltaTime);
        flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);

        float total = GameManager.instance.NearMissCurrentDuration;
        float fill = total > 0f ? Mathf.Clamp01(GameManager.instance.NearMissStreakTimer / total) : 0f;
        ApplyVisual(count, fill, Time.time);
    }

    // Editörde, oyun çalışmadan taslağı görmek için (görsel kontrol)
    public void PreviewInEditor(int count, float fill, float punchProgress)
    {
        lastCount = count;
        punchTimer = tierUpPunchDuration * (1f - Mathf.Clamp01(punchProgress));
        flashTimer = tierUpFlashDuration * (1f - Mathf.Clamp01(punchProgress));
        ApplyVisual(Mathf.Clamp(count, 1, 3), fill, 0.4f);
    }

    void ApplyVisual(int count, float fill, float time)
    {
        EnsureDecorations();
        bool onFire = enableMaxStreakFlames && count >= 3;

        Color c = count == 1 ? color1 : count == 2 ? color2 : color3;
        if (onFire)
        {
            // Alev gibi düzensiz titreşim
            c = Color.Lerp(color3, flameHotColor, Mathf.PerlinNoise(time * flameFlickerSpeed, 0.37f));
        }

        if (multiplierText != null)
        {
            multiplierText.text = count <= 3 ? Labels[count] : Labels[3];

            // Süre bitmek üzereyken yanıp söner
            Color textColor = c;
            if (fill < lowTimeThreshold) textColor.a = 0.35f + 0.65f * Mathf.Abs(Mathf.Cos(time * 14f));
            multiplierText.color = textColor;

            float scale = 1f;
            if (punchTimer > 0f)
            {
                float p = 1f - punchTimer / Mathf.Max(tierUpPunchDuration, 0.01f);
                scale += tierUpPunch * (1f - EaseOutBack(p));
            }
            if (onFire) scale *= 1f + Mathf.Abs(Mathf.Sin(time * 8f)) * textPulseAmount;
            multiplierText.transform.localScale = Vector3.one * scale;
        }

        if (timerBarFill != null)
        {
            // Seviye atlayınca barın dolu kısmı bir an beyaza parlar
            float flash = flashTimer > 0f ? flashTimer / Mathf.Max(tierUpFlashDuration, 0.01f) : 0f;
            timerBarFill.fillAmount = fill;
            timerBarFill.color = Color.Lerp(c, Color.white, flash * 0.8f);
        }

        if (glow != null)
        {
            Color g = c;
            g.a = glowAlpha * (0.75f + 0.25f * Mathf.Sin(time * 5f));
            glow.color = g;
        }

        if (tierPips != null)
        {
            Color[] tierColors = { color1, color2, color3 };
            for (int i = 0; i < tierPips.Length; i++)
            {
                int tier = i + 1;
                bool current = tier == count;
                bool passed = tier < count;
                Color tc = tierColors[i];
                tierPips[i].color = current ? tc : passed ? new Color(tc.r, tc.g, tc.b, 0.45f) : tierOffColor;
                tierPips[i].rectTransform.localScale = Vector3.one * (current ? 1.15f : 1f);
                tierPipTexts[i].color = current ? new Color(0.05f, 0.02f, 0.25f, 1f) : new Color(1f, 1f, 1f, passed ? 0.85f : 0.4f);
            }
        }
    }

    static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
    }

    // Parıltı ve x2/x3/x5 etiketleri kodla bir kez kurulur (sahneye kaydedilmez)
    void EnsureDecorations()
    {
        var root = (RectTransform)transform;

        if (showGlow && glow == null && multiplierText != null)
        {
            glow = NewImage("StreakGlow", root, GetFlameSprite());
            glow.rectTransform.sizeDelta = Vector2.one * glowSize;
            multiplierText.ForceMeshUpdate();
            Vector3 textCenter = multiplierText.rectTransform.TransformPoint(multiplierText.textBounds.center);
            glow.rectTransform.position = textCenter;
            glow.transform.SetSiblingIndex(multiplierText.transform.GetSiblingIndex()); // yazının arkasında
        }

        if (showTierPips && tierPips == null && multiplierText != null)
        {
            tierPips = new Image[3];
            tierPipTexts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                var pip = NewImage("StreakTier" + (i + 1), root, null);
                pip.rectTransform.sizeDelta = tierPipSize;
                pip.rectTransform.anchoredPosition = tierPipsPosition + new Vector2((i - 1) * tierPipSpacing, 0f);
                tierPips[i] = pip;

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textGo.layer = gameObject.layer;
                textGo.hideFlags = HideFlags.DontSave;
                var t = textGo.GetComponent<TextMeshProUGUI>();
                t.rectTransform.SetParent(pip.rectTransform, false);
                t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                t.font = multiplierText.font;
                t.fontSharedMaterial = multiplierText.fontSharedMaterial;
                t.text = Labels[i + 1];
                t.fontSize = tierPipSize.y * 0.7f;
                t.alignment = TextAlignmentOptions.Center;
                t.raycastTarget = false;
                tierPipTexts[i] = t;
            }
        }
    }

    Image NewImage(string name, RectTransform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.hideFlags = HideFlags.DontSave;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
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
