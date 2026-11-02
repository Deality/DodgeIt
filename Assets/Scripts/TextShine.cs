using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Yazıyı parıltılı gösterir: harflerin üzerinden arada bir parlama geçer, yazının çevresinde küçük yıldızlar yanıp söner.
[RequireComponent(typeof(TMP_Text))]
public class TextShine : MonoBehaviour
{
    public Color shineColor = new Color(1f, 0.97f, 0.7f);
    [Tooltip("Parlamanın yazıyı baştan sona geçme süresi (saniye).")]
    public float sweepDuration = 0.8f;
    [Tooltip("İki parlama arasındaki bekleme süresi (saniye).")]
    public float interval = 1.2f;
    [Tooltip("Parlama şeridinin genişliği (yazı genişliğine oranla).")]
    public float bandWidth = 0.35f;
    public int starCount = 3;
    [Tooltip("Yıldızların boyutu (yazı yüksekliğine oranla).")]
    public float starSize = 0.8f;
    public float starPeriod = 1.4f;

    private static Sprite starSprite;

    private TMP_Text text;
    private RectTransform[] stars;
    private Image[] starImages;
    private float startTime;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        startTime = Time.unscaledTime;
    }

    void OnDisable()
    {
        // Yazı kendi rengine dönsün
        if (text != null) text.SetVerticesDirty();
    }

    void LateUpdate()
    {
        TMP_TextInfo info = text.textInfo;
        if (info == null || info.characterCount == 0) return;

        var first = info.characterInfo[0];
        var last = info.characterInfo[info.characterCount - 1];
        float left = first.bottomLeft.x, right = last.topRight.x, width = Mathf.Max(right - left, 1f);

        // Şerit yazının tamamen solundan tamamen sağına geçer, sonra bekler
        float cycle = sweepDuration + interval;
        float p = ((Time.unscaledTime - startTime) % cycle) / Mathf.Max(sweepDuration, 0.01f);
        float band = width * bandWidth;
        float centre = Mathf.Lerp(left - band, right + band, Mathf.Clamp01(p));

        Color32 baseColor = text.color;
        Color32 shine = shineColor;
        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible) continue;
            var meshInfo = info.meshInfo[c.materialReferenceIndex];
            Color32[] colors = meshInfo.colors32;
            Vector3[] vertices = meshInfo.vertices;
            for (int v = c.vertexIndex; v < c.vertexIndex + 4; v++)
            {
                float k = p >= 1f ? 0f : Mathf.Clamp01(1f - Mathf.Abs(vertices[v].x - centre) / band);
                colors[v] = Color32.Lerp(baseColor, shine, k);
            }
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        UpdateStars(left, right, first.descender, first.ascender);
    }

    // Yıldızlar sırayla büyüyüp küçülür; küçüldüğünde yeni bir yere geçer
    void UpdateStars(float left, float right, float bottom, float top)
    {
        if (starCount <= 0) return;
        if (stars == null) CreateStars();

        float size = (top - bottom) * starSize;
        for (int i = 0; i < stars.Length; i++)
        {
            float t = (Time.unscaledTime - startTime) / starPeriod + (float)i / stars.Length;
            float phase = t - Mathf.Floor(t);
            float s = Mathf.Sin(phase * Mathf.PI);
            if (phase < 0.05f || stars[i].sizeDelta.x <= 0f)
            {
                // Rakamların üstüne binmesin: yalnızca yazının üst ve alt kenarında
                stars[i].localPosition = new Vector3(Random.Range(left, right), Random.value < 0.5f ? top : bottom, 0f);
                stars[i].sizeDelta = new Vector2(size, size);
            }
            stars[i].localScale = Vector3.one * s;
            stars[i].localRotation = Quaternion.Euler(0f, 0f, phase * 60f);
            Color c = starImages[i].color; c.a = s; starImages[i].color = c;
        }
    }

    void CreateStars()
    {
        stars = new RectTransform[starCount];
        starImages = new Image[starCount];
        for (int i = 0; i < starCount; i++)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var img = go.GetComponent<Image>();
            img.sprite = StarSprite();
            img.color = Color.white;
            img.raycastTarget = false;
            stars[i] = img.rectTransform;
            stars[i].SetParent(transform, false);
            stars[i].sizeDelta = Vector2.zero;
            starImages[i] = img;
        }
    }

    // Dört köşeli yıldız, kodla çizilir
    static Sprite StarSprite()
    {
        if (starSprite != null) return starSprite;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                // Astroid eğrisi: kollar merkezden köşelere doğru incelir
                float d = Mathf.Sqrt(dx) + Mathf.Sqrt(dy);
                float a = Mathf.Clamp01((1f - d) * 8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return starSprite;
    }
}
