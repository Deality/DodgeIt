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

    [Header("Drift Kilidi (öğretici bitene kadar)")]
    [Tooltip("Drift kartını kaplayan kilit katmanı")]
    public CanvasGroup driftLockOverlay;
    [Tooltip("Kilit ikonu (gövde + halka) - açılırken sallanıp büyüyerek kaybolur")]
    public RectTransform lockIcon;
    [Tooltip("Kilidin halkası - açılırken yukarı kalkar")]
    public RectTransform lockShackle;
    public TextMeshProUGUI lockText;
    [Tooltip("Kilitliyken gizlenen kart öğeleri (açıklama, hak rozeti, sayaç). Açılınca yumuşakça belirir.")]
    public CanvasGroup[] hiddenWhileLocked;
    public string lockedMessage = "FINISH THE TUTORIAL TO UNLOCK";
    public string unlockedMessage = "UNLOCKED!";

    [Header("Kilit Açılışı Odak Ekranı")]
    [Tooltip("Kilit açılırken ekranın geri kalanını karartan perdenin rengi")]
    public Color focusDimColor = new Color(0f, 0f, 0f, 0.8f);

    // Oyuncu Drift'i kilitli gördüyse 1 (kilidi hiç görmemiş eski oyunculara açılma animasyonu oynatılmaz)
    private const string DriftLockSeenKey = "DriftLockSeen";
    // Oyuncu kilit açılışından sonra Drift kartına dokunduysa 1 (o zamana kadar animasyon her açılışta tekrar oynar)
    private const string DriftUnlockShownKey = "DriftUnlockShown";
    private Coroutine unlockRoutine;
    private Vector2 shackleClosedPos;
    private bool shackleCached;

    // Odak ekranı: Drift kartı dışındaki her şeyi karartır ve dokunuşları keser
    private Image focusDim;
    private int driftCardSibling = -1;
    private bool focusActive;

    void OnEnable()
    {
        Refresh();
    }

    void OnDisable()
    {
        // Panel animasyon ya da odak sırasında kapandıysa her şey eski hâline dönsün (kilit açılışı bir dahaki sefere tekrar oynar)
        if (unlockRoutine != null)
        {
            StopCoroutine(unlockRoutine);
            unlockRoutine = null;
        }
        EndFocus();
    }

    bool DriftLocked => !TutorialManager.IsMainTutorialDone;
    bool UnlockPending => !DriftLocked && PlayerPrefs.GetInt(DriftLockSeenKey, 0) == 1 && PlayerPrefs.GetInt(DriftUnlockShownKey, 0) == 0;

    public void Refresh()
    {
        RefreshClassicCarIcon();

        // Hak yazısı kilitliyken de güncel kalsın (kilit açılırken eski yazı görünmesin)
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

        if (unlockRoutine != null || focusActive) return; // açılma animasyonu kartı kendisi yönetiyor

        // Kilitli ya da açılışı henüz gösterilmemiş: kart kilitli görünür, animasyonu START (OpenModeSelect) başlatır
        if (DriftLocked || (UnlockPending && driftLockOverlay != null))
        {
            if (DriftLocked) PlayerPrefs.SetInt(DriftLockSeenKey, 1);
            SetLockOverlay(true);
            if (lockText != null) lockText.text = lockedMessage;
            if (driftButton != null) driftButton.interactable = false;
            if (driftButtonImage != null) driftButtonImage.color = driftEmptyColor;
            return;
        }
        SetLockOverlay(false);

        if (driftButton != null) driftButton.interactable = left > 0;
        if (driftButtonImage != null) driftButtonImage.color = left > 0 ? driftReadyColor : driftEmptyColor;
    }

    // UIManager.OpenModeSelect çağırır: öğretici yeni bittiyse panel kayarak yerine oturunca kilit açılışı oynar
    public void PlayUnlockIfPending()
    {
        if (!UnlockPending || driftLockOverlay == null || unlockRoutine != null || focusActive || !isActiveAndEnabled) return;
        unlockRoutine = StartCoroutine(UnlockSequence());
    }

    void SetCardContentAlpha(float a)
    {
        if (hiddenWhileLocked == null) return;
        foreach (var g in hiddenWhileLocked) if (g != null) g.alpha = a;
    }

    void SetLockOverlay(bool on)
    {
        if (driftLockOverlay == null) return;
        SetCardContentAlpha(on ? 0f : 1f);
        driftLockOverlay.gameObject.SetActive(on);
        driftLockOverlay.alpha = 1f;
        if (lockIcon != null)
        {
            lockIcon.localScale = Vector3.one;
            lockIcon.localRotation = Quaternion.identity;
        }
        if (lockShackle != null)
        {
            if (!shackleCached) { shackleClosedPos = lockShackle.anchoredPosition; shackleCached = true; }
            lockShackle.anchoredPosition = shackleClosedPos;
        }
        if (lockText != null) lockText.transform.localScale = Vector3.one;
    }

    // Ekran kararır, yalnızca Drift kartı aydınlık kalır; kilit sallanır, halkası açılır, kilit büyüyerek kaybolur,
    // kart renklenir ve oyuncu Drift kartına dokunana kadar nabız gibi atar
    System.Collections.IEnumerator UnlockSequence()
    {
        SetLockOverlay(true);
        if (lockText != null) lockText.text = lockedMessage;
        if (driftButton != null) driftButton.interactable = false;
        if (driftButtonImage != null) driftButtonImage.color = driftEmptyColor;
        Transform card = driftButton != null ? driftButton.transform : null;

        // Panel kayarak yerine otursun
        var rect = (RectTransform)transform;
        float wait = 0f;
        while (rect.anchoredPosition.sqrMagnitude > 1f && wait < 2f) { wait += Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(0.15f);

        // 0) Ekran kararır
        BeginFocus();
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            SetDimAlpha(Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        SetDimAlpha(1f);
        yield return new WaitForSecondsRealtime(0.25f);

        // 1) Sallanma
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float decay = 1f - t / 0.5f;
            if (lockIcon != null) lockIcon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 45f) * 14f * decay);
            yield return null;
        }
        if (lockIcon != null) lockIcon.localRotation = Quaternion.identity;

        // 2) Halka açılır, yazı "UNLOCKED!" olur
        if (AudioManager.instance != null) AudioManager.instance.PlayPurchaseSound();
        if (lockText != null) lockText.text = unlockedMessage;
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.2f);
            float ease = 1f - (1f - p) * (1f - p);
            if (lockShackle != null) lockShackle.anchoredPosition = shackleClosedPos + new Vector2(0f, 30f * ease);
            if (lockText != null) lockText.transform.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.45f);

        // 3) Kilit büyüyerek kaybolur, kart renklenir
        t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / 0.35f);
            if (lockIcon != null) lockIcon.localScale = Vector3.one * (1f + 0.4f * p);
            if (driftLockOverlay != null) driftLockOverlay.alpha = 1f - p;
            SetCardContentAlpha(p);
            if (driftButtonImage != null) driftButtonImage.color = Color.Lerp(driftEmptyColor, driftReadyColor, p);
            yield return null;
        }
        SetLockOverlay(false);

        // Hak yoksa (normalde yeni oyuncuda olmaz) oyuncuyu bekletmeden ekranı aç
        if (GameModeState.DriftPlaysLeft <= 0 && !GameModeState.NextDriftPlayNeedsAd)
        {
            MarkUnlockShown();
            unlockRoutine = null;
            EndFocus();
            Refresh();
            yield break;
        }

        // 4) Kart dokunulabilir olur ve dokunulana kadar nabız gibi atar
        if (driftButton != null) driftButton.interactable = true;
        unlockRoutine = null;
        t = 0f;
        while (focusActive && card != null)
        {
            t += Time.unscaledDeltaTime;
            float beat = Mathf.Abs(Mathf.Sin(t * 3.2f));
            card.localScale = Vector3.one * (1f + 0.05f * beat);
            yield return null;
        }
        if (card != null) card.localScale = Vector3.one;
    }

    void MarkUnlockShown()
    {
        PlayerPrefs.SetInt(DriftUnlockShownKey, 1);
        PlayerPrefs.Save();
    }

    void BeginFocus()
    {
        if (driftButton == null) return;
        RectTransform card = (RectTransform)driftButton.transform;

        if (focusDim == null)
        {
            var go = new GameObject("FocusDim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            // Paneli taşacak kadar büyük: kenarlarda karartılmamış şerit kalmasın
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-600f, -600f); rt.offsetMax = new Vector2(600f, 600f);
            focusDim = go.GetComponent<Image>();
            focusDim.raycastTarget = true; // kart dışındaki dokunuşları keser (geri, Classic)
        }
        driftCardSibling = card.GetSiblingIndex();
        focusDim.gameObject.SetActive(true);
        focusDim.transform.SetAsLastSibling();
        card.SetAsLastSibling(); // kart perdenin önünde kalır
        SetDimAlpha(0f);
        focusActive = true;
    }

    void SetDimAlpha(float a)
    {
        if (focusDim == null) return;
        Color c = focusDimColor;
        c.a *= a;
        focusDim.color = c;
    }

    void EndFocus()
    {
        if (!focusActive) return;
        focusActive = false;
        if (focusDim != null) focusDim.gameObject.SetActive(false);
        if (driftButton != null)
        {
            driftButton.transform.localScale = Vector3.one;
            if (driftCardSibling >= 0) driftButton.transform.SetSiblingIndex(driftCardSibling);
        }
        driftCardSibling = -1;
    }

    // Classic kartında oyuncunun markette seçtiği araba görünür
    void RefreshClassicCarIcon()
    {
        if (classicCarIcon == null || CarDatabase.instance == null) return;
        GameObject car = CarDatabase.instance.GetCar(PlayerPrefs.GetInt("Selected_Car", 0));
        var sr = car != null ? car.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (sr != null && sr.sprite != null)
        {
            classicCarIcon.sprite = sr.sprite;
            // Bazı prefablar görseli Flip X/Y ile çevirir (örn. varsayılan araba); kartta da aynı yöne baksın
            Vector3 s = classicCarIcon.rectTransform.localScale;
            classicCarIcon.rectTransform.localScale = new Vector3(Mathf.Abs(s.x) * (sr.flipX ? -1f : 1f), Mathf.Abs(s.y) * (sr.flipY ? -1f : 1f), s.z);
        }
    }

    public void PlayClassic()
    {
        if (UIManager.instance != null) UIManager.instance.StartGameInMode(GameMode.Classic);
    }

    private bool waitingForAd; // reklam sürerken butona tekrar basılırsa ikinci reklam açılmasın

    public void PlayDrift()
    {
        if (waitingForAd || unlockRoutine != null) return;

        // Kilit açılışından sonra ilk dokunuş: açılış tamamlandı, karartma kalkar
        if (focusActive)
        {
            MarkUnlockShown();
            EndFocus();
        }

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
