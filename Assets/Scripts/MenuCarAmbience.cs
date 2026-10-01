using UnityEngine;

// Ana menüde yukarıdan aşağı geçen polis arabasına eklenir: siren, araba ekranın ortasındayken en
// yüksek, kenarlara doğru azalır ve ekrandan çıkınca söner (AudioManager.SetSirenLevel).
public class MenuCarAmbience : MonoBehaviour
{
    [Header("Siren")]
    [Tooltip("Araba ekranın bu kadar (dünya birimi) dışına çıkınca siren tamamen susar.")]
    public float sirenOffscreenFade = 450f;
    [Tooltip("Araba ekranın alt/üst kenarındayken siren seviyesi (ekranın ortasında 1 olur) - geçip gitme hissi.")]
    [Range(0f, 1f)] public float sirenEdgeLevel = 0.5f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        AudioManager am = AudioManager.instance;
        if (am == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float half = cam.orthographicSize;
        float fromCenter = Mathf.Abs(transform.position.y - cam.transform.position.y);
        float level;
        if (fromCenter <= half)
        {
            // Ekranda: ortada en yüksek, kenarlara doğru azalır
            level = Mathf.Lerp(1f, sirenEdgeLevel, fromCenter / half);
        }
        else
        {
            // Ekran dışında: kenar seviyesinden sessizliğe iner
            float outside = fromCenter - half;
            level = sirenEdgeLevel * (1f - Mathf.Clamp01(outside / Mathf.Max(sirenOffscreenFade, 1f)));
        }
        am.SetSirenLevel(level);
    }
}
