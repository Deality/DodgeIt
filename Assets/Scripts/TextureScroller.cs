using UnityEngine;
using UnityEngine.UI; // UI bileşenleri için gerekli

public class TextureScroller : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Yatay kayma hızı (Sağ/Sol).")]
    public float scrollSpeedX = 0f;

    [Tooltip("Dikey kayma hızı (Yukarı/Aşağı). Aşağı akması için negatif değer girin (Örn: -0.5).")]
    public float scrollSpeedY = -0.5f;

    // Kullanılan bileşenleri saklamak için
    private RawImage _rawImage;
    private Renderer _renderer;

    void Awake()
    {
        // Objenin üzerindeki bileşenleri kontrol et
        _rawImage = GetComponent<RawImage>();
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // Zamanla ne kadar kayacağını hesapla
        float x = scrollSpeedX * Time.deltaTime;
        float y = scrollSpeedY * Time.deltaTime;

        // 1. Eğer obje bir UI (RawImage) ise:
        if (_rawImage != null)
        {
            Rect uv = _rawImage.uvRect;
            uv.x += x;
            uv.y += y;
            _rawImage.uvRect = uv;
        }
        // 2. Eğer obje bir 3D Obje (Mesh Renderer / Quad) ise:
        else if (_renderer != null)
        {
            // Materyalin offset değerini kaydır
            Vector2 offset = _renderer.material.mainTextureOffset;
            offset.x += x;
            offset.y += y;
            _renderer.material.mainTextureOffset = offset;
        }
    }
}