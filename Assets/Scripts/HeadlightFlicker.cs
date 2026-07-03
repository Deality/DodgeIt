using UnityEngine;
using System.Collections;

public class HeadlightFlicker : MonoBehaviour
{
    [Header("Selektör Ayarları")]
    [Tooltip("Yanıp sönme hızı. (Saniyede kaç kere?)")]
    public float flickerRate = 2f;

    [Tooltip("Farların rastgele zamanlarda yanıp sönmesini sağlar.")]
    public bool randomizeStart = true;

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Eğer rastgele başlangıç seçildiyse, her araba farklı zamanda yanıp sönsün diye bekle
        if (randomizeStart)
        {
            StartCoroutine(StartWithDelay());
        }
        else
        {
            StartCoroutine(FlickerRoutine());
        }
    }

    IEnumerator StartWithDelay()
    {
        yield return new WaitForSeconds(Random.Range(0f, 1f));
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Açık-Kapalı döngüsü
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            // Bekleme süresi (1 / Hız)
            yield return new WaitForSeconds(1f / flickerRate);
        }
    }
}