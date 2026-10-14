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

    // Drift modu: günde 3 hak; gün değişince sıfırlanır
    public const int DriftDailyPlays = 3;
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

    // Yeni hakların açılmasına kalan süre (gece yarısı)
    public static TimeSpan TimeUntilReset => DateTime.Today.AddDays(1) - DateTime.Now;
}
