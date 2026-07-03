using UnityEngine;
using System.Collections; // Bu kütüphane şart!

public class HighSpeedEffect : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Hız çizgilerinin görünmesi için gereken minimum oyun hızı.")]
    public float activationSpeed = 20f;

    [Tooltip("Efektin belirme süresi (saniye).")]
    public float fadeInDuration = 0.5f;

    [Tooltip("Efektin kaybolma süresi (saniye).")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Efektin maksimum opaklığı (0-1 arası).")]
    [Range(0f, 1f)]
    public float maxOpacity = 1f;

    [Header("Görsel Referansı")]
    [Tooltip("Üzerinde Particle System olan obje (Bu scriptin olduğu obje olabilir).")]
    public GameObject speedLinesObject;

    private bool isActive = false;
    private ParticleSystem ps;
    private Coroutine currentFadeCoroutine;
    private Color baseColor; // Orijinal rengi saklamak için

    void Start()
    {
        // Eğer referans verilmemişse, bu objenin kendisindeki Particle System'i al
        if (speedLinesObject == null) speedLinesObject = this.gameObject;

        ps = speedLinesObject.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            // Orijinal rengi al
            baseColor = ps.main.startColor.color;

            // Başlangıçta durdur, temizle ve rengi şeffaf yap
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            SetParticleAlpha(0f);

            // Loop açık olsun
            var main = ps.main;
            main.loop = true;
        }
        else
        {
            Debug.LogWarning("HighSpeedEffect: Particle System bulunamadı!");
        }
    }

    void Update()
    {
        if (ps == null) return;

        // Mevcut hızı al
        float currentSpeed = 0f;
        if (ObstacleManager.instance != null)
        {
            currentSpeed = ObstacleManager.scrollSpeed;
        }

        // Hız Sınırı Aşıldıysa -> FADE IN BAŞLAT
        if (currentSpeed >= activationSpeed)
        {
            if (!isActive)
            {
                // Eğer durmuşsa başlat
                if (!ps.isPlaying) ps.Play();

                StartFade(true); // Görünür yap
                isActive = true;
            }
        }
        // Hız Sınırının Altındaysa -> FADE OUT BAŞLAT
        else
        {
            if (isActive)
            {
                StartFade(false); // Gizle
                isActive = false;
            }
        }
    }

    private void StartFade(bool fadeIn)
    {
        if (currentFadeCoroutine != null) StopCoroutine(currentFadeCoroutine);
        currentFadeCoroutine = StartCoroutine(FadeEffect(fadeIn));
    }

    private IEnumerator FadeEffect(bool fadeIn)
    {
        float targetAlpha = fadeIn ? maxOpacity : 0f;
        float startAlpha = ps.main.startColor.color.a;
        float duration = fadeIn ? fadeInDuration : fadeOutDuration;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);

            // Her karede Alpha'yı güncelle
            SetParticleAlpha(newAlpha);

            yield return null;
        }

        // Bitince tam değere eşitle
        SetParticleAlpha(targetAlpha);

        // Eğer kaybolduysa sistemi tamamen durdur (Performans için)
        if (!fadeIn)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        currentFadeCoroutine = null;
    }

    // Particle System rengini güncelleyen yardımcı fonksiyon
    private void SetParticleAlpha(float alpha)
    {
        if (ps == null) return;

        var main = ps.main;
        Color newColor = baseColor;
        newColor.a = alpha;
        main.startColor = newColor;
    }
}