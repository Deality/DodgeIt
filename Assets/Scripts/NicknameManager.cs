using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shown once on first launch so the player can pick a display name before it's saved to
// this device (PlayerPrefs) and pushed to Unity Gaming Services as the player's cloud
// leaderboard name (see CloudLeaderboardManager). Spaces are rejected because UGS player
// names can't contain whitespace.
public class NicknameManager : MonoBehaviour
{
    public const string NicknameKey = "PlayerNickname";
    private const int MaxNicknameLength = 16;

    public static NicknameManager instance;

    [Header("References")]
    public GameObject nicknamePanel;
    public TMP_InputField nicknameInputField;
    public TextMeshProUGUI warningText;
    public Button confirmButton;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (warningText != null) warningText.gameObject.SetActive(false);

        if (HasNickname())
        {
            if (nicknamePanel != null) nicknamePanel.SetActive(false);
        }
        else
        {
    if (nicknamePanel != null) nicknamePanel.SetActive(true);
            if (nicknameInputField != null)
            {
                nicknameInputField.text = "";
                nicknameInputField.onValidateInput = (text, charIndex, addedChar) => char.IsWhiteSpace(addedChar) ? '\0' : addedChar;
                nicknameInputField.ActivateInputField();
            }
        }
    }

    public void OnConfirmClicked()
    {
        if (nicknameInputField == null) return;

        string nickname = nicknameInputField.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            ShowWarning("Please enter a nickname.");
            return;
        }

        if (nickname.Any(char.IsWhiteSpace))
        {
            ShowWarning("Nickname can't contain spaces.");
            return;
        }

        if (nickname.Length > MaxNicknameLength)
        {
            nickname = nickname.Substring(0, MaxNicknameLength);
        }

        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.Save();

        if (nicknamePanel != null) nicknamePanel.SetActive(false);

        _ = CloudLeaderboardManager.instance?.UpdatePlayerNameAsync(nickname);
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    public static string GetNickname()
    {
        return PlayerPrefs.GetString(NicknameKey, "Player");
    }

    public static bool HasNickname()
    {
        return !string.IsNullOrWhiteSpace(PlayerPrefs.GetString(NicknameKey, ""));
    }
}
