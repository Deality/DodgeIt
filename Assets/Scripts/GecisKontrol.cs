using UnityEngine;
using UnityEngine.SceneManagement; // Sahne yönetimi için bu kütüphane gereklidir.

public class GecisKontrol : MonoBehaviour
{
    // Bu metot butona tıklandığında çağrılacak.
    public void OyunuBaslat()
    {

        Time.timeScale = 1f;
        // SceneManager.LoadScene metodu sahne adı veya build index numarası ile sahne yükler.
        // "SampleScene" sahnesine geçiş yapar.
        SceneManager.LoadScene("SampleScene");
    }
}