using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Start butonuna basınca açılan mod seçim ekranı: Klasik oyun ya da günde 3 hakkı olan Drift modu.
public class ModeSelectPanel : MonoBehaviour
{
    public TextMeshProUGUI driftPlaysText;
    public Button driftButton;
    public Image driftButtonImage;

    public Color driftReadyColor = new Color32(246, 198, 62, 255);
    public Color driftEmptyColor = new Color32(120, 120, 140, 255);

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int left = GameModeState.DriftPlaysLeft;
        if (driftPlaysText != null)
        {
            if (left > 0)
            {
                driftPlaysText.text = $"{left}/{GameModeState.DriftDailyPlays} PLAYS LEFT TODAY";
            }
            else
            {
                var wait = GameModeState.TimeUntilReset;
                driftPlaysText.text = $"NEW PLAYS IN {wait.Hours:00}:{wait.Minutes:00}";
            }
        }
        if (driftButton != null) driftButton.interactable = left > 0;
        if (driftButtonImage != null) driftButtonImage.color = left > 0 ? driftReadyColor : driftEmptyColor;
    }

    public void PlayClassic()
    {
        if (UIManager.instance != null) UIManager.instance.StartGameInMode(GameMode.Classic);
    }

    public void PlayDrift()
    {
        if (!GameModeState.TryConsumeDriftPlay())
        {
            Refresh();
            return;
        }
        if (UIManager.instance != null) UIManager.instance.StartGameInMode(GameMode.Drift);
    }
}
