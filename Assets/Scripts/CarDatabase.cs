using UnityEngine;

public class CarDatabase : MonoBehaviour
{
    public static CarDatabase instance;

    [Header("Araba Prefabları (Market Sırasıyla AYNI Olmalı!)")]
    // Buraya, oyun içinde kullanılacak gerçek araba prefablarını sürükleyin.
    public GameObject[] carPrefabs;

    [Header("Yol Resimleri (Market Sırasıyla AYNI Olmalı!)")]
    // Buraya, seçilen yolu değiştirmek için yol sprite'larını sürükleyin.
    public Sprite[] roadSprites;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    // İndekse göre Araba Prefabını getirir
    public GameObject GetCar(int index)
    {
        if (index >= 0 && index < carPrefabs.Length)
            return carPrefabs[index];

        // Hata durumunda varsayılan (0.) arabayı ver
        if (carPrefabs.Length > 0) return carPrefabs[0];

        return null;
    }

    // İndekse göre Yol Resmini getirir
    public Sprite GetRoad(int index)
    {
        if (index >= 0 && index < roadSprites.Length)
            return roadSprites[index];

        if (roadSprites.Length > 0) return roadSprites[0];

        return null;
    }
}