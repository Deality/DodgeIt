using UnityEngine;

public class ArabaHareketi : MonoBehaviour
{
    // === AYARLANABİLİR DEĞİŞKENLER ===

    [Header("Hız ve Sınırlar")]
    // Arabanın aşağı hareket hızı
    public float hareketHizi = 5f;

    // Arabanın ekranın altından çıktığı sınır (kaybolma noktası)
    public float altSinirY = -10f;

    // Kameranın üst kenarından ne kadar yukarıda (ekran dışında) spawn olsun
    public float spawnUzakligi = 1f;

    [Header("Şerit Koordinatları")]
    // Arabanın spawn olabileceği X koordinatları (şeritler)
    public float[] seritXKoordinatlari = { -2.0f, 0.0f, 2.0f };

    // === DAHİLİ DEĞİŞKENLER ===

    // Arabanın yeniden doğacağı (spawn olacağı) Y pozisyonu (Kod tarafından hesaplanır)
    private float ustSinirY;

    private void Start()
    {
        // Kamera ana (Main) değilse veya ortografik değilse uyarı verilebilir.
        if (Camera.main == null || !Camera.main.orthographic)
        {
            Debug.LogError("Lütfen sahnenizde 'MainCamera' etiketli bir Ortografik Kamera olduğundan emin olun.");
            return;
        }

        // Kamera boyutuna göre üst spawn sınırını otomatik hesapla:
        // Kameranın dikey yarım boyutu (orthographicSize) bize Y=0'dan üst kenara olan mesafeyi verir.
        // Bu değer genellikle üst kenar Y koordinatıdır (kamera Y=0'da ise).
        float kameraUstKenari = Camera.main.orthographicSize;

        // Yeniden doğma pozisyonu = Üst Kenar + Ekstra Güvenli Uzaklık
        ustSinirY = kameraUstKenari + spawnUzakligi;

        // Oyuna başlarken arabanın hemen yukarıda spawn olmasını sağlayalım.
        YenidenDog();
    }

    void Update()
    {
        // 1. Arabayı aşağı doğru hareket ettir
        transform.Translate(Vector3.down * hareketHizi * Time.deltaTime);

        // 2. Alt sınıra ulaşıp ulaşmadığını kontrol et
        if (transform.position.y < altSinirY)
        {
            // 3. Yeniden doğma ve şerit değiştirme işlemini yap
            YenidenDog();
        }
    }

    void YenidenDog()
    {
        // Rastgele bir şerit seç
        int rastgeleSeritIndex = Random.Range(0, seritXKoordinatlari.Length);
        float yeniX = seritXKoordinatlari[rastgeleSeritIndex];

        // Yeni pozisyonu belirle (üstSinirY değeri Start() metodunda hesaplandı)
        Vector3 yeniPozisyon = new Vector3(yeniX, ustSinirY, transform.position.z);

        // Arabayı yeni pozisyona anında taşı
        transform.position = yeniPozisyon;
    }
}