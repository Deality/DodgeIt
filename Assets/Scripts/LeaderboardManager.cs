using UnityEngine;
using System.Threading.Tasks;

public class LeaderboardManager : MonoBehaviour
{
    [Header("References")]
    public GameObject rowPrefab;
    public Transform contentParent;

    private const string HighScoreKey = "HighScore";

    private async void OnEnable()
    {
        await PopulateBoard();
    }

    // Unity Gaming Services her oyuncu adının sonuna "#12345" gibi bir numara ekler; tabloda yalnızca
    // oyuncunun seçtiği takma adı gösteriyoruz.
    private static string DisplayName(string cloudName)
    {
        if (string.IsNullOrEmpty(cloudName)) return "Player";
        int hash = cloudName.LastIndexOf('#');
        return hash > 0 ? cloudName.Substring(0, hash) : cloudName;
    }

    private async Task PopulateBoard()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (CloudLeaderboardManager.instance != null)
        {
            var entries = await CloudLeaderboardManager.instance.GetTopScoresAsync(50);
            if (entries != null && entries.Count > 0)
            {
                foreach (var cloudEntry in entries)
                {
                    GameObject row = Instantiate(rowPrefab, contentParent);
                    LeaderboardEntry rowView = row.GetComponent<LeaderboardEntry>();
                    rowView.SetData(cloudEntry.Rank + 1, DisplayName(cloudEntry.PlayerName), Mathf.RoundToInt((float)cloudEntry.Score));
                }
                return;
            }
        }

        // Fallback (cloud unreachable, or no scores submitted yet anywhere): show the
        // local high score only, so the panel isn't just empty.
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (highScore <= 0) return;

        GameObject fallbackRow = Instantiate(rowPrefab, contentParent);
        LeaderboardEntry fallbackEntry = fallbackRow.GetComponent<LeaderboardEntry>();
        fallbackEntry.SetData(1, NicknameManager.GetNickname(), highScore);
    }
}
