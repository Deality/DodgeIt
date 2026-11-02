using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Ump.Api;

// Ayarlar panelindeki gizlilik bağlantısı: oyuncu reklam izni tercihini buradan değiştirebilir.
// Yalnızca izin formunun gerektiği bölgelerde (AB, Birleşik Krallık vb.) görünür.
public class PrivacySettingsButton : MonoBehaviour
{
    public Button button;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(ShowForm);
    }

    void OnEnable()
    {
        Refresh();
    }

    void Refresh()
    {
        if (button == null) return;
        button.gameObject.SetActive(ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required);
    }

    void ShowForm()
    {
        if (!RewardedAd.IsAvailable) { RewardedAd.ShowNoInternetNotice(); return; }
        ConsentForm.ShowPrivacyOptionsForm(error =>
        {
            if (error != null) Debug.LogWarning("Privacy options form failed: " + error.Message);
        });
    }
}
