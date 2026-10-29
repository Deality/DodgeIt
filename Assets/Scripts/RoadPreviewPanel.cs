using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Marketteki yol kartlarındaki küçük görselin büyük hâlini gösteren tam ekran önizleme.
// Yol, oyundaki gibi yukarıdan aşağı akar. Araba kartları da bunu kullanır: araba, seçili yolun ortasında durur. Arayüz ilk kullanımda kodla kurulur; ekranın
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

    // carWidthRatio: arabanın oyundaki genişliğinin yol genişliğine oranı (CarDatabase.CarWidthOnRoad)
    // carPrefab: verilirse araba prefabdaki hâliyle (egzoz alevi, gölge dahil) ayrı bir kamerayla çizilir
    public static void Show(Sprite roadSprite, Canvas canvas, TMP_FontAsset font, Sprite carSprite = null, float carWidthRatio = 0.12f, GameObject carPrefab = null)
    {
        if (roadSprite == null || canvas == null) return;
        if (instance == null) instance = Build(canvas.rootCanvas, font);
        instance.carPrefab = carSprite != null ? carPrefab : null;
        instance.Open(roadSprite, carSprite, carWidthRatio);
    }

    // --- Araba prefabının canlı görüntüsü ---
    // Parçacık efektleri (egzoz alevi) arayüzde çizilemediği için araba, sahnenin çok uzağında oluşturulup
    // kendi kamerasıyla bir RenderTexture'a çizilir; panel bu görüntüyü arabanın yerinde gösterir.
    private static readonly Vector3 StagePosition = new Vector3(100000f, 100000f, 0f);
    private const float PreviewGameSpeed = 60f; // oyun başındaki yol hızı (km/h göstergesindeki değer)
    private GameObject carPrefab;
    private GameObject stageCar;
    private Camera stageCamera;
    private RenderTexture stageTexture;
    private RawImage carView;

    bool ShowLiveCar(float carWidthUI)
    {
        ClearStage();
        if (carPrefab == null) return false;

        // Ebeveyn kapalıyken kopyalanır: oyun scriptleri (kontrol, spawn vb.) hiç çalışmadan kapatılır
        var holder = new GameObject("CarPreviewStage");
        holder.SetActive(false);
        holder.transform.position = StagePosition;
        stageCar = holder;
        GameObject car = Instantiate(carPrefab, holder.transform);
        car.transform.localPosition = Vector3.zero; // dönüş prefabdaki gibi kalır
        // Kapalı bileşenlerin bile Awake'i çalışır (ör. CarController2D.instance'ı ezer); bu yüzden silinir
        foreach (var mb in car.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(mb is ExhaustEffect)) DestroyImmediate(mb);
        foreach (var anim in car.GetComponentsInChildren<Animator>(true)) anim.enabled = false;
        foreach (var col in car.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foreach (var rb in car.GetComponentsInChildren<Rigidbody2D>(true)) rb.simulated = false;
        foreach (var audio in car.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        // Yalnızca yüksek hızda çıkan rüzgar çizgileri kapalı; geri kalan her şey prefabdaki gibi
        foreach (Transform t in car.GetComponentsInChildren<Transform>(true))
            if (t.name.Contains("SpeedLines")) t.gameObject.SetActive(false);

        var body = car.GetComponent<SpriteRenderer>();
        if (body == null) body = car.GetComponentInChildren<SpriteRenderer>(true);
        if (body == null || body.sprite == null) { ClearStage(); return false; }

        holder.SetActive(true);

        // Egzoz alevi oyunda yol hızıyla geriye uzar (ExhaustEffect); önizlemede oyun hızı olmadığı için aynısı burada verilir
        foreach (var ex in car.GetComponentsInChildren<ExhaustEffect>(true))
        {
            ex.enabled = false;
            var ps = ex.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startSize = ex.normalStartSize * ex.sizeMultiplier;
            var em = ps.emission;
            em.rateOverTime = ex.normalEmissionRate;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(-PreviewGameSpeed * ex.windForceMultiplier);
            var shape = ps.shape;
            shape.angle = 0.5f;
            shape.radius = 0.05f;
            ps.Clear();
            ps.Play();
        }

        // Arabanın gövdesine göre kamera: üstte biraz boşluk, altta egzoz alevi için daha fazla yer
        Bounds b = body.bounds;
        float viewW = b.size.x * 2.2f;
        float viewH = b.size.y * 1.9f;
        float centerY = b.center.y - b.size.y * 0.3f;

        const int texHeight = 512;
        int texWidth = Mathf.Max(64, Mathf.RoundToInt(texHeight * viewW / viewH));
        stageTexture = new RenderTexture(texWidth, texHeight, 16, RenderTextureFormat.ARGB32);
        stageTexture.Create();

        var camGo = new GameObject("CarPreviewCamera");
        camGo.transform.SetParent(holder.transform, false);
        camGo.transform.position = new Vector3(b.center.x, centerY, b.center.z - 50f);
        stageCamera = camGo.AddComponent<Camera>();
        stageCamera.orthographic = true;
        stageCamera.orthographicSize = viewH * 0.5f;
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        stageCamera.nearClipPlane = 0.1f;
        stageCamera.farClipPlane = 200f;
        stageCamera.targetTexture = stageTexture;
        stageCamera.depth = -100;

        // Arayüzde: gövde genişliği eski görseldeki araba genişliğiyle aynı
        float uiPerWorld = carWidthUI / b.size.x;
        carView.texture = stageTexture;
        carView.rectTransform.sizeDelta = new Vector2(viewW * uiPerWorld, viewH * uiPerWorld);
        Vector2 carCenterUI = new Vector2(0f, -tileHeight * 0.15f);
        carView.rectTransform.anchoredPosition = carCenterUI + new Vector2(0f, (centerY - b.center.y) * uiPerWorld);
        carView.gameObject.SetActive(true);
        carView.transform.SetAsLastSibling();
        return true;
    }

    void ClearStage()
    {
        if (stageCar != null) Destroy(stageCar);
        stageCar = null;
        stageCamera = null;
        if (stageTexture != null) { stageTexture.Release(); Destroy(stageTexture); stageTexture = null; }
        if (carView != null) { carView.texture = null; carView.gameObject.SetActive(false); }
    }

    void OnDisable() => ClearStage();

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

        // Araba önizlemesinde yolun ortasında duran araba
        var car = NewImage("Car", viewport.rectTransform, Color.white);
        car.raycastTarget = false;
        car.preserveAspect = true;
        car.rectTransform.anchorMin = car.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panel.carImage = car;

        // Prefabdan çizilen canlı araba görüntüsü
        var viewGo = new GameObject("CarLive", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        viewGo.layer = viewport.gameObject.layer;
        viewGo.transform.SetParent(viewport.rectTransform, false);
        panel.carView = viewGo.GetComponent<RawImage>();
        panel.carView.raycastTarget = false;
        panel.carView.rectTransform.anchorMin = panel.carView.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        viewGo.SetActive(false);

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
    private Image carImage;

    void Open(Sprite roadSprite, Sprite carSprite, float carWidthRatio)
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

        carImage.gameObject.SetActive(carSprite != null);
        if (carSprite != null)
        {
            carImage.sprite = carSprite;
            float carWidth = width * carWidthRatio;
            carImage.rectTransform.sizeDelta = new Vector2(carWidth, carWidth * carSprite.rect.height / carSprite.rect.width);
            carImage.rectTransform.anchoredPosition = new Vector2(0f, -tileHeight * 0.15f);
            carImage.transform.SetAsLastSibling();
        }

        // Prefab varsa araba onun canlı görüntüsüyle (egzoz alevi dahil) gösterilir; düz görsel gizlenir
        if (carSprite != null && ShowLiveCar(width * carWidthRatio)) carImage.gameObject.SetActive(false);
        else ClearStage();

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
