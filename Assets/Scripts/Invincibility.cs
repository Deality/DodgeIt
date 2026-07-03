using UnityEngine;

public class Invincibility : MonoBehaviour
{
    [Header("Ayarlar")]
    public float duration = 5.0f;

    [Header("Animation Settings - Büyüme (Pulse)")]
    public bool enableScaling = true;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.15f;

    [Header("Görsel Efektler")]
    public GameObject collectEffectPrefab;

    private Vector3 initialScale;
    private bool isCollected = false;

    void Start()
    {
        initialScale = transform.localScale;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void Update()
    {
        if (isCollected) return;

        if (enableScaling)
        {
            float scaleFactor = 1 + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            transform.localScale = initialScale * scaleFactor;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            float camHeight = 2f * cam.orthographicSize;
            float camBottomY = cam.transform.position.y - (camHeight / 2f);

            if (transform.position.y < camBottomY - 2f)
                Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        // 🔥 GÜNCELLEME: Alt nesne ve sensör çarpmalarını tamamen kapsayan kurşun geçirmez oyuncu tespiti
        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<CarController2D>() != null;

        if (isPlayer)
        {
            isCollected = true;

            if (GameManager.instance != null)
            {
                GameManager.instance.ActivateInvincibility(duration);
                Debug.Log("🛡️ Görünmezlik toplandı!");

                // 🔥 Statik bağımsız tampon sistemini kullanarak Kalkan ilerlemesini ekle!
                MissionsManager.AddGameplayProgress(MissionType.CollectShield, 1);
            }

            if (AudioManager.instance != null && AudioManager.instance.powerUpSound != null)
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.powerUpSound);
            }

            if (collectEffectPrefab != null)
            {
                GameObject effectInstance = Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effectInstance, 2f);
            }

            Destroy(gameObject);
        }
    }
}