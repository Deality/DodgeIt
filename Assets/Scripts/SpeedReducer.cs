using UnityEngine;

public class SpeedReducer : MonoBehaviour
{
    [Header("Animation Settings - Büyüme (Pulse)")]
    public bool enableScaling = true;
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.15f;

    private Vector3 initialScale;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        if (enableScaling)
        {
            float scaleFactor = 1 + (Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
            transform.localScale = initialScale * scaleFactor;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 🔥 GÜNCELLEME: Alt nesne ve sensör çarpmalarını tamamen kapsayan kurşun geçirmez oyuncu tespiti
        bool isPlayer = other.CompareTag("Player") || other.GetComponentInParent<CarController2D>() != null;

        if (isPlayer)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(AudioManager.instance.powerUpSound);

            if (ObstacleManager.instance != null)
            {
                ObstacleManager.instance.ActivateSpeedReduction();
                Debug.Log("Hız Düşürücü toplandı! Spawn aralığı genişletildi.");

                // 🔥 Statik bağımsız tampon sistemini kullanarak Yavaşlatıcı ilerlemesini ekle!
                MissionsManager.AddGameplayProgress(MissionType.CollectSlowMotion, 1);
            }
            else
            {
                Debug.LogError("Sahne üzerinde aktif bir ObstacleManager instance'ı bulunamadı!");
            }

            Destroy(gameObject);
        }
    }
}