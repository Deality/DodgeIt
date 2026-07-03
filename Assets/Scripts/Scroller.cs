using UnityEngine;

public class Scroller : MonoBehaviour
{
    // Bu script, ObstacleManager'ın statik olarak tuttuğu hızı kullanır.

    private Camera mainCam;
    // Rigidbody2D referansını artık hareket için kullanmıyoruz, sadece çarpışma için gerekli.

    void Start()
    {
        mainCam = Camera.main;

        // Rigidbody bileşenini kontrol etmek isteyebilirsiniz, ancak hareket için kullanmayacağız.
        // Rigidbody2D'nin prefab'da Kinematic olarak kaldığından emin olun.
    }

    // 🔥 GÜNCELLEME: Hareketi Update'e taşıyoruz ve transform.Translate kullanıyoruz.
    void Update()
    {
        // Hızı kullanarak objeyi aşağı kaydır
        float currentSpeed = ObstacleManager.scrollSpeed;

        // Kinematic Rigidbody'nin bağlı olduğu Transform'u direkt hareket ettir.
        transform.Translate(Vector3.down * currentSpeed * Time.deltaTime, Space.World);

        // Obje, kamera sınırının altına indiğinde yok et (Viewport bazlı yok etme)
        if (mainCam != null)
        {
            Vector3 viewportPos = mainCam.WorldToViewportPoint(transform.position);

            if (viewportPos.y < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}