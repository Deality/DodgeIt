using UnityEngine;

// Yolda çıkan Drift Boost güçlendirmesi. Hareketi prefabdaki Scroller yapar (Kalkan/Yavaşlatıcı gibi).
public class DriftBoostPickup : MonoBehaviour
{
    [Header("Animation Settings - Büyüme (Pulse)")]
    [SerializeField] private bool enableScaling = true;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.15f;

    [Header("Görsel Efektler")]
    [SerializeField] private GameObject collectEffectPrefab;
    [Tooltip("Boş bırakılırsa AudioManager'daki coinRushSound (o da boşsa powerUpSound) çalınır.")]
    [SerializeField] private AudioClip pickupSound;

    private Vector3 initialScale;
    private bool isCollected = false;

    void Awake()
    {
        initialScale = transform.localScale;
    }

    // 🔥 HAVUZ NOTU: Start() havuzdan yeniden kullanımda çalışmaz, bu yüzden bayrak burada sıfırlanıyor.
    void OnEnable()
    {
        isCollected = false;
        transform.localScale = initialScale;
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
            float camBottomY = cam.transform.position.y - cam.orthographicSize;
            if (transform.position.y < camBottomY - 2f)
                PoolManager.Despawn(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;

        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<CarController2D>() != null;
        if (!isPlayer) return;

        isCollected = true;

        bool activated = DriftBoostManager.instance != null && DriftBoostManager.instance.TryActivate();

        if (activated)
        {
            if (AudioManager.instance != null)
            {
                AudioClip clip = pickupSound != null ? pickupSound
                    : AudioManager.instance.coinRushSound != null ? AudioManager.instance.coinRushSound
                    : AudioManager.instance.powerUpSound;
                if (clip != null) AudioManager.instance.PlaySFX(clip);
            }

            if (collectEffectPrefab != null)
            {
                GameObject effectInstance = PoolManager.Spawn(collectEffectPrefab, transform.position, Quaternion.identity);
                PoolManager.DespawnAfter(effectInstance, 2f);
            }
        }
        else if (DriftBoostManager.instance == null)
        {
            Debug.LogError("Sahnede DriftBoostManager yok! Drift Boost toplanamadı.");
        }

        PoolManager.Despawn(gameObject);
    }
}
