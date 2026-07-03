using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // İstediğin yerden erişebilmek için static yapabiliriz
    public static CameraShake instance;

    private Vector3 originalPos;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        originalPos = transform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Rastgele bir x ve y pozisyonu belirle
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Kamerayı titret
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            // UnscaledDeltaTime kullanıyoruz çünkü kaza anında zamanı yavaşlatabiliriz
            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        // Titreme bitince eski yerine döndür
        transform.localPosition = originalPos;
    }
}