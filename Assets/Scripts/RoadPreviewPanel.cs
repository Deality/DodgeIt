using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Marketteki yol kartlarındaki küçük görselin büyük hâlini gösteren tam ekran önizleme.
// Yol, oyundaki gibi yukarıdan aşağı akar. Arayüz ilk kullanımda kodla kurulur; ekranın
// herhangi bir yerine dokununca kapanır.
public class RoadPreviewPanel : MonoBehaviour
{
    private static RoadPreviewPanel instance;

    private const float PreviewWidth = 640f;
    private const float FrameThickness = 14f;
    private const float ScrollSpeed = 520f; // saniyede piksel (canvas birimi)

    private static readonly Color BackdropColor = new Color(0f, 0f, 0.12f, 0.88f);
    private static readonly Color FrameColor = new Color32(138, 221, 84, 255);

    private RectTransform[] tiles;
    private Image[] tileImages;
    private float tileHeight;
    private float scrollOffset;

    public static void Show(Sprite roadSprite, Canvas canvas, TMP_FontAsset font)
    {
        if (roadSprite == null || canvas == null) return;
        if (instance == null) instance = Build(canvas.rootCanvas, font);
        instance.Open(roadSprite);
    }

    static RoadPreviewPanel Build(Canvas canvas, TMP_FontAsset font)
    {
        // Karartma: tüm ekranı kaplar, dokununca kapatır
        var root = new GameObject("RoadPreviewPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        root.layer = canvas.gameObject.layer;
        var rootRt = (RectTransform)root.transform;
        rootRt.SetParent(canvas.transform, false);
        Stretch(rootRt);
        root.GetComponent<Image>().color = BackdropColor;

        var panel = root.AddComponent<RoadPreviewPanel>();
        var button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(panel.Close);

        // Çerçeve + içinde yolun aktığı maskeli pencere
        var frame = NewImage("Frame", rootRt, FrameColor);
        frame.raycastTarget = false;
        var viewport = NewImage("Viewport", frame.rectTransform, Color.black);
        viewport.raycastTarget = false;
        viewport.gameObject.AddComponent<RectMask2D>();
        Stretch(viewport.rectTransform);
        viewport.rectTransform.offsetMin = Vector2.one * FrameThickness;
        viewport.rectTransform.offsetMax = Vector2.one * -FrameThickness;

        panel.frame = frame.rectTransform;
        panel.tiles = new RectTransform[2];
        panel.tileImages = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            Image tile = NewImage("RoadTile" + i, viewport.rectTransform, Color.white);
            tile.raycastTarget = false;
            tile.rectTransform.anchorMin = tile.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.tiles[i] = tile.rectTransform;
            panel.tileImages[i] = tile;
        }

        // Kapatma ipucu
        var hintGo = new GameObject("CloseHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        hintGo.layer = root.layer;
        var hint = hintGo.GetComponent<TextMeshProUGUI>();
        hint.rectTransform.SetParent(rootRt, false);
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        hint.rectTransform.sizeDelta = new Vector2(900f, 80f);
        if (font != null) hint.font = font;
        hint.text = "TAP ANYWHERE TO CLOSE";
        hint.fontSize = 34f;
        hint.alignment = TextAlignmentOptions.Center;
        hint.color = new Color(1f, 1f, 1f, 0.8f);
        hint.raycastTarget = false;
        panel.hint = hint.rectTransform;

        root.SetActive(false);
        return panel;
    }

    private RectTransform frame;
    private RectTransform hint;

    void Open(Sprite roadSprite)
    {
        // Yolun en-boy oranını koru
        float aspect = roadSprite.rect.height / roadSprite.rect.width;
        tileHeight = PreviewWidth * aspect;

        // Ekrana sığmıyorsa küçült
        float available = ((RectTransform)transform).rect.height - 320f;
        float scale = tileHeight + FrameThickness * 2f > available && available > 0f ? available / (tileHeight + FrameThickness * 2f) : 1f;
        float width = PreviewWidth * scale;
        tileHeight *= scale;

        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(width + FrameThickness * 2f, tileHeight + FrameThickness * 2f);
        frame.anchoredPosition = new Vector2(0f, 40f);
        hint.anchoredPosition = new Vector2(0f, 40f - frame.sizeDelta.y * 0.5f - 70f);

        for (int i = 0; i < tiles.Length; i++)
        {
            tileImages[i].sprite = roadSprite;
            tiles[i].sizeDelta = new Vector2(width, tileHeight + 2f); // +2: iki karo arasında çizgi kalmasın
        }

        scrollOffset = 0f;
        PlaceTiles();

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        // unscaledDeltaTime: menüde timeScale ne olursa olsun aksın
        scrollOffset = Mathf.Repeat(scrollOffset + ScrollSpeed * Time.unscaledDeltaTime, tileHeight);
        PlaceTiles();
    }

    void PlaceTiles()
    {
        tiles[0].anchoredPosition = new Vector2(0f, -scrollOffset);
        tiles[1].anchoredPosition = new Vector2(0f, -scrollOffset + tileHeight);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    static Image NewImage(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
