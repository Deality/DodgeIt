using UnityEngine;

public class InfiniteRoad2D : MonoBehaviour
{
    public Transform[] roadTiles;

    [Header("Road Scroll Settings")]
    [Tooltip("Yolun, engellerin hızına göre ne kadar yavaş akacağını belirler. (Örn: 0.8 = %80 hız)")]
    [Range(0.0f, 1.0f)]
    public float scrollFactor = 0.8f;

    [Header("Görsel Düzeltme")]
    [Tooltip("İki yol arasındaki siyah çizgiyi yok etmek için bindirme payı. (0.01 ile 0.1 arası deneyin)")]
    public float seamFixAmount = 0.05f; // 🔥 YENİ: Varsayılan bindirme payı

    private float tileHeight;
    private float repositionHeight; // Hesaplanan yeni yerleştirme yüksekliği

    void Start()
    {
        if (roadTiles.Length > 0)
        {
            // İlk yol parçasının yüksekliğini al
            SpriteRenderer renderer = roadTiles[0].GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // Orijinal yükseklik
                tileHeight = renderer.bounds.size.y;

                // 🔥 YENİ MANTIK:
                // Yol parçasını yukarı taşırken kullanacağımız mesafeyi,
                // bindirme payı kadar KISALTIYORUZ.
                // Böylece yeni parça, bir öncekinin tam ucuna değil, çok azıcık içine giriyor.
                repositionHeight = (tileHeight * roadTiles.Length) - seamFixAmount;
            }
            else
            {
                Debug.LogError("Yol parçası üzerinde SpriteRenderer bulunamadı!");
            }
        }
    }

    void Update()
    {
        // Dinamik olarak ObstacleManager'dan hızı al
        float currentObstacleSpeed = ObstacleManager.scrollSpeed;
        float roadScrollSpeed = currentObstacleSpeed * scrollFactor;

        foreach (Transform tile in roadTiles)
        {
            // Güncel hızı kullanarak yolu aşağı kaydır
            tile.position += Vector3.down * roadScrollSpeed * Time.deltaTime;

            // Eğer yol parçası ekranın altına indiyse
            // (Tamamen çıkmasını garantilemek için -tileHeight kullanıyoruz)
            if (tile.position.y < -tileHeight)
            {
                // Parçayı en üste taşı ama "repositionHeight" kullanarak
                // hafifçe iç içe geçir.
                tile.position += Vector3.up * repositionHeight;
            }
        }
    }
}