using UnityEngine;

// Maganda (hızlı) aracın arkasında iki ince hız çizgisi bırakır; oyuncu onun diğer araçlardan
// daha hızlı geldiğini uzaktan fark edebilsin diye. ObstacleManager, maganda spawn ederken ekler.
public class SpeedStreaks : MonoBehaviour
{
    public Color streakColor = new Color(1f, 1f, 1f, 0.75f);
    [Tooltip("Çizgi uzunluğu, aracın boyuna oranla.")]
    public float lengthFactor = 0.9f;
    [Tooltip("Çizgi kalınlığı, aracın enine oranla.")]
    public float widthFactor = 0.08f;

    private TrailRenderer[] trails;
    private Obstacle obstacle;
    private float streakLength;

    private static Material sharedMaterial;

    public static void Attach(GameObject car)
    {
        if (car.GetComponent<SpeedStreaks>() == null) car.AddComponent<SpeedStreaks>();
    }

    void Awake()
    {
        obstacle = GetComponent<Obstacle>();

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return;

        if (sharedMaterial == null) sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        // Araç aşağı doğru aktığı için arkası ekranda üst taraf: çizgiler üst iki köşenin biraz içinden çıkar
        Bounds b = sr.bounds;
        streakLength = b.size.y * lengthFactor;
        float y = b.max.y - b.size.y * 0.12f;

        trails = new TrailRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            float x = b.center.x + (i == 0 ? -1f : 1f) * b.extents.x * 0.55f;

            var go = new GameObject("SpeedStreak");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x, y, transform.position.z);

            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = sharedMaterial;
            tr.startWidth = b.size.x * widthFactor;
            tr.endWidth = 0f;
            tr.startColor = streakColor;
            tr.endColor = new Color(streakColor.r, streakColor.g, streakColor.b, 0f);
            tr.numCapVertices = 2;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.sortingLayerID = sr.sortingLayerID;
            tr.sortingOrder = 9; // araçların (10-11) hemen altında
            trails[i] = tr;
        }
    }

    // Havuzdan yeniden kullanımda eski konumdan yenisine uzanan çizgi kalmasın
    void OnEnable()
    {
        if (trails == null) return;
        foreach (TrailRenderer tr in trails) tr.Clear();
    }

    void Update()
    {
        if (trails == null) return;

        // Çizgi boyu hızdan bağımsız sabit kalsın: süre = uzunluk / hız
        float speed = ObstacleManager.scrollSpeed + (obstacle != null ? obstacle.bonusSpeed : 0f);
        float time = streakLength / Mathf.Max(speed, 1f);
        foreach (TrailRenderer tr in trails) tr.time = time;
    }
}
