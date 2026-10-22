using System;
using UnityEngine;
using TMPro;

// Günlük görevlerin ve Coin Rush haklarının sıfırlanmasına (gece yarısı) kalan süreyi
// 24 saatten geriye sayan yazı. Her saniye güncellenir.
[RequireComponent(typeof(TextMeshProUGUI))]
public class DailyResetCountdown : MonoBehaviour
{
    [Tooltip("{0} yerine kalan süre (SS:DD:ss) yazılır.")]
    public string format = "RESETS IN {0}";

    private TextMeshProUGUI label;
    private int lastSecond = -1;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        lastSecond = -1;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        TimeSpan left = DateTime.Today.AddDays(1) - DateTime.Now;
        int total = Mathf.Max(0, (int)left.TotalSeconds);
        if (total == lastSecond) return;
        lastSecond = total;
        label.text = string.Format(format, $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}");
    }
}
