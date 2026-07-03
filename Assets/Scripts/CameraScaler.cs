using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    [Header("Ayarlar")]
    public bool enableScaler = true; // Ana menüde kapatmak istersen tiki kaldır

    [Tooltip("Tasarım yaptığın ekran çözünürlüğü (Örn: 1080x1920)")]
    public Vector2 referenceResolution = new Vector2(1080f, 1920f);

    [Tooltip("0 = Genişliği Koru (Yanlar kesilmez, Alt/Üst uzar)\n1 = Yüksekliği Koru (Alt/Üst sabit, Yanlar kesilir)\n0.5 = Dengeli")]
    [Range(0f, 1f)]
    public float matchWidthOrHeight = 0f; // Varsayılan 0: Tamamen genişliğe odaklan

    private float initialSize = 5f;

    void Start()
    {
        if (!enableScaler) return;

        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        // Başlangıç size değerini al (Eğer 0 gelirse 5 yap)
        if (cam.orthographicSize > 0)
            initialSize = cam.orthographicSize;

        UpdateCameraSize(cam);
    }

    void UpdateCameraSize(Camera cam)
    {
        float targetAspectRatio = referenceResolution.x / referenceResolution.y;
        float currentAspectRatio = (float)Screen.width / (float)Screen.height;

        // Yüksekliği koruyan boyut (Unity'nin varsayılanı)
        float heightBasedSize = initialSize;

        // Genişliği koruyan boyut (Bizim hesapladığımız)
        float widthBasedSize = initialSize * (targetAspectRatio / currentAspectRatio);

        // İkisi arasında seçilen değere göre (Slider) geçiş yap
        // 0 seçilirse widthBasedSize (Yanlar tam oturur)
        // 1 seçilirse heightBasedSize (Alt/Üst tam oturur)
        cam.orthographicSize = Mathf.Lerp(widthBasedSize, heightBasedSize, matchWidthOrHeight);
    }
}