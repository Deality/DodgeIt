using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

// --- 1. GÖREV SİSTEMİ ENUMLARI ---
public enum MissionType
{
    UseBoost,
    TriggerNearmiss,
    CollectSlowMotion,
    CollectShield,
    BuyCar,
    BuyRoad,
    ReachScore
}

public enum RewardType
{
    Coin,
    Boost,
    Shield
}

public enum MissionCategory
{
    Weekly,       // Haftalık (Belirli aralıklarla sıfırlanır)
    Achievement   // Başarım (Kalıcı)
}

// --- 2. GÖREV VERİ YAPISI ---
[System.Serializable]
public class Mission
{
    [Header("Görev Bilgileri")]
    public string missionID;          // Benzersiz ID 
    public string description;        // Arayüzde görünecek açıklama
    public MissionType missionType;
    public int targetValue;           // Ulaşılması gereken hedef
    public int currentValue;          // Mevcut ilerleme

    [Header("Ödüller")]
    public RewardType rewardType;
    public int rewardAmount;          // Ödül miktarı

    [Header("Durum")]
    public bool isCompleted;          // Görev tamamlandı mı?
    public bool isClaimed;            // Ödül alındı mı?

    // Kümülâtif ilerleyen görevler için (+1, +5 ekleme yapar)
    public void Progress(int amount)
    {
        if (isCompleted) return;

        currentValue += amount;
        if (currentValue >= targetValue)
        {
            currentValue = targetValue;
            isCompleted = true;
            Debug.Log($"[Görev Tamamlandı] {description}");
        }
    }

    // Skor gibi tek seferde en yüksek değerin kontrol edildiği görevler için
    public void CheckAbsoluteTarget(int value)
    {
        if (isCompleted) return;

        if (value > currentValue)
        {
            currentValue = value;
            if (currentValue >= targetValue)
            {
                currentValue = targetValue;
                isCompleted = true;
                Debug.Log($"[Görev Tamamlandı] {description}");
            }
        }
    }
}

// --- 3. ANA GÖREV YÖNETİCİSİ SINIFI ---
[DefaultExecutionOrder(-100)]
public class MissionsManager : MonoBehaviour
{
    public static MissionsManager instance;
    public static MissionsManager Instance => instance;

    [Header("UI Referansları")]
    [Tooltip("Sahnede bulunan ana Scroll View objesinin üzerindeki ScrollRect bileşeni")]
    public ScrollRect scrollView;

    [Tooltip("Haftalık görevlerin (Weekly Quests) dizileceği 1. Content objesi")]
    public Transform weeklyContentParent;

    [Tooltip("Başarımların (Achievements) dizileceği 2. Content objesi")]
    public Transform achievementsContentParent;

    [Header("Sekme Butonları")]
    public Button weeklyTabButton;
    public Button achievementsTabButton;
    public Color activeTabColor = Color.green;
    public Color inactiveTabColor = Color.gray;

    [Header("Ana Menü Bildirim Ayarı")]
    public GameObject missionNotificationBadge;

    [Header("Görev Havuzları")]
    public List<Mission> allWeeklyQuests;
    public List<Mission> allAchievements;

    // Sahnede elle dizilen satırların hafızası
    private MissionRowView[] weeklyRows;
    private MissionRowView[] achievementRows;

    private MissionCategory currentCategory = MissionCategory.Weekly;
    private const string LastWeekKey = "LastPlayedWeek";
    private bool isInitialized = false;

    void Awake()
    {
        // Çift ve boş kopyaların çakışmasını önleyen Güvenlik Duvarı
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (activeSceneName == "MainMenu")
        {
            if (instance == null) instance = this;
            else if (instance != this) Destroy(this);
        }
        else
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        // Sadece Mission ID'si BOŞ olanlara otomatik ID atar, yazdıklarını KESİNLİKLE bozmaz!
        AssignMissingIDs();

        if (weeklyTabButton != null) weeklyTabButton.onClick.AddListener(() => SwitchTab(MissionCategory.Weekly));
        if (achievementsTabButton != null) achievementsTabButton.onClick.AddListener(() => SwitchTab(MissionCategory.Achievement));

        CheckWeeklyReset();
        SyncPendingGameplayProgress();

        InitializeExistingRows();

        // Başlangıçta Achievement sekmesini test etmek istersen burayı MissionCategory.Achievement yapabilirsin
        SwitchTab(MissionCategory.Achievement);
        UpdateNotificationBadge();
    }

    public void UpdateUI()
    {
        CheckWeeklyReset();
        SyncPendingGameplayProgress();
        StopAllCoroutines();
        StartCoroutine(SafeRebuildRoutine());
    }

    // --- SADECE EKSİK KİMLİKLERİ TAMAMLAYAN SİSTEM ---
    private void AssignMissingIDs()
    {
        if (allWeeklyQuests != null)
        {
            for (int i = 0; i < allWeeklyQuests.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(allWeeklyQuests[i].missionID))
                    allWeeklyQuests[i].missionID = $"Weekly_{allWeeklyQuests[i].missionType}_{i}";
            }
        }

        if (allAchievements != null)
        {
            for (int i = 0; i < allAchievements.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(allAchievements[i].missionID))
                    allAchievements[i].missionID = $"Achieve_{allAchievements[i].missionType}_{i}";
            }
        }
    }

    // Oyun içindeyken (GameScene) ilerlemeleri geçici hafızaya alır
    public static void AddGameplayProgress(MissionType type, int amount)
    {
        string key = "Pending_" + type.ToString();
        int currentPending = PlayerPrefs.GetInt(key, 0);

        if (type == MissionType.ReachScore)
        {
            PlayerPrefs.SetInt(key, Mathf.Max(currentPending, amount));
        }
        else
        {
            PlayerPrefs.SetInt(key, currentPending + amount);
        }
        PlayerPrefs.Save();
        Debug.Log($"[Görev Yöneticisi] Oyun içi ilerleme alındı -> {type}: +{amount}");
    }

    // Ana menüye dönünce geçici hafızadaki verileri asıl görevlere işler
    public void SyncPendingGameplayProgress()
    {
        foreach (MissionType type in Enum.GetValues(typeof(MissionType)))
        {
            string key = "Pending_" + type.ToString();
            if (PlayerPrefs.HasKey(key))
            {
                int pendingAmount = PlayerPrefs.GetInt(key, 0);
                if (pendingAmount > 0)
                {
                    Debug.Log($"[Görev Yöneticisi] Veriler asıl görevlere işleniyor -> {type}: +{pendingAmount}");

                    bool foundInWeekly = CheckAndAddProgress(allWeeklyQuests, type, pendingAmount);
                    bool foundInAchievements = CheckAndAddProgress(allAchievements, type, pendingAmount);

                    if (!foundInWeekly && !foundInAchievements)
                    {
                        Debug.LogWarning($"[Görev Uyarısı] '{type}' tipinde bir görev bulunamadı!");
                    }
                }
                PlayerPrefs.DeleteKey(key);
            }
        }
        PlayerPrefs.Save();
    }

    // Sahnedeki MANUEL tasarımlarınızı görev verileriyle birleştiren fonksiyon
    void InitializeExistingRows()
    {
        if (isInitialized) return;

        // Haftalık Content altındaki manuel tasarımlarınızı bulur
        if (weeklyContentParent != null)
        {
            weeklyRows = weeklyContentParent.GetComponentsInChildren<MissionRowView>(true);
            for (int i = 0; i < allWeeklyQuests.Count; i++)
            {
                if (i < weeklyRows.Length)
                {
                    weeklyRows[i].Setup(allWeeklyQuests[i]);
                }
            }
        }

        // Başarım (Achievements) Content altındaki manuel tasarımlarınızı bulur
        if (achievementsContentParent != null)
        {
            achievementRows = achievementsContentParent.GetComponentsInChildren<MissionRowView>(true);
            for (int i = 0; i < allAchievements.Count; i++)
            {
                if (i < achievementRows.Length)
                {
                    achievementRows[i].Setup(allAchievements[i]);
                }
                else
                {
                    Debug.LogWarning($"[UI EKSİK] Achievements listesinde {i + 1}. görev için sahnede (Content içinde) tasarım satırı eksik! Lütfen satır kopyalayıp çoğaltın.");
                }
            }
        }

        isInitialized = true;
    }

    void CheckWeeklyReset()
    {
        int currentWeek = GetIso8601WeekOfYear(DateTime.Now);
        int savedWeek = PlayerPrefs.GetInt(LastWeekKey, -1);

        if (savedWeek != currentWeek)
        {
            Debug.Log("Yeni bir haftaya girildi! Haftalık görevler sıfırlanıyor...");

            foreach (var mission in allWeeklyQuests)
            {
                PlayerPrefs.DeleteKey("Progress_" + mission.missionID);
                PlayerPrefs.DeleteKey("Claimed_" + mission.missionID);
            }

            PlayerPrefs.SetInt(LastWeekKey, currentWeek);
            PlayerPrefs.Save();
        }

        LoadAndSyncProgress(allWeeklyQuests);
        LoadAndSyncProgress(allAchievements);
    }

    void LoadAndSyncProgress(List<Mission> missions)
    {
        if (missions == null) return;
        for (int i = 0; i < missions.Count; i++)
        {
            var m = missions[i];
            if (m == null) continue;
            m.currentValue = PlayerPrefs.GetInt("Progress_" + m.missionID, 0);
            m.isClaimed = PlayerPrefs.GetInt("Claimed_" + m.missionID, 0) == 1;
            m.isCompleted = m.currentValue >= m.targetValue;
        }
    }

    public void SwitchTab(MissionCategory category)
    {
        currentCategory = category;

        if (weeklyTabButton != null) weeklyTabButton.GetComponent<Image>().color = (category == MissionCategory.Weekly) ? activeTabColor : inactiveTabColor;
        if (achievementsTabButton != null) achievementsTabButton.GetComponent<Image>().color = (category == MissionCategory.Achievement) ? activeTabColor : inactiveTabColor;

        bool isWeekly = (category == MissionCategory.Weekly);
        if (weeklyContentParent != null) weeklyContentParent.gameObject.SetActive(isWeekly);
        if (achievementsContentParent != null) achievementsContentParent.gameObject.SetActive(!isWeekly);

        if (scrollView != null)
        {
            RectTransform activeContent = isWeekly ?
                weeklyContentParent.GetComponent<RectTransform>() :
                achievementsContentParent.GetComponent<RectTransform>();

            scrollView.content = activeContent;
            scrollView.verticalNormalizedPosition = 1f;
        }

        RefreshAndSortUI();
    }

    public void RefreshAndSortUI()
    {
        InitializeExistingRows();

        SortAndFilterRows(weeklyContentParent, weeklyRows);
        SortAndFilterRows(achievementsContentParent, achievementRows);
        UpdateNotificationBadge();

        if (weeklyContentParent != null)
        {
            RectTransform rect = weeklyContentParent.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }
        if (achievementsContentParent != null)
        {
            RectTransform rect = achievementsContentParent.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }
        if (scrollView != null && scrollView.content != null)
        {
            scrollView.content.anchoredPosition = new Vector2(0f, scrollView.content.anchoredPosition.y);
        }

        Canvas.ForceUpdateCanvases();

        if (weeklyContentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(weeklyContentParent.GetComponent<RectTransform>());
        if (achievementsContentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(achievementsContentParent.GetComponent<RectTransform>());
        if (scrollView != null && scrollView.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.content);
    }

    void SortAndFilterRows(Transform parent, MissionRowView[] rows)
    {
        if (parent == null || rows == null) return;

        List<MissionRowView> readyToClaim = new List<MissionRowView>();
        List<MissionRowView> inProgress = new List<MissionRowView>();
        List<MissionRowView> claimed = new List<MissionRowView>();

        foreach (var row in rows)
        {
            if (row == null) continue;
            Mission m = row.GetCurrentMission();
            if (m == null) continue;

            m.currentValue = PlayerPrefs.GetInt("Progress_" + m.missionID, 0);
            m.isClaimed = PlayerPrefs.GetInt("Claimed_" + m.missionID, 0) == 1;
            m.isCompleted = m.currentValue >= m.targetValue;

            row.Setup(m);

            if (m.isClaimed) claimed.Add(row);
            else if (m.isCompleted) readyToClaim.Add(row);
            else inProgress.Add(row);
        }

        int siblingIndex = 0;

        foreach (var row in readyToClaim) { row.gameObject.SetActive(true); row.transform.SetSiblingIndex(siblingIndex++); }
        foreach (var row in inProgress) { row.gameObject.SetActive(true); row.transform.SetSiblingIndex(siblingIndex++); }
        foreach (var row in claimed) { row.gameObject.SetActive(true); row.transform.SetSiblingIndex(siblingIndex++); }
    }

    public void AddProgress(MissionType type, int amount)
    {
        bool foundInWeekly = CheckAndAddProgress(allWeeklyQuests, type, amount);
        bool foundInAchievements = CheckAndAddProgress(allAchievements, type, amount);

        if (!foundInWeekly && !foundInAchievements)
        {
            Debug.LogWarning($"[Görev Uyarısı] Sahnede '{type}' tipinde tanımlanmış bir görev bulunamadı!");
        }

        RefreshAndSortUI();
    }

    bool CheckAndAddProgress(List<Mission> missionList, MissionType type, int amount)
    {
        bool foundMatch = false;
        if (missionList == null) return false;

        foreach (var mission in missionList)
        {
            if (mission == null) continue;
            if (mission.missionType == type)
            {
                foundMatch = true;
                string claimKey = "Claimed_" + mission.missionID;
                if (PlayerPrefs.GetInt(claimKey, 0) == 1) continue;

                string progressKey = "Progress_" + mission.missionID;

                if (type == MissionType.ReachScore)
                {
                    mission.CheckAbsoluteTarget(amount);
                }
                else
                {
                    mission.Progress(amount);
                }

                PlayerPrefs.SetInt(progressKey, mission.currentValue);
                Debug.Log($"[Görev Yöneticisi] {mission.description} güncellendi: {mission.currentValue}/{mission.targetValue}");
            }
        }

        PlayerPrefs.Save();
        return foundMatch;
    }

    public void ClaimReward(Mission mission)
    {
        string claimKey = "Claimed_" + mission.missionID;
        PlayerPrefs.SetInt(claimKey, 1);
        mission.isClaimed = true;
        PlayerPrefs.Save();

        if (GameManager.instance != null)
        {
            switch (mission.rewardType)
            {
                case RewardType.Coin: GameManager.instance.AddGems(mission.rewardAmount); break;
                case RewardType.Boost:
                    int currentBoosts = PlayerPrefs.GetInt("PlayerBoosts", 0);
                    currentBoosts += mission.rewardAmount;
                    PlayerPrefs.SetInt("PlayerBoosts", currentBoosts);
                    PlayerPrefs.Save();
                    GameManager.instance.currentBoostAmount = currentBoosts;
                    break;
                case RewardType.Shield: Debug.Log($"🛡️ {mission.rewardAmount} Kalkan ödülü!"); break;
            }
        }

        RefreshAndSortUI();
    }

    private IEnumerator SafeRebuildRoutine()
    {
        SwitchTab(currentCategory);
        UpdateNotificationBadge();
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (weeklyContentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(weeklyContentParent.GetComponent<RectTransform>());
        if (achievementsContentParent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(achievementsContentParent.GetComponent<RectTransform>());
        if (scrollView != null && scrollView.content != null) LayoutRebuilder.ForceRebuildLayoutImmediate(scrollView.content);

        yield return null;
        if (scrollView != null) scrollView.verticalNormalizedPosition = 1f;
    }

    public void UpdateNotificationBadge()
    {
        if (missionNotificationBadge == null) return;

        bool hasUnclaimedReward = false;

        if (allWeeklyQuests != null)
            foreach (var m in allWeeklyQuests) { if (m == null) continue; if (m.isCompleted && !m.isClaimed) { hasUnclaimedReward = true; break; } }

        if (!hasUnclaimedReward && allAchievements != null)
            foreach (var m in allAchievements) { if (m == null) continue; if (m.isCompleted && !m.isClaimed) { hasUnclaimedReward = true; break; } }

        missionNotificationBadge.SetActive(hasUnclaimedReward);
    }

    public static int GetIso8601WeekOfYear(DateTime time)
    {
        DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday) time = time.AddDays(3);
        return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    // =====================================================================
    // 🔥 SAĞ TIK KONTROLLERİ (TEST VE SIFIRLAMA İÇİN EKLENDİ) 🔥
    // Inspector'da MissionsManager scriptine sağ tıklayarak bunları çalıştırabilirsin!
    // =====================================================================

    [ContextMenu("TEST: Tüm Başarımları (Achievements) Tamamla")]
    public void TestCompleteAllAchievements()
    {
        if (allAchievements == null) return;

        foreach (var m in allAchievements)
        {
            if (!m.isClaimed)
            {
                m.currentValue = m.targetValue;
                PlayerPrefs.SetInt("Progress_" + m.missionID, m.currentValue);
            }
        }
        PlayerPrefs.Save();
        LoadAndSyncProgress(allAchievements);
        RefreshAndSortUI();
        Debug.Log("✅ [TEST] Tüm başarımlar doldu! 'TAKE' butonlarına basabilirsin.");
    }

    [ContextMenu("SIFIRLA: Sadece Görev Kayıtlarını Temizle")]
    public void ClearMissionData()
    {
        // Bu işlem paralarına (Gem) dokunmaz, sadece görevleri sıfırlar!
        if (allAchievements != null)
        {
            foreach (var m in allAchievements)
            {
                PlayerPrefs.DeleteKey("Progress_" + m.missionID);
                PlayerPrefs.DeleteKey("Claimed_" + m.missionID);
            }
        }
        if (allWeeklyQuests != null)
        {
            foreach (var m in allWeeklyQuests)
            {
                PlayerPrefs.DeleteKey("Progress_" + m.missionID);
                PlayerPrefs.DeleteKey("Claimed_" + m.missionID);
            }
        }
        foreach (MissionType type in Enum.GetValues(typeof(MissionType)))
        {
            PlayerPrefs.DeleteKey("Pending_" + type.ToString());
        }
        PlayerPrefs.Save();

        // Anlık olarak UI'ı da sıfırla
        LoadAndSyncProgress(allAchievements);
        LoadAndSyncProgress(allWeeklyQuests);
        RefreshAndSortUI();

        Debug.Log("🗑️ [SIFIRLANDI] Tüm görev ilerlemeleri ilk günkü haline döndü!");
    }
}

public static class MissionExtension
{
    public static string transformMissionID(this Mission m) => m.missionID;
}