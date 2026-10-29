using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LeaderboardManager : MonoBehaviour
{
    [Header("References")]
    public GameObject rowPrefab;
    public Transform contentParent;

    private const string HighScoreKey = "HighScore";

    // Ekrandaki listenin özeti; arkadan gelen liste aynıysa satırlar yeniden çizilmez
    private string shownSignature;

    private void OnEnable()
    {
        shownSignature = null;
        var cloud = CloudLeaderboardManager.instance;
        if (cloud != null)
        {
            cloud.TopScoresUpdated += OnTopScoresUpdated;
            // Son bilinen liste hemen görünür, güncel olanı arkadan istenir
            ShowEntries(cloud.CachedTopScores, true);
            _ = cloud.RefreshTopScoresAsync();
        }
        else
        {
            ShowEntries(null, true);
        }
    }

    private void OnDisable()
    {
        if (CloudLeaderboardManager.instance != null)
            CloudLeaderboardManager.instance.TopScoresUpdated -= OnTopScoresUpdated;
    }

    private void OnTopScoresUpdated()
    {
        if (this == null || !isActiveAndEnabled) return;
        // İlk çizimde liste boştuysa oyuncunun satırına kaydır; doluysa oyuncu kaydırıyor olabilir, dokunma
        bool wasEmpty = string.IsNullOrEmpty(shownSignature);
        ShowEntries(CloudLeaderboardManager.instance.CachedTopScores, wasEmpty);
    }

    // Unity Gaming Services her oyuncu adının sonuna "#12345" gibi bir numara ekler. Aynı adı seçen
    // oyuncular ayırt edilebilsin diye bu numara adın yanında küçük ve soluk gösterilir.
    private static string DisplayName(string cloudName)
    {
        if (string.IsNullOrEmpty(cloudName)) return "Player";
        int hash = cloudName.LastIndexOf('#');
        string name = hash > 0 ? cloudName.Substring(0, hash) : cloudName;
        string tag = hash > 0 ? cloudName.Substring(hash) : "";
        // Filtre eklenmeden önce alınmış ya da başka yoldan girmiş uygunsuz adlar tabloda gösterilmez
        if (!NameFilter.IsAllowed(name)) name = "Player";
        // noparse: oyuncunun yazdığı "<" gibi karakterler zengin metin etiketi sanılmasın
        string shown = "<noparse>" + name + "</noparse>";
        return tag.Length > 0 ? shown + "<size=60%><alpha=#73> " + tag + "</size>" : shown;
    }

    private static string Signature(List<CloudLeaderboardManager.CachedEntry> entries)
    {
        if (entries == null) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var e in entries) sb.Append(e.rank).Append('|').Append(e.name).Append('|').Append(e.score).Append('|').Append(e.isOwn).Append(';');
        return sb.ToString();
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

    private void ShowEntries(List<CloudLeaderboardManager.CachedEntry> entries, bool scrollToOwn)
    {
        string signature = Signature(entries);
        if (signature == shownSignature) return;
        shownSignature = signature;

        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        if (entries != null && entries.Count > 0)
        {
            RectTransform myRow = null;
            foreach (var entry in entries)
            {
                GameObject row = Instantiate(rowPrefab, contentParent);
                // Oyuncunun kendi satırı koyu mavi çerçeveyle işaretlenir
                row.GetComponent<LeaderboardEntry>().SetData(entry.rank, DisplayName(entry.name), entry.score, entry.isOwn);
                if (entry.isOwn) myRow = (RectTransform)row.transform;
            }

            if (scrollToOwn && myRow != null) _ = ScrollNextFrame(myRow);
            return;
        }

        // Fallback (cloud unreachable, or no scores submitted yet anywhere): show the
        // local high score only, so the panel isn't just empty.
        int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (highScore <= 0) return;

        GameObject fallbackRow = Instantiate(rowPrefab, contentParent);
        LeaderboardEntry fallbackEntry = fallbackRow.GetComponent<LeaderboardEntry>();
        fallbackEntry.SetData(1, NicknameManager.GetNickname(), highScore, true);
    }

    private async Task ScrollNextFrame(RectTransform myRow)
    {
        await Task.Yield(); // satırların yerleşmesi için bir kare bekle
        if (this != null && myRow != null) ScrollToRow(myRow);
    }
}
