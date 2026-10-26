using UnityEngine;

// Güçlendirme yükseltmeleri. Powerups panelinden coin ile satın alınır; seviye PlayerPrefs'te tutulur.
// Kalkan ve Coin Rush: her seviye süreye saniye ekler. Hız düşürücü: her seviye hızı daha fazla düşürür (süre sabit).
// Boost artık yükseltilmiyor (panelde kartı kapalı); daha önce alınmış seviyeler oyunda geçerli kalır.
public enum UpgradeType
{
    Boost,
    Shield,
    CoinRush,
    SpeedReducer
}

public static class PowerUpUpgrades
{
    // Seviye başına fiyat: 1. seviyeye çıkmak 3.000, 5. seviyeye çıkmak 60.000 coin
    private static readonly int[] LevelPrices = { 3000, 7500, 15000, 30000, 60000 };

    private const string GemsKey = "GemsCount";

    // Hız düşürücü 25 -> 27.5 -> 30 -> 32.5 -> 35: 4 seviye. Diğerleri 5 seviye.
    public static int MaxLevel(UpgradeType type)
    {
        return type == UpgradeType.SpeedReducer ? 4 : 5;
    }

    // Her seviyenin eklediği değer: saniye (kalkan, coin rush, boost) ya da hız düşürme miktarı (hız düşürücü)
    public static float Step(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.CoinRush: return 1f;
            case UpgradeType.SpeedReducer: return 2.5f;
            default: return 0.5f;
        }
    }

    public static int GetLevel(UpgradeType type)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(Key(type), 0), 0, MaxLevel(type));
    }

    // Bu güçlendirmenin temel değerine eklenecek miktar (saniye ya da hız düşürücüde düşürme miktarı)
    public static float Bonus(UpgradeType type)
    {
        return GetLevel(type) * Step(type);
    }

    // Bir sonraki seviyenin fiyatı; en üst seviyedeyse -1
    public static int NextPrice(UpgradeType type)
    {
        int level = GetLevel(type);
        return level >= MaxLevel(type) ? -1 : LevelPrices[level];
    }

    // Coin yetiyorsa bir seviye yükseltir
    public static bool TryUpgrade(UpgradeType type)
    {
        int price = NextPrice(type);
        if (price < 0) return false;

        int gems = PlayerPrefs.GetInt(GemsKey, 0);
        if (gems < price) return false;

        gems -= price;
        PlayerPrefs.SetInt(GemsKey, gems);
        PlayerPrefs.SetInt(Key(type), GetLevel(type) + 1);
        PlayerPrefs.Save();
        GameManager.gems = gems;
        return true;
    }

    public static string DisplayName(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Boost: return "BOOST";
            case UpgradeType.Shield: return "SHIELD";
            case UpgradeType.CoinRush: return "COIN RUSH";
            default: return "SPEED REDUCER";
        }
    }

    private static string Key(UpgradeType type) => "Upgrade_" + type;
}
