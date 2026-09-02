using UnityEngine;

// Genel Scroller.cs, nesnenin yalnızca PIVOT'unun ekran dışına çıkışını kontrol eder. Lastik
// izinde pivot, şeklin bir UCUNDA (ortasında değil) olduğu için bu kontrol izin hâlâ kısmen
// ekranda görünürken yok edilmesine yol açar. Bu yüzden pivot yerine Renderer'ın gerçek
// (LineRenderer genişliğini de içeren) dünya-uzayı sınırlarını kullanıyoruz: iz, en üstteki
// (en son çizilen) noktası da ekranın altından çıkana kadar yok edilmiyor.
[RequireComponent(typeof(Renderer))]
public class TireMarkScroller : MonoBehaviour
{
    private Camera mainCam;
    private Renderer rend;

    void OnEnable()
    {
        mainCam = Camera.main;
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // İz, yolun kendi hızında kaymalı (yol ObstacleManager.scrollSpeed'in scrollFactor
        // kadarını kullanıyor); tam hızda kayarsa iz zamanla yolun üzerinde ileri sürüklenir.
        float roadFactor = InfiniteRoad2D.Instance != null ? InfiniteRoad2D.Instance.scrollFactor : 1f;
        float currentSpeed = ObstacleManager.scrollSpeed * roadFactor;
        transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World);

        if (mainCam == null || rend == null) return;

        Vector3 topPoint = new Vector3(transform.position.x, rend.bounds.max.y, 0f);
        Vector3 viewportPos = mainCam.WorldToViewportPoint(topPoint);

        if (viewportPos.y < 0f)
        {
            PoolManager.Despawn(gameObject);
        }
    }
}
