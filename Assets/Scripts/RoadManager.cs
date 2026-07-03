using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [Header("Bağlantılar")]
    [Tooltip("Sahnede yolu hareket ettiren InfiniteRoad2D scriptinin olduğu objeyi buraya sürükle.")]
    public InfiniteRoad2D infiniteRoadScript;

    void Start()
    {
        // Eğer inspector'dan atamayı unuttuysan otomatik bulmayı dene
        if (infiniteRoadScript == null)
        {
            infiniteRoadScript = FindObjectOfType<InfiniteRoad2D>();
        }

        ApplySelectedRoad();
    }

    public void ApplySelectedRoad()
    {
        // 1. Seçilen yolun numarasını hafızadan al
        int selectedIndex = PlayerPrefs.GetInt("Selected_Road", 0);

        // 2. Veritabanından (CarDatabase) o yolun resmini bul
        if (CarDatabase.instance != null && infiniteRoadScript != null)
        {
            Sprite selectedSprite = CarDatabase.instance.GetRoad(selectedIndex);

            if (selectedSprite != null)
            {
                // 3. InfiniteRoad2D içindeki tüm yol parçalarının resmini değiştir
                if (infiniteRoadScript.roadTiles != null)
                {
                    foreach (Transform tile in infiniteRoadScript.roadTiles)
                    {
                        if (tile != null)
                        {
                            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.sprite = selectedSprite;
                            }
                        }
                    }
                    Debug.Log($"🛣️ Yol Görseli Değiştirildi: {selectedSprite.name}");
                }
            }
            else
            {
                Debug.LogWarning($"HATA: CarDatabase içinde {selectedIndex} numaralı yol resmi bulunamadı!");
            }
        }
        else
        {
            Debug.LogWarning("HATA: CarDatabase veya InfiniteRoad2D referansı eksik!");
        }
    }
}