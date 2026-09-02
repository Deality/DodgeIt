using UnityEngine;
using TMPro;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    // 🔥 DÜZELTME 1: Tek bir yazı yerine Dizi (Array) tanımladık "[]" işaretiyle.
    // Artık Unity'de Inspector'da "Size" yazan yere 3 yazıp, 3 yazıyı da buraya sürükleyebilirsin!
    [Tooltip("Ana menüdeki Gem miktarını gösteren TÜM UI elementlerini buraya sürükleyin.")]
    public TextMeshProUGUI[] mainMenuGemTexts;

    // PlayerPrefs Anahtarı (GameManager ile aynı olmalı)
    private const string GemsKey = "GemsCount";

    // Performans için eski parayı hafızada tutuyoruz
    private int lastGemCount = -1;

    void Start()
    {
        // Başlangıçta hemen güncelle
        UpdateAllGemTexts();
        StartCoroutine(PollGemsRoutine());
    }

    // Her karede PlayerPrefs okumak gereksiz CPU harcıyordu (para her karede değişmiyor);
    // bunun yerine düşük frekanslı bir döngüyle kontrol ediyoruz.
    IEnumerator PollGemsRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.25f);
        while (true)
        {
            int currentGems = PlayerPrefs.GetInt(GemsKey, 0);
            if (currentGems != lastGemCount)
            {
                lastGemCount = currentGems;
                UpdateAllGemTexts();
            }
            yield return wait;
        }
    }

    // Listeye eklediğin tüm yazıları tek seferde güncelleyen fonksiyon
    private void UpdateAllGemTexts()
    {
        int savedGems = PlayerPrefs.GetInt(GemsKey, 0);

        // Dizideki (Listedi) her bir yazı objesi için tek tek dön
        foreach (TextMeshProUGUI gemText in mainMenuGemTexts)
        {
            if (gemText != null)
            {
                gemText.text = savedGems.ToString();
            }
        }
    }
}