using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ExhaustEffect : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;

    [Header("Genel Boyut Çarpanı")]
    public float sizeMultiplier = 1.0f;

    [Header("Normal Durum Ayarları")]
    public float normalStartSize = 0.2f;   // 🔥 GÜNCELLENDİ: Daha ince olması için küçültüldü (0.3 -> 0.2)
    public float normalEmissionRate = 20f;

    [Header("Boost (Hızlanma) Ayarları")]
    public float boostStartSize = 0.4f;    // 🔥 GÜNCELLENDİ: Boostta aşırı şişmemesi için küçültüldü (0.6 -> 0.4)
    public float boostEmissionRate = 50f;

    [Header("Rüzgar/Akış Ayarı")]
    [Tooltip("Dumanın aşağı (geriye) doğru ne kadar hızlı akacağını belirler.")]
    public float windForceMultiplier = 2.0f; // 🔥 GÜNCELLENDİ: Daha fazla uzaması için artırıldı (1.5 -> 2.0)

    [Header("Geçiş Ayarları")]
    public float transitionSpeed = 5.0f;

    // Anlık değerler
    private float currentSize;
    private float currentEmission;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        main = ps.main;
        emission = ps.emission;
        velocityModule = ps.velocityOverLifetime;

        // 🔥 GÜNCELLENDİ: Dumanın sağa sola yayılmasını engelle (Koni açısını İYİCE daralt)
        var shape = ps.shape;
        shape.angle = 0.5f; // Neredeyse düz çizgi (2f -> 0.5f)
        shape.radius = 0.05f; // Çıkış noktası çok dar (0.1f -> 0.05f)

        // Simülasyonu "Local" yaparak dumanın arabayı takip etmesini sağlıyoruz.
        // Araba sağa/sola gittiğinde duman kütlesi de onunla beraber gider.
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        // Velocity modülünü aç
        velocityModule.enabled = true;
        // Hız etkisini de Local yapıyoruz ki arabanın arkasına doğru (Local Y) gitsin
        velocityModule.space = ParticleSystemSimulationSpace.Local;

        currentSize = normalStartSize;
        currentEmission = normalEmissionRate;
    }

    void Update()
    {
        if (GameManager.instance == null || ObstacleManager.instance == null) return;

        bool isBoosting = GameManager.instance.isBoosting;

        // Hedef değerler
        float targetSize = isBoosting ? boostStartSize : normalStartSize;
        float targetEmission = isBoosting ? boostEmissionRate : normalEmissionRate;

        // Yumuşak geçiş
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * transitionSpeed);
        currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * transitionSpeed);

        // Değerleri Uygula
        main.startSize = currentSize * sizeMultiplier;

        var rate = emission.rateOverTime;
        rate.constant = currentEmission;
        emission.rateOverTime = rate;

        // 🔥 Rüzgar Etkisi (Hız İzi)
        // Oyunun o anki akış hızını alıyoruz.
        float currentGameSpeed = ObstacleManager.scrollSpeed;

        // Dumanın Y hızını, oyun hızının tersine (-) eşitliyoruz.
        // Local olduğu için bu her zaman "arabanın arkası" yönünde olur.
        velocityModule.y = new ParticleSystem.MinMaxCurve(-currentGameSpeed * windForceMultiplier);

        // X hızını 0 yapalım ki sağa sola savrulmasın
        velocityModule.x = new ParticleSystem.MinMaxCurve(0);
    }
}