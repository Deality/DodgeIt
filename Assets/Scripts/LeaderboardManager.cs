using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [Header("References")]
    public GameObject rowPrefab;
    public Transform contentParent;

    private const string HighScoreKey = "HighScore";

    private void OnEnable()
    {
        PopulateBoard();
    }

    private void PopulateBoard()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (highScore <= 0) return;

        GameObject row = Instantiate(rowPrefab, contentParent);
        LeaderboardEntry entry = row.GetComponent<LeaderboardEntry>();
        entry.SetData(1, "Player", highScore);
    }
}
