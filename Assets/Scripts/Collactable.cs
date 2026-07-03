using UnityEngine;

public class Collectable : MonoBehaviour
{
    [Header("Coin Değeri Ayarları")]
    [Tooltip("Bu coinden çıkabilecek EN AZ miktar")]
    public int minGemValue = 5;
    [Tooltip("Bu coinden çıkabilecek EN ÇOK miktar")]
    public int maxGemValue = 10;

    [Header("Animation Settings - Dönme")]
    public bool enableRotation = true;
    public float rotationSpeed = 180f; // Dönme hızı

    [Header("Animation Settings - Yüzme (Bobbing)")]
    public bool enableBobbing = true;
    public float bobSpeed = 2f;    // Yüzme hızı
    public float bobAmount = 0.1f; // Yüzme yüksekliği

    [Header("Animation Settings - Büyüme (Pulse)")]
    public bool enableScaling = true;
    public float pulseSpeed = 3f;  // Büyüme hızı
    public float pulseAmount = 0.15f; // Ne kadar büyüyecek (Örn: %15)

    [Header("Görsel Efektler")]
    [Tooltip("Coin toplandığında çıkacak parçacık efekti prefabı.")]
    public GameObject collectEffectPrefab;

    [Tooltip("Coin toplandığında çıkacak kayan yazı (+1 vb.) prefabı.")]
    public GameObject floatingTextPrefab;

    private Vector3 initialScale;
    private bool isCollected = false; // Çifte toplamayı önlemek için bayrak

    void Start()
    {
        // Başlangıç büyüklüğünü kaydet
        initialScale = transform.localScale;
    }

    void Update()
    {
        // Eğer toplandıysa hareket veya animasyon yapma
        if (isCollected) return;

        // 1. AŞAĞI KAYMA HAREKETİ
        if (ObstacleManager.instance != null)
        {
            float currentSpeed = ObstacleManager.scrollSpeed;
            transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World);
        }

        // 2. YÜZME (BOBBING) HAREKETİ
        if (enableBobbing)
        {
            float newY = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            transform.position += new Vector3(0, newY * Time.deltaTime, 0);
        }

        // 3. DÖNME ANİMASYONU
        if (enableRotation)
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }

        // 4. BÜYÜYÜP KÜÇÜLME (PULSE)
        if (enableScaling)
        {
            float scaleFactor = 1 + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            transform.localScale = initialScale * scaleFactor;
        }

        // 5. KAMERA DIŞI YOK ETME
        Camera cam = Camera.main;
        if (cam != null)
        {
            float camHeight = 2f * cam.orthographicSize;
            float camBottomY = cam.transform.position.y - (camHeight / 2f);

            if (transform.position.y < camBottomY - 2f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        // 🔥 GÜNCELLEME: Alt nesne ve sensör çarpmalarını tamamen kapsayan kurşun geçirmez oyuncu tespiti
        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<CarController2D>() != null;

        if (isPlayer)
        {
            isCollected = true;

            // RASTGELE COIN DEĞERİ HESAPLAMA
            int earnedValue = Random.Range(minGemValue, maxGemValue + 1);

            if (GameManager.instance != null)
            {
                // Rastgele değeri kalıcı para birimine ekle
                GameManager.instance.AddGems(earnedValue);
            }

            // YAZIYI GÜNCELLEME (+kaç ise onu yazdır)
            if (floatingTextPrefab != null)
            {
                Vector3 spawnPos = transform.position + new Vector3(0, 1.5f, -2f);
                GameObject textObj = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);

                FloatingText ft = textObj.GetComponent<FloatingText>();
                if (ft != null)
                {
                    ft.SetText("+" + earnedValue); // Rastgele çıkan değeri ekrana yazar
                }
            }
            else if (GameManager.instance != null)
            {
                // Özel prefab yoksa GameManager'ı kullan
                GameManager.instance.ShowFloatingText("+" + earnedValue, transform.position + new Vector3(0, 1.5f, -2f));
            }

            // SES EFEKTİ
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.coinSound);
            }

            // PARÇACIK EFEKTİ
            if (collectEffectPrefab != null)
            {
                GameObject effectInstance = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);

                Rigidbody2D rb = effectInstance.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = effectInstance.AddComponent<Rigidbody2D>();
                }

                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;

                Destroy(effectInstance, 2f);
            }

            // Objenin kendisini yok et
            Destroy(gameObject);
        }
    }
}