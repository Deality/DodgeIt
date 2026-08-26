using UnityEngine;

public class CarCollision : MonoBehaviour
{
    private Collider2D carBodyCollider;

    void Start()
    {
        // Arabanın kendi Collider'ını al
        carBodyCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 🔥 KONTROL: Eğer çarpan şey bizim ana gövdemize (Body Collider) DEĞMİYORSA,
        // demek ki child objelerden birine (Sensöre) çarpmıştır. Yoksay.
        if (carBodyCollider != null && !carBodyCollider.IsTouching(other))
        {
            return;
        }

        // Engel ile çarpışma kontrolü
        if (other.CompareTag("Obstacle"))
        {
            // 🔥 GÜNCELLENDİ: Görünmezlik, Öfke Modu (Boost) veya Yavaşlama Koruması (Grace Period) varsa kaza yapma
            bool isGrace = (CarController2D.instance != null && CarController2D.instance.isBoostGracePeriod);

            if (GameManager.instance != null && (GameManager.instance.IsInvincible || GameManager.instance.isBoosting || isGrace))
            {
                return;
            }

            Debug.Log("💀 Çarptın! Oyun Bitti!");

            if (GameManager.instance != null)
            {
                // Çarpışmanın bu kanaldan gelmesi durumunda çift dikiş koruma titreşimi
                if (GameManager.instance.enableVibration)
                {
#if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate(); // Telefonun kendi motorunu kullanarak fiziksel titreşim verir
#endif
                }

                // 🔥 DÜZELTME: Burada bare GameOver() çağrılıyordu, ama çarpışma efekti/sesi
                // sadece TriggerCrash() içinde tetikleniyor. Obstacle.cs'teki çarpışma da AYNI
                // fiziksel temas için AYRICA tetikleniyor (iki taraf da OnTriggerEnter2D alıyor);
                // hangisi önce çalışırsa çalışsın artık ikisi de aynı TriggerCrash() metodunu
                // çağırıyor. TriggerCrash() zaten "IsGameOver ise tekrar çalışma" koruması
                // içerdiğinden efekt/ses YALNIZCA bir kez, ama HER ZAMAN çalışır.
                Vector3 crashPosition = carBodyCollider != null ? carBodyCollider.ClosestPoint(other.transform.position) : transform.position;
                GameManager.instance.TriggerCrash(crashPosition);
            }
        }
    }
}