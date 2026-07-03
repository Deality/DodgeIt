using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    // **Mutlaka public ve parametresiz olmalı**
    public void RestartGame()
    {
        Time.timeScale = 1f; // oyunu normale al
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // sahneyi yeniden başlat
    }
}