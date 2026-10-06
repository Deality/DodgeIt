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
            // Şerit eğik durduğu için butonun iki yanından biraz daha uzaktan başlayıp bitmeli
            float travel = rect.rect.width * 0.5f + shine.rect.width + shine.rect.height * 0.25f;
            shine.gameObject.SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(duration, 0.01f);
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, Mathf.Clamp01(t)), 0f);
                yield return null;
            }

            shine.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
