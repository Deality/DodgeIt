using UnityEngine;
using System.Collections;

public class NotificationBadgeAnimate : MonoBehaviour
{
    [Header("Animasyon Ayarları")]
    [Tooltip("Sallanma hareketinin hızı (Ne kadar hızlı sağa sola gidecek).")]
    public float shakeSpeed = 20f;

    [Tooltip("Sallanma açısının büyüklüğü (Ne kadar uzağa sallanacak).")]
    public float shakeMagnitude = 15f; // Derece cinsinden

    [Tooltip("Sallanma animasyonunun süresi (Kaç saniye sürecek).")]
    public float shakeDuration = 0.5f;

    [Header("Zamanlama Ayarları")]
    [Tooltip("İki sallanma hareketi arasındaki bekleme süresi (Saniye).")]
    public float pauseDuration = 3f;

    private RectTransform rectTransform;
    private Coroutine animationCoroutine;
    private Quaternion originalRotation;
    private bool isPivotSet = false; // Tek seferlik pivot ayar kontrolü

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // 🔥 GÜNCELLEME: Awake içinde pivot değiştirme kaldırıldı. 
        // Çünkü panel başlangıçta kapalıysa boyutlar sıfır (0) gelir ve yer kaymasına sebep olur.
    }

    void OnEnable()
    {
        if (rectTransform != null)
        {
            if (isPivotSet)
            {
                rectTransform.localRotation = originalRotation; // Zaten ayarlıysa rotasyonu sıfırla
            }
            animationCoroutine = StartCoroutine(PeriodicShakeRoutine());
        }
    }

    void OnDisable()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            if (isPivotSet && rectTransform != null)
            {
                rectTransform.localRotation = originalRotation; // Rotasyonu düzelt
            }
        }
    }

    // Pivot değiştirirken RectTransform kaymasını önleyen güvenli yardımcı fonksiyon
    void SetPivot(RectTransform rect, Vector2 pivot)
    {
        if (rect == null) return;
        Vector2 size = rect.rect.size;

        // Eğer boyut hala sıfırsa sizeDelta'yı yedek olarak kullan
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rect.sizeDelta;
        }

        Vector2 deltaPivot = rect.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x * rect.localScale.x, deltaPivot.y * size.y * rect.localScale.y);
        rect.pivot = pivot;
        rect.localPosition -= deltaPosition;
    }

    // 🔥 Sonsuz Döngü: Bekle -> Sallan (Sönerek Dur) -> Bekle...
    IEnumerator PeriodicShakeRoutine()
    {
        // 🔥 ÇÖZÜM: Panel ilk açıldığında Rect hesaplamalarının tamamlanması ve 
        // boyutların (Size) sıfırdan büyük olması için tam 1 kare bekliyoruz.
        // Bu sayede sekmeye ilk tıklandığında konum asla kaymaz!
        if (!isPivotSet && rectTransform != null)
        {
            yield return null; // Bir kare bekle (Canvas Layout Pass)
            SetPivot(rectTransform, new Vector2(0.5f, 0f));
            originalRotation = rectTransform.localRotation;
            isPivotSet = true;
        }

        while (true)
        {
            // --- 1. AŞAMA: Bekleme Süresi ---
            yield return new WaitForSeconds(pauseDuration);

            // --- 2. AŞAMA: Sönümlü Sallanma (Damped Swing) ---
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;

                // Sönümleme çarpanı (1'den 0'a doğru yumuşakça sönümlenir)
                float decay = Mathf.SmoothStep(1f, 0f, t);

                // elapsed sıfırdan başladığı için animasyon her zaman pürüzsüz başlar ve pürüzsüzce sıfırda sönerek biter
                float angle = Mathf.Sin(elapsed * shakeSpeed) * shakeMagnitude * decay;

                rectTransform.localRotation = Quaternion.Euler(0, 0, angle);

                yield return null; // Bir sonraki kareye kadar bekle
            }

            // Animasyon tamamen bittiğinde tam sıfır derecede durduğundan emin oluyoruz (Sıfır Işınlanma)
            rectTransform.localRotation = originalRotation;
        }
    }
}