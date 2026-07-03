using UnityEngine;

public class FastMover : MonoBehaviour
{
    [Tooltip("Normal oyun hızına eklenecek ekstra hız.")]
    public float bonusSpeed = 5f; // Maganda daha hızlı aksın

    private Camera mainCam;
    private Collider2D myCollider;

    void Start()
    {
        mainCam = Camera.main;
        myCollider = GetComponent<Collider2D>();

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = 11; // Diğer arabaların hafif önüne geçsin
    }

    void Update()
    {
        // Normal Hız + Bonus Hız ile dümdüz aşağı
        float currentSpeed = ObstacleManager.scrollSpeed + bonusSpeed;

        transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World);

        // Ekran dışı yok etme
        if (mainCam != null)
        {
            Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);
            if (viewportPos.y < -0.5f)
            {
                Destroy(gameObject);
            }
        }
    }

    // Çarpışma mantığı Obstacle.cs ile aynı (Kopyası)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<NearMissDetector>() != null) return;

        if (other.CompareTag("Player"))
        {
            // 🔥 GÜNCELLENDİ: Görünmezlik, Boost veya Yavaşlama Koruması Kontrolü
            if (GameManager.instance != null)
            {
                bool isGrace = (CarController2D.instance != null && CarController2D.instance.isBoostGracePeriod);

                if (GameManager.instance.isBoosting || isGrace)
                {
                    GameManager.instance.TriggerBoostCrash(transform.position);
                    Destroy(gameObject);
                    return;
                }
                else if (GameManager.instance.IsInvincible)
                {
                    GameManager.instance.TriggerInvincibleCrash(transform.position);
                    Destroy(gameObject);
                    return;
                }
            }

            if (GameManager.instance != null)
            {
                GameManager.instance.TriggerCrash(transform.position);
            }
        }
    }
}