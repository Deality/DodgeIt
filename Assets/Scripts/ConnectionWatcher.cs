using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

// Sahne değişse bile hayatta kalan tekil (singleton) bileşen.
// Oyun internetsiz de oynanabildiği için bağlantı kopunca artık kendiliğinden pop-up açmaz;
// internet yoksa oyuncu bunu yalnızca reklam izlemek istediğinde görür (RewardedAd).
// CheckConnectionAndShowPopupIfNeeded elle çağrılırsa eski pop-up davranışı hâlâ çalışır.
public class ConnectionWatcher : MonoBehaviour
{
    public static ConnectionWatcher instance;

    [Header("Bağlantı Kaybı Pop-up UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI messageText;
    public Button connectButton;

    [Tooltip("Bağlantı durumunun arka planda kontrol edilme sıklığı (saniye).")]
    public float checkInterval = 1f;

    [Tooltip("Bu sahnede pop-up hiç gösterilmez; Yükleme Ekranı zaten kendi bağlantı kontrolünü yapıyor.")]
    public string loadingSceneName = "LoadingScreen";

    private bool popupVisible = false;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectButtonClicked);

        HidePopup();
    }

    // Oyunun herhangi bir yerinden (ör. Game Over paneli açıldığında) anında kontrol
    // tetiklemek için de kullanılabilir.
    public void CheckConnectionAndShowPopupIfNeeded()
    {
        if (SceneManager.GetActiveScene().name == loadingSceneName)
        {
            if (popupVisible) HidePopup();
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ShowPopup();
        }
    }

    private void ShowPopup()
    {
        if (popupVisible || popupPanel == null) return;
        popupVisible = true;
        popupPanel.SetActive(true);

        if (messageText != null)
            messageText.text = "Your internet connection was lost. Please check your connection and try again.";
    }

    private void HidePopup()
    {
        popupVisible = false;
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void OnConnectButtonClicked()
    {
        HidePopup();
        Time.timeScale = 1f;
        SceneManager.LoadScene(loadingSceneName);
    }
}
