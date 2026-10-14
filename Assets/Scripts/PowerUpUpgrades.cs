using UnityEngine;

// Güçlendirmelerin süre yükseltmeleri. Powerups panelinden coin ile satın alınır; seviye PlayerPrefs'te tutulur.
// Oyun içindeki her güçlendirme kendi temel süresine Bonus(...) kadar saniye ekler.
public enum UpgradeType
{
    Boost,
    Shield,
    CoinRush,
    SpeedReducer
}

public static class PowerUpUpgrades
{
    public const int MaxLevel = 5;

    // Seviye başına fiyat: 1. seviyeye çıkmak 1.000, 5. seviyeye çıkmak 20.000 coin
    private static readonly int[] LevelPrices = { 1000, 2500, 5000, 10000, 20000 };

    private const string GemsKey = "GemsCount";

    // Her seviyenin süreye eklediği saniye
    public static float StepSeconds(UpgradeType type)
    {
        return type == UpgradeType.CoinRush ? 1f : 0.5f;
    }

    public static int GetLevel(UpgradeType type)
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(Key(type), 0), 0, MaxLevel);
    }

    // Bu güçlendirmenin temel süresine eklenecek saniye
    public static float Bonus(UpgradeType type)
    {
        return GetLevel(type) * StepSeconds(type);
    }

    // Bir sonraki seviyenin fiyatı; en üst seviyedeyse -1
    public static int NextPrice(UpgradeType type)
    {
        int level = GetLevel(type);
        return level >= MaxLevel ? -1 : LevelPrices[level];
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
