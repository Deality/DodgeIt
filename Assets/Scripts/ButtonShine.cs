using System.Collections;
using UnityEngine;

// Butonun üzerinden arada bir soldan sağa geçen parlama (yansıma) şeridi.
// Şerit, butonun kendi görseliyle maskelenen bir alt objedir (butonda Mask bileşeni olmalı).
public class ButtonShine : MonoBehaviour
{
    [Tooltip("Buton üzerinde kayan parlama şeridi (maskelenmiş alt obje).")]
    public RectTransform shine;
    [Tooltip("İki parlama arasındaki bekleme süresi (saniye).")]
    public float interval = 3.5f;
    [Tooltip("Şeridin butonu baştan sona geçme süresi (saniye).")]
    public float duration = 0.7f;
    [Tooltip("Menü açıldıktan sonra ilk parlamaya kadar geçen süre (saniye).")]
    public float startDelay = 1.2f;
    [Tooltip("Şeridin ilerleme yönü. (1,0) = soldan sağa, (1,-1) = sol üstten sağ alta.")]
    public Vector2 direction = Vector2.right;
    [Tooltip("Şerit eğik duruyorsa butonun dışına tamamen çıkması için eklenen pay.")]
    public float extraTravel = 0f;

    private RectTransform rect;

    void OnEnable()
    {
        rect = (RectTransform)transform;
        if (shine != null) StartCoroutine(ShineLoop());
    }

    IEnumerator ShineLoop()
    {
        shine.gameObject.SetActive(false);
        // Gerçek zaman: menüde timeScale ne olursa olsun çalışsın
        yield return new WaitForSecondsRealtime(startDelay);

        while (true)
        {
            // Şerit butonun tamamen dışından başlayıp tamamen dışında biter
            Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            float travel = Mathf.Abs(dir.x) * rect.rect.width * 0.5f + Mathf.Abs(dir.y) * rect.rect.height * 0.5f
                           + shine.rect.width + extraTravel;
            shine.gameObject.SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(duration, 0.01f);
                shine.anchoredPosition = dir * Mathf.Lerp(-travel, travel, Mathf.Clamp01(t));
                yield return null;
            }

            shine.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
