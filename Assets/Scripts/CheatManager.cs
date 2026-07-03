using UnityEngine;
using TMPro;

public class CheatManager : MonoBehaviour
{
    [Header("Hile Ayarları")]
    public string cheatCode = "2000gems";
    public int rewardAmount = 2000;

    [Header("UI Referansı")]
    public TMP_InputField cheatInputField;

    // Bu fonksiyonu butona bağla
    public void SubmitCode()
    {
        Debug.Log("🖱️ Butona Basıldı! Kontrol başlıyor...");

        if (cheatInputField == null)
        {
            Debug.LogError("❌ HATA: Inspector'da 'Cheat Input Field' kutusu boş! Lütfen atamayı yap.");
            return;
        }

        // Girilen metni al, sağındaki solundaki boşlukları (Trim) sil ve küçült
        string input = cheatInputField.text.Trim().ToLower();
        string targetCode = cheatCode.Trim().ToLower();

        Debug.Log($"Girdiğin: '{input}' | Olması Gereken: '{targetCode}'");

        if (input == targetCode)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AddGems(rewardAmount);

                Debug.Log($"✅ HİLE BAŞARILI! {rewardAmount} Coin eklendi.");

                // Başarılı olunca kutuyu temizle
                cheatInputField.text = "";

                // Market açıksa UI güncelle
                if (MarketManager.instance != null)
                {
                    MarketManager.instance.UpdateUI();
                }
            }
            else
            {
                Debug.LogError("❌ HATA: GameManager sahnede bulunamadı!");
            }
        }
        else
        {
            Debug.Log("❌ Yanlış Şifre! Tekrar dene.");
            cheatInputField.text = ""; // Yanlışsa sil
        }
    }
}