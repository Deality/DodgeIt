using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Start butonuna basınca açılan mod seçim ekranı: Klasik oyun ya da günde 1 ücretsiz + 1 reklamlı hakkı olan Drift (Coin Rush) modu.
public class ModeSelectPanel : MonoBehaviour
{
    public TextMeshProUGUI driftPlaysText;
    public Button driftButton;
    public Image driftButtonImage;
    [Tooltip("Classic kartındaki araba görseli. Markette seçili arabanın sprite'ı ile değiştirilir.")]
    public Image classicCarIcon;

    public Color driftReadyColor = new Color32(246, 198, 62, 255);
    public Color driftEmptyColor = new Color32(120, 120, 140, 255);

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        RefreshClassicCarIcon();

        int left = GameModeState.DriftPlaysLeft;
        if (driftPlaysText != null)
        {
            if (GameModeState.NextDriftPlayNeedsAd)
            {
                driftPlaysText.text = "WATCH AD FOR +1 PLAY";
            }
            else if (left > 0)
            {
                driftPlaysText.text = $"{GameModeState.DriftFreePlays} FREE PLAY TODAY";
            }
            else
            {
                driftPlaysText.text = "NO PLAYS LEFT TODAY"; // yenilenmeye kalan süreyi kartın altındaki sayaç gösterir
            }
        }
        if (driftButton != null) driftButton.interactable = left > 0;
        if (driftButtonImage != null) driftButtonImage.color = left > 0 ? driftReadyColor : driftEmptyColor;
    }

    // Classic kartında oyuncunun markette seçtiği araba görünür
    void RefreshClassicCarIcon()
    {
        if (classicCarIcon == null || CarDatabase.instance == null) return;
        GameObject car = CarDatabase.instance.GetCar(PlayerPrefs.GetInt("Selected_Car", 0));
        var sr = car != null ? car.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (sr != null && sr.sprite != null) classicCarIcon.sprite = sr.sprite;
    }

    public void PlayClassic()
    {
        if (UIManager.instance != null) UIManager.instance.StartGameInMode(GameMode.Classic);
    }

    private bool waitingForAd; // reklam sürerken butona tekrar basılırsa ikinci reklam açılmasın

    public void PlayDrift()
    {
        if (waitingForAd) return;

        // Ücretsiz hak bittiyse ikinci hak reklam izlenince açılır
        if (GameModeState.NextDriftPlayNeedsAd)
        {
            waitingForAd = true;
            RewardedAd.Show(this, () => { waitingForAd = false; StartDrift(); }, () => waitingForAd = false);
            return;
        }
        StartDrift();
    }

    void StartDrift()
    {
        if (!GameModeState.TryConsumeDriftPlay())
        {
            Refresh();
            return;
        }
        if (UIManager.instance != null) UIManager.instance.StartGameInMode(GameMode.Drift);
    }
}
