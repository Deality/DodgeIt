using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Efekt Ayarları")]
    [Tooltip("Basıldığında buton ne kadar küçülsün? (0.9 = %90 boyuta iner)")]
    public float pressedScale = 0.9f;
    [Tooltip("Animasyon hızı.")]
    public float animationDuration = 0.1f;

    [Header("Ses ve Titreşim")]
    public bool playSound = true;
    public bool useHaptic = false;

    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isInitialized = false;

    void Awake()
    {
        // Start yerine Awake kullanıyoruz ki objeler aktifleşmeden gerçek boyutunu bilelim
        originalScale = transform.localScale;

        // Eğer Unity'de scale yanlışlıkla 0 olduysa, varsayılan olarak 1,1,1 yap
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
            transform.localScale = Vector3.one;
        }

        isInitialized = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartScaleAnimation(pressedScale);

        if (playSound && AudioManager.instance != null)
        {
            AudioManager.instance.PlayButtonSound();
        }

        if (useHaptic)
        {
            // Handheld.Vibrate(); 
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StartScaleAnimation(1.0f);
    }

    void StartScaleAnimation(float targetScaleMultiplier)
    {
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(targetScaleMultiplier));
    }

    IEnumerator ScaleRoutine(float targetMultiplier)
    {
        if (!isInitialized) yield break;

        Vector3 targetSize = originalScale * targetMultiplier;
        float timer = 0f;
        Vector3 startSize = transform.localScale;

        while (timer < animationDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / animationDuration;

            t = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(startSize, targetSize, t);
            yield return null;
        }

        transform.localScale = targetSize;
    }

    void OnDisable()
    {
        if (isInitialized)
        {
            transform.localScale = originalScale;
        }
    }
}