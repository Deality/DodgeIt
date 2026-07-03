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

                GameManager.instance.GameOver();
            }
        }
    }
}