using UnityEngine;

// Oyunu cihaz ekranının desteklediği en yüksek yenileme hızında (60/90/120/144Hz) çalıştırır.
// Eskiden hedef her cihazda sabit 120'ydi; 90Hz ekranlarda oyun ekranın gösteremeyeceği kareleri
// üretmeye zorlanıyor, bu da takılmaya (bir kare tekrar, sonraki kare iki adım) yol açıyordu.
// Hedef artık oyun açılırken ekranın kendisinden okunuyor. Güç tasarrufu modunda sistemin ekranı
// düşürmesine özel bir şey yapılmıyor; bu, diğer oyunlarda da olduğu gibi sistemin kararı.
public static class RefreshRateManager
{
    private const int FallbackFrameRate = 60;

    // Ekranın desteklediği en yüksek yenileme hızı (Hz). Bulunamazsa 60.
    public static int TargetFrameRate { get; private set; } = FallbackFrameRate;

    // Oyun açılırken, ilk sahne yüklenmeden önce bir kez çalışır.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        TargetFrameRate = FindHighestRefreshRate();
        Apply();
    }

    // VSync kapalı; kare hızı ekranın en yüksek yenileme hızına sabitlenir.
    // Uygulama arka plandan dönünce de çağrılır (bazı cihazlar arka planda hedefi düşürüyor).
    public static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
    }

    private static int FindHighestRefreshRate()
    {
        double highest = 0;
        foreach (Resolution resolution in Screen.resolutions)
            highest = System.Math.Max(highest, resolution.refreshRateRatio.value);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android'de Screen.resolutions çoğu zaman yalnızca o anki modu döndürür; anlık çözünürlüğe de bak
        highest = System.Math.Max(highest, Screen.currentResolution.refreshRateRatio.value);
#endif

        int hz = (int)System.Math.Round(highest);
        return hz > 0 ? hz : FallbackFrameRate;
    }
}
