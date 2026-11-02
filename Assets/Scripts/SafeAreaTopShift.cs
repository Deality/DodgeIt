using UnityEngine;
using UnityEngine.UI;

// Ekranın üstündeki kamera deliği / çentikten arayüzü uzak tutar. Oyun o bölgenin altına da çizildiği için
// (Render Outside Safe Area) öğe deliğe değiyorsa yalnızca değmeyecek kadar aşağı kaydırılır; oyun alanı daralmasın.
// Bazı script'ler konumu Awake'te okuduğu için onlardan önce çalışır.
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaTopShift : MonoBehaviour
{
    [Tooltip("Öğenin görünen üst kenarının ekranın tepesine uzaklığı (kanvas birimi). Delik bundan kısaysa kaydırma gerekmez.")]
    public float topMargin;
    [Tooltip("Üst bar gibi ekranın en üstüne yaslı görsellerde: kayınca üstte açılan boşluğu bu renkle doldurur.")]
    public bool fillGapAbove;
    public Color fillColor = new Color32(68, 68, 176, 255);

    private const float Padding = 8f;

    // Test: 0'dan büyükse gerçek delik yerine ekran yüksekliğinin bu oranı kullanılır
    public static float DebugTopInsetRatio;

    void Awake()
    {
        float shift = ShiftFor(GetComponentInParent<Canvas>(true), topMargin);
        if (shift <= 0f) return;

        var rect = (RectTransform)transform;
        rect.anchoredPosition += new Vector2(0f, -shift);
        if (fillGapAbove) AddFill(rect, shift);
    }

    // Üst kenarı tepeden topMargin uzakta olan öğenin deliğe değmemesi için gereken kayma, kanvas biriminde
    public static float ShiftFor(Canvas canvas, float topMargin)
    {
        float pixels = TopCutoutPixels();
        if (pixels <= 0f || canvas == null) return 0f;
        return Mathf.Max(0f, pixels / ScaleFactor(canvas.rootCanvas) + Padding - topMargin);
    }

    // Deliğin tepeden aşağı uzandığı mesafe. Telefon delik bilgisi vermiyorsa güvenli alan kullanılır.
    static float TopCutoutPixels()
    {
        if (DebugTopInsetRatio > 0f) return Screen.height * DebugTopInsetRatio;

        float safeInset = Screen.height - Screen.safeArea.yMax;
        float hole = 0f;
        foreach (Rect cutout in Screen.cutouts)
            hole = Mathf.Max(hole, cutout.center.y > Screen.height * 0.5f ? Screen.height - cutout.yMin : cutout.yMax);
        return hole > 0f ? Mathf.Min(hole, safeInset > 0f ? safeInset : hole) : safeInset;
    }

    // Kanvas ölçeği Awake'te henüz güncellenmemiş olabilir; CanvasScaler'ın formülüyle hesaplanır
    static float ScaleFactor(Canvas canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return Mathf.Max(canvas.scaleFactor, 0.01f);
        float logWidth = Mathf.Log(Screen.width / scaler.referenceResolution.x, 2f);
        float logHeight = Mathf.Log(Screen.height / scaler.referenceResolution.y, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
    }

    void AddFill(RectTransform rect, float shift)
    {
        var go = new GameObject("SafeAreaFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        var img = go.GetComponent<Image>();
        img.color = fillColor;
        img.raycastTarget = false;
        var fill = img.rectTransform;
        fill.SetParent(rect, false);
        fill.SetAsFirstSibling();
        fill.anchorMin = new Vector2(0f, 1f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.pivot = new Vector2(0.5f, 0f);
        fill.offsetMin = new Vector2(0f, -8f); // görselin üst kenar çizgisini de örter
        fill.offsetMax = new Vector2(0f, shift + 20f);
    }
}
