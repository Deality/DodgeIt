using UnityEngine;
using UnityEngine.UI;
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
        string name = hash > 0 ? cloudName.Substring(0, hash) : cloudName;
        // Filtre eklenmeden önce alınmış ya da başka yoldan girmiş uygunsuz adlar tabloda gösterilmez
        return NameFilter.IsAllowed(name) ? name : "Player";
    }

    // Listeyi, oyuncunun kendi satırı görünür alanın ortasına gelecek şekilde kaydırır
    private void ScrollToRow(RectTransform row)
    {
        ScrollRect scroll = contentParent.GetComponentInParent<ScrollRect>();
        RectTransform content = contentParent as RectTransform;
        if (scroll == null || content == null || row == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        float hidden = content.rect.height - viewport.rect.height;
        if (hidden <= 0f) return; // hepsi zaten sığıyor

        // Satırın içerik üst kenarından uzaklığı (içerik pivotundan bağımsız)
        float rowCentreFromTop = content.rect.yMax - row.localPosition.y;
        float target = rowCentreFromTop - viewport.rect.height * 0.5f;
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(target / hidden);
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
                string myId = CloudLeaderboardManager.instance.PlayerId;
                RectTransform myRow = null;
                foreach (var cloudEntry in entries)
                {
                    GameObject row = Instantiate(rowPrefab, contentParent);
                    LeaderboardEntry rowView = row.GetComponent<LeaderboardEntry>();
                    // Oyuncunun kendi satırı koyu mavi çerçeveyle işaretlenir
                    bool isOwn = !string.IsNullOrEmpty(myId) && cloudEntry.PlayerId == myId;
                    rowView.SetData(cloudEntry.Rank + 1, DisplayName(cloudEntry.PlayerName), Mathf.RoundToInt((float)cloudEntry.Score), isOwn);
                    if (isOwn) myRow = (RectTransform)row.transform;
                }

                if (myRow != null)
                {
                    await Task.Yield(); // satırların yerleşmesi için bir kare bekle
                    if (this != null && myRow != null) ScrollToRow(myRow);
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
        fallbackEntry.SetData(1, NicknameManager.GetNickname(), highScore, true);
    }
}
