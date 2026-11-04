using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;

// Akira fontunda ₺, € gibi para birimi işaretleri yok. Mağaza fiyatları cihazın kendi para biriminde geldiği için
// cihazın sistem fontundan dinamik bir TMP yedek fontu oluşturup tüm yazılara yedek olarak ekliyoruz.
public static class CurrencyFontFallback
{
    static readonly string[] FontFiles =
    {
        "/system/fonts/Roboto-Regular.ttf",
        "/system/fonts/RobotoStatic-Regular.ttf",
        "/system/fonts/NotoSans-Regular.ttf",
        "/system/fonts/DroidSans.ttf",
    };

    static readonly string[] FontFamilies = { "Roboto", "Helvetica Neue", "Helvetica", "Arial", "Segoe UI" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        var fallback = Create();
        if (fallback == null)
        {
            Debug.LogWarning("CurrencyFontFallback: sistem fontu bulunamadı, para birimi işaretleri görünmeyebilir.");
            return;
        }
        fallback.name = "System Currency Fallback";

        var list = TMP_Settings.fallbackFontAssets;
        if (list == null) return;
        list.RemoveAll(f => f == null || f.name == fallback.name);
        list.Add(fallback);
        // Editörde TMP Settings asset'i Play'den sonra da bellekte kalır; geçici fontu listeye kaydettirmeyelim.
        Application.quitting += () => list.Remove(fallback);
    }

    static TMP_FontAsset Create()
    {
        foreach (var path in FontFiles)
        {
            if (!File.Exists(path)) continue;
            var asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512);
            if (HasLira(asset)) return asset;
        }

        // Üreticiler dosya adlarını değiştirebiliyor; bilinen adlar yoksa sistemdeki diğer Sans fontlarını dene.
        if (Directory.Exists("/system/fonts"))
        {
            foreach (var path in Directory.GetFiles("/system/fonts", "*Sans*-Regular.ttf"))
            {
                var asset = TMP_FontAsset.CreateFontAsset(path, 0, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512);
                if (HasLira(asset)) return asset;
            }
        }

        var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
        foreach (var family in FontFamilies)
        {
            if (!installed.Contains(family)) continue;
            var asset = TMP_FontAsset.CreateFontAsset(family, "Regular");
            if (HasLira(asset)) return asset;
        }
        return null;
    }

    static bool HasLira(TMP_FontAsset asset)
    {
        return asset != null && asset.HasCharacter('₺', false, true);
    }
}
