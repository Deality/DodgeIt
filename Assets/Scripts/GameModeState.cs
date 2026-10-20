using System;
using UnityEngine;

// Seçili oyun modu (sahne geçişlerinde korunur) ve Drift modunun günlük hakları.
public enum GameMode
{
    Classic,
    Drift
}

public static class GameModeState
{
    public static GameMode Current = GameMode.Classic;
    public static bool IsDrift => Current == GameMode.Drift;

    // Drift (Coin Rush) modu: günde 1 ücretsiz hak + reklam izleyerek 1 hak daha; gün değişince sıfırlanır
    public const int DriftFreePlays = 1;
    public const int DriftDailyPlays = 2; // ücretsiz + reklamlı toplam
    private const string DriftDateKey = "DriftMode_Date";
    private const string DriftUsedKey = "DriftMode_Used";

    private static string Today => DateTime.Now.ToString("yyyyMMdd");

    public static int DriftPlaysLeft
    {
        get
        {
            if (PlayerPrefs.GetString(DriftDateKey, "") != Today) return DriftDailyPlays;
            return Mathf.Max(0, DriftDailyPlays - PlayerPrefs.GetInt(DriftUsedKey, 0));
        }
    }

    // Bir hak kullanır; hak kalmadıysa false döner
    public static bool TryConsumeDriftPlay()
    {
        if (DriftPlaysLeft <= 0) return false;

        if (PlayerPrefs.GetString(DriftDateKey, "") != Today)
        {
            PlayerPrefs.SetString(DriftDateKey, Today);
            PlayerPrefs.SetInt(DriftUsedKey, 0);
        }
        PlayerPrefs.SetInt(DriftUsedKey, PlayerPrefs.GetInt(DriftUsedKey, 0) + 1);
        PlayerPrefs.Save();
        return true;
    }

    // Ücretsiz hak bitti ama reklamla açılacak hak var mı?
    public static bool NextDriftPlayNeedsAd => DriftPlaysLeft > 0 && DriftPlaysLeft <= DriftDailyPlays - DriftFreePlays;

    // Yeni hakların açılmasına kalan süre (gece yarısı)
    public static TimeSpan TimeUntilReset => DateTime.Today.AddDays(1) - DateTime.Now;
}
