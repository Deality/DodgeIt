using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private Camera mainCam;
    private Collider2D myCollider;

    [Header("Hareket Ayarları")]
    [Tooltip("Bu araç normalden ne kadar daha hızlı gitsin? (Maganda için 5 veya 10 yap)")]
    public float bonusSpeed = 0f;

    [Header("Yalpalama (Sway) Ayarları")]
    [Tooltip("Bu araç sağa sola yalpallasın mı? (Normal araçlar için AÇ, Maganda için KAPAT)")]
    public bool enableSway = true;

    [Tooltip("Yalpalama hızı.")]
    public float swaySpeed = 2.0f;

    [Tooltip("Yalpalama genişliği.")]
    public float swayAmount = 0.5f;

    private float initialX;
    private float randomOffset;

    void Start()
    {
        mainCam = Camera.main;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 10;

        myCollider = GetComponent<Collider2D>();

        // Başlangıçtaki X konumunu kaydet
        initialX = transform.position.x;

        // Hepsi aynı anda dans etmesin diye rastgele zaman farkı
        randomOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        // 1. AŞAĞI HAREKET (Normal Hız + Varsa Bonus Hız)
        float totalSpeed = ObstacleManager.scrollSpeed + bonusSpeed;

        // Y ekseninde aşağı in
        float newY = transform.position.y - (totalSpeed * Time.deltaTime);

        // 2. YATAY HAREKET (Sway)
        float newX = transform.position.x;

        if (enableSway)
        {
            // Sinüs dalgası ile yumuşak sağ-sol hareketi (Sarhoş modu)
            newX = initialX + Mathf.Sin((Time.time + randomOffset) * swaySpeed) * swayAmount;
        }
        else
        {
            // Sway kapalıysa (Maganda), şeridinde dümdüz (ip gibi) kalsın
            newX = initialX;
        }

        // Yeni pozisyonu uygula
        transform.position = new Vector3(newX, newY, transform.position.z);

        // --- EKRAN DIŞI YOK ETME ---
        if (mainCam != null)
        {
            Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);
            // Ekranın altına indiğinde yok et
            if (viewportPos.y < -0.5f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Sensör (Puan Alanı) Kontrolü
        if (other.GetComponent<NearMissDetector>() != null)
        {
            return;
        }

        // Oyuncu ile Çarpışma
        if (other.CompareTag("Player"))
        {
            Vector3 crashPosition = transform.position;
            if (myCollider != null)
            {
                crashPosition = myCollider.ClosestPoint(other.transform.position);
            }

            // 🔥 GÜNCELLENDİ: Görünmezlik, Boost veya Yavaşlama Koruması Kontrolü
            if (GameManager.instance != null)
            {
                bool isGrace = (CarController2D.instance != null && CarController2D.instance.isBoostGracePeriod);

                if (GameManager.instance.isBoosting || isGrace)
                {
                    Debug.Log("🔥 Öfke Modu veya yavaşlama koruması aktif! Engel parçalandı.");
                    GameManager.instance.TriggerBoostCrash(crashPosition);
                    Destroy(gameObject);
                    return;
                }
                else if (GameManager.instance.IsInvincible)
                {
                    Debug.Log("🛡️ Görünmezlik aktif! Engel yok edildi.");
                    GameManager.instance.TriggerInvincibleCrash(crashPosition);
                    Destroy(gameObject);
                    return;
                }
            }

            // Kaza
            Debug.Log("💀 Kaza yapıldı!");

            if (GameManager.instance != null)
            {
                GameManager.instance.TriggerCrash(crashPosition);
            }
        }
    }
}