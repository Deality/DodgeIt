using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float fadeSpeed = 1.5f;
    public float destroyTime = 1.5f; // Varsayılan ömür

    [Header("Ses Efekti (Opsiyonel)")]
    public AudioClip spawnSound; // Eğer yazı çıkınca ses çalsın istersen

    // Objenin ve alt objelerinin tüm grafik bileşenlerini tutacağız
    private TextMeshPro[] textMeshes;
    private TextMeshProUGUI[] uiTexts;
    private SpriteRenderer[] spriteRenderers;
    private Image[] images;

    private float timer;
    private Coroutine destroyCoroutine;

    void Awake()
    {
        InitComponents();
    }

    void OnEnable()
    {
        timer = 0f;
        if (spawnSound != null)
            AudioSource.PlayClipAtPoint(spawnSound, transform.position);

        destroyCoroutine = StartCoroutine(DestroyAfter(destroyTime));
    }

    IEnumerator DestroyAfter(float t)
    {
        yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }

    // Call right after Instantiate to make this text match the streak duration
    public void SetLifetime(float duration)
    {
        float totalTravel = moveSpeed * destroyTime;
        destroyTime = duration;
        fadeSpeed   = 1f / duration;
        moveSpeed   = totalTravel / duration;

        if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
        destroyCoroutine = StartCoroutine(DestroyAfter(duration));
    }

    void InitComponents()
    {
        // Kendi üzerindeki ve içine koyduğun alt objelerdeki (Örn: Coin İkonu) tüm grafikleri bulur
        textMeshes = GetComponentsInChildren<TextMeshPro>();
        uiTexts = GetComponentsInChildren<TextMeshProUGUI>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        images = GetComponentsInChildren<Image>();
    }

    void Update()
    {
        // Yukarı doğru süzülme hareketi
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // Alpha (Solma) hesabı
        timer += Time.deltaTime * fadeSpeed;
        float newAlpha = Mathf.Lerp(1f, 0f, timer);

        // Bulduğu tüm metinlerin ve resimlerin alphasını (şeffaflığını) kısar
        foreach (var t in textMeshes) { Color c = t.color; c.a = newAlpha; t.color = c; }
        foreach (var t in uiTexts) { Color c = t.color; c.a = newAlpha; t.color = c; }
        foreach (var s in spriteRenderers) { Color c = s.color; c.a = newAlpha; s.color = c; }
        foreach (var i in images) { Color c = i.color; c.a = newAlpha; i.color = c; }
    }

    public void SetText(string text)
    {
        if (textMeshes == null) InitComponents();

        // Metin bileşenlerine değeri (+10 vb.) yazdırır
        foreach (var t in textMeshes) t.text = text;
        foreach (var t in uiTexts) t.text = text;
    }
}