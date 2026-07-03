using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Ayarlar")]
    public Transform spawnPoint; // "StartPoint" objesini buraya at

    void Start()
    {
        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        // 1. Hangi araba seçili?
        // MarketManager'da kaydettiğimiz anahtar "Selected_Car" idi.
        int selectedIndex = PlayerPrefs.GetInt("Selected_Car", 0);

        // 2. Veritabanından o prefabı al
        if (CarDatabase.instance != null)
        {
            GameObject carPrefab = CarDatabase.instance.GetCar(selectedIndex);

            // 3. Yarat
            if (carPrefab != null)
            {
                // Varsa sahnedeki eski arabayı bul ve sil (Çakışma olmasın)
                GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
                if (oldPlayer != null) Destroy(oldPlayer);

                // YENİ ARABAYI YARAT
                GameObject newPlayer = Instantiate(carPrefab, spawnPoint.position, Quaternion.identity);

                // 🔥 ÖNEMLİ: Tag'i ve İsmi standart yapalım ki diğer scriptler bulsun
                newPlayer.tag = "Player";
                newPlayer.name = "Player";

                // Not: Eğer araba prefabında Animator varsa, 
                // Instantiate edildiği anda otomatik olarak Intro animasyonunu oynatacaktır.
            }
            else
            {
                Debug.LogError($"HATA: CarDatabase içinde {selectedIndex} numaralı araba bulunamadı!");
            }
        }
        else
        {
            Debug.LogError("HATA: Sahnede CarDatabase bulunamadı! GameManager objesine CarDatabase scriptini eklediniz mi?");
        }
    }
}