using UnityEngine;

// Kalkan / Kademe Düşürme gibi toplanabilir güçlendirmelerin arkasına parlayan bir hale ekler.
// Prefabdaki düz, sabit parlama dairesinin (sourceGlow) yerini alır: onun konumunu, boyutunu ve
// sıralamasını kullanıp yerine yumuşak kenarlı, nabız gibi atan bir hale + yavaşça dönen ışınlar koyar.
// Dokular çalışma anında bir kez üretilir ve tüm güçlendirmeler arasında paylaşılır.
public class PickupGlow : MonoBehaviour
{
    [Header("Kaynak (Prefabdaki eski düz parlama)")]
    public SpriteRenderer sourceGlow;
    [Tooltip("Kapalıysa kaynak görünür kalır: parlama, kaynağın kendi görselinin (örn. altın) arkasına eklenir.")]
    public bool hideSource = true;
    [Tooltip("Parlamanın, kaynağın sıralamasına (sorting order) göre farkı. Kaynağın arkasında kalsın diye -1 verin.")]
    public int sortingOrderOffset = 0;

    [Header("Renk")]
    public Color glowColor = new Color(0f, 0.9f, 1f, 1f);

    [Header("Hale (Halo)")]
    [Tooltip("Eski parlama dairesine göre hale boyutu.")]
    public float haloSize = 1.6f;
    public float haloMinAlpha = 0.45f;
    public float haloMaxAlpha = 0.9f;
    public float haloPulseSpeed = 4f;
    public float haloPulseAmount = 0.12f;

    [Header("Işınlar")]
    public bool enableRays = true;
    [Tooltip("Eski parlama dairesine göre ışın boyutu.")]
    public float raySize = 2.2f;
    public float rayAlpha = 0.35f;
    public float rayRotateSpeed = 40f;

    private Transform halo;
    private Transform rays;
    private SpriteRenderer haloRenderer;
    private SpriteRenderer rayRenderer;
    private float baseScale;
    private float timeOffset;

    private static Sprite haloSprite;
    private static Sprite raySprite;

    void Awake()
    {
        if (sourceGlow == null) return;

        Transform parent = sourceGlow.transform.parent;
        Vector3 localPos = sourceGlow.transform.localPosition;

        // Oluşturduğumuz sprite'lar 1 birim çapında; eski dairenin çapına eşitle
        baseScale = sourceGlow.sprite.bounds.size.x * sourceGlow.transform.localScale.x;

        if (enableRays)
        {
            rayRenderer = CreateLayer("GlowRays", GetRaySprite(), parent, localPos + new Vector3(0f, 0f, 0.01f));
            rays = rayRenderer.transform;
            rays.localScale = Vector3.one * baseScale * raySize;
        }

        haloRenderer = CreateLayer("GlowHalo", GetHaloSprite(), parent, localPos);
        halo = haloRenderer.transform;

        if (hideSource) sourceGlow.enabled = false;
    }

    void OnEnable()
    {
        // Havuzdan gelen güçlendirmeler aynı anda nabız atmasın
        timeOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        if (halo == null) return;

        float t = Time.time + timeOffset;
        float wave = (Mathf.Sin(t * haloPulseSpeed) + 1f) * 0.5f; // 0..1

        halo.localScale = Vector3.one * baseScale * haloSize * (1f + (wave - 0.5f) * 2f * haloPulseAmount);
        haloRenderer.color = WithAlpha(glowColor, Mathf.Lerp(haloMinAlpha, haloMaxAlpha, wave));

        if (rays != null)
        {
            rays.localRotation = Quaternion.Euler(0f, 0f, t * rayRotateSpeed);
            rayRenderer.color = WithAlpha(glowColor, rayAlpha * Mathf.Lerp(0.7f, 1f, wave));
        }
    }

    SpriteRenderer CreateLayer(string name, Sprite sprite, Transform parent, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = sourceGlow.sharedMaterial;
        sr.sortingLayerID = sourceGlow.sortingLayerID;
        sr.sortingOrder = sourceGlow.sortingOrder + sortingOrderOffset;
        sr.color = WithAlpha(glowColor, 0f);
        return sr;
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    // --- PROSEDÜREL DOKULAR ---

    static Sprite GetHaloSprite()
    {
        if (haloSprite == null)
        {
            haloSprite = BuildSprite("PickupGlowHalo", 128, (r, angle) =>
            {
                // Merkezde parlak, kenara doğru yumuşakça sönen radyal gradyan
                float falloff = Mathf.Clamp01(1f - r);
                return falloff * falloff * (3f - 2f * falloff);
            });
        }
        return haloSprite;
    }

    static Sprite GetRaySprite()
    {
        if (raySprite == null)
        {
            const int rayCount = 8;
            raySprite = BuildSprite("PickupGlowRays", 256, (r, angle) =>
            {
                float beam = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * rayCount * 0.5f)), 12f);
                float falloff = Mathf.Clamp01(1f - r);
                return beam * falloff * falloff;
            });
        }
        return raySprite;
    }

    static Sprite BuildSprite(string name, int size, System.Func<float, float, float> alphaAt)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
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
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = r >= 1f ? 0f : alphaAt(r, Mathf.Atan2(dy, dx));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        // pixelsPerUnit = size -> sprite 1 birim genişliğinde
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
