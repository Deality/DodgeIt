using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// DRIFT BOOST (COIN RUSH) akışı:
//  1) Toplanınca: ekranda "COIN RUSH" yazısı çıkar, hız ve zorluk kilitlenir, yeni trafik spawn olmaz.
//     Yoldaki mevcut araçlar yok olmaz; normal şekilde akıp ekranın altından çıkar (oyuncu hâlâ normal kontrolde).
//  2) Son araç ekranın altından çıkınca: drift kontrolü (ekranın sağ/sol yarısına basılı tut = o yöne kay,
//     dokunma yok = dümdüz git) açılır,
//     zikzak altınlar gelmeye başlar ve boost süresi işlemeye başlar.
//  3) Süre bitince: araba en yakın şeride oturur, altın artıkları temizlenir ve kısa bir beklemeden sonra
//     trafik kilitlendiği hızla, zorluk da kaldığı yerden devam eder.
public class DriftBoostManager : MonoBehaviour
{
    public static DriftBoostManager instance;

    private enum Phase { None, WaitingForTraffic, Drifting }

    [Header("Süre")]
    [Tooltip("Drift (altın toplama) bölümü kaç saniye sürecek? Süre, son araç ekrandan çıktıktan sonra başlar.")]
    [SerializeField] private float boostDuration = 5f;
    [Tooltip("Boost bittikten sonra trafik tekrar başlamadan önceki bekleme süresi (saniye).")]
    [SerializeField] private float trafficResumeDelay = 1f;

    [Header("Trafiğin Çekilmesini Bekleme")]
    [Tooltip("Yol boş olsa bile altınlar başlamadan önce en az bu kadar beklenir (yazının okunabilmesi için).")]
    [SerializeField] private float minWaitBeforeCoins = 1f;
    [Tooltip("Güvenlik sınırı: Bu süre dolduğunda hâlâ yolda araç varsa kaldırılır ve altınlar başlar.")]
    [SerializeField] private float maxWaitForTraffic = 8f;

    [Header("COIN RUSH Yazısı")]
    [Tooltip("Boş bırakılırsa oyun sırasında skor yazısının fontuyla otomatik oluşturulur.")]
    [SerializeField] private TextMeshProUGUI coinRushText;
    [SerializeField] private string coinRushLabel = "COIN RUSH";
    [SerializeField] private Color coinRushColor = new Color(1f, 0.84f, 0.1f, 1f);
    [SerializeField] private float coinRushFontSize = 130f;
    [Tooltip("Otomatik oluşturulan yazının ekran ortasına göre dikey konumu.")]
    [SerializeField] private float coinRushPositionY = 250f;
    [SerializeField] private float coinRushPopInDuration = 0.3f;
    [SerializeField] private float coinRushHoldDuration = 1.2f;
    [SerializeField] private float coinRushFadeOutDuration = 0.4f;

    [Header("Kontrol İpucu (COIN RUSH Yazısının Altında)")]
    [Tooltip("Boş bırakılırsa oyun sırasında skor yazısının fontuyla otomatik oluşturulur.")]
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string hintLabel = "HOLD AND SWIPE";
    [SerializeField] private Color hintColor = Color.white;
    [SerializeField] private float hintFontSize = 64f;
    [Tooltip("COIN RUSH yazısının ne kadar altında duracağı (UI birimi).")]
    [SerializeField] private float hintOffsetBelowTitle = 120f;
    [SerializeField] private float hintFadeDuration = 0.2f;

    [Header("Sonuç Yazısı (Boost Sonunda Toplanan Altın)")]
    [Tooltip("Boş bırakılırsa oyun sırasında skor yazısının fontuyla otomatik oluşturulur.")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private string resultTitle = "COIN RUSH";
    [SerializeField] private Color resultColor = new Color(1f, 0.84f, 0.1f, 1f);
    [SerializeField] private float resultFontSize = 140f;
    [Tooltip("Ekran ortasına göre başlangıçtaki dikey konum.")]
    [SerializeField] private float resultPositionY = 150f;
    [SerializeField] private float resultPopInDuration = 0.25f;
    [Tooltip("Sayının 0'dan toplanan miktara yükselme süresi.")]
    [SerializeField] private float resultCountUpDuration = 0.9f;
    [SerializeField] private float resultHoldDuration = 0.6f;
    [Tooltip("Yazının kaybolurken ne kadar yukarı yükseleceği (UI birimi).")]
    [SerializeField] private float resultRiseDistance = 200f;
    [SerializeField] private float resultRiseDuration = 0.7f;
    [Tooltip("Sayı yükselirken çalan sayaç tıkları arasındaki en kısa süre (saniye).")]
    [SerializeField] private float resultTickInterval = 0.055f;
    [Header("Zikzak Altınlar")]
    [Tooltip("Boş bırakılırsa ObstacleManager'daki normal coin prefabı kullanılır.")]
    [SerializeField] private GameObject coinPrefab;
    [Tooltip("Dizideki iki altın arasındaki dikey mesafe.")]
    [SerializeField] private float coinSpacing = 12f;
    [Tooltip("Zikzağın eğimi, arabanın en yüksek drift hızının bu oranına göre ayarlanır. 1'e yakın = daha dik ve zor, düşük = daha yatık ve kolay.")]
    [SerializeField, Range(0.3f, 1f)] private float zigzagFollowFactor = 0.75f;
    [Tooltip("Boost bitmeden en az bu kadar saniye önce arabaya ulaşamayacak altınlar spawn edilmez.")]
    [SerializeField] private float coinArrivalMargin = 0.2f;

    [Header("Efektler (İsteğe Bağlı)")]
    [Tooltip("Bekleme süresi aşılır ve kalan araçlar zorla kaldırılırsa her araç için oynatılacak efekt.")]
    [SerializeField] private GameObject trafficClearEffectPrefab;
    [SerializeField] private AudioClip activateSound;

    private Phase phase = Phase.None;

    // Drift (altın) bölümü aktif mi? Zamanlayıcı çubuğu bunu kullanır.
    public bool IsActive => phase == Phase.Drifting;
    // Boost toplanmış ve henüz bitmemiş mi (bekleme veya drift)?
    public bool IsBusy => phase != Phase.None;
    public float TimeRemaining { get; private set; } = 0f;
    // Toplam drift süresi = altınların spawn çizgisinden arabaya ulaşma süresi + boostDuration (zamanlayıcı çubuğu için)
    public float Duration { get; private set; } = 1f;

    private readonly HashSet<GameObject> boostCoins = new HashSet<GameObject>();
    private Coroutine resumeTrafficCoroutine;
    private Coroutine coinRushTextCoroutine;
    private Coroutine resultTextCoroutine;
    private Coroutine hintTextCoroutine;

    private int collectedValue;
    private int collectedCount;
    public int CollectedValue => collectedValue;
    public int CollectedCount => collectedCount;
    private Camera mainCam;

    private float waitTimer;
    private float trafficCheckTimer;
    private const float TrafficCheckInterval = 0.1f;

    private float minX;
    private float maxX;
    private float nextCoinX;
    private float nextCoinY;
    private int zigzagDirection;
    private bool coinsFinished;

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Pickup toplandığında çağrılır. Başlatılamadıysa false döner.
    public bool TryActivate()
    {
        if (IsBusy) return false;

        GameManager gm = GameManager.instance;
        ObstacleManager om = ObstacleManager.instance;
        if (gm == null || om == null || CarController2D.instance == null) return false;
        if (!gm.isGameActive || gm.IsGameOver) return false;

        // Önceki boost'un bekleme süresindeyken yeni boost toplandıysa: trafik zaten kilitli, bekleme iptal
        if (resumeTrafficCoroutine != null)
        {
            StopCoroutine(resumeTrafficCoroutine);
            resumeTrafficCoroutine = null;
        }

        // Hız ve zorluk kilitlenir, yeni araç gelmez; yoldakiler normal şekilde akıp gider
        om.LockTraffic();

        phase = Phase.WaitingForTraffic;
        waitTimer = 0f;
        trafficCheckTimer = 0f;
        TimeRemaining = 0f;
        mainCam = Camera.main;

        ShowCoinRushText();

        if (activateSound != null && AudioManager.instance != null)
            AudioManager.instance.PlaySFX(activateSound);

        return true;
    }

    void Update()
    {
        if (phase == Phase.WaitingForTraffic) UpdateWaiting();
        else if (phase == Phase.Drifting) UpdateDrifting();
    }

    // --- 1) TRAFİĞİN ÇEKİLMESİNİ BEKLE ---

    void UpdateWaiting()
    {
        // Time.deltaTime -> oyun duraklatılınca bekleme de durur
        waitTimer += Time.deltaTime;
        if (waitTimer < minWaitBeforeCoins) return;

        ObstacleManager om = ObstacleManager.instance;
        if (om == null || mainCam == null) return;

        if (waitTimer >= maxWaitForTraffic)
        {
            // Güvenlik: takılı kalan bir araç varsa sonsuza kadar beklemeyelim
            om.ClearTraffic(trafficClearEffectPrefab);
            StartDriftPhase();
            return;
        }

        // Her karede tüm araçları aramamak için kısa aralıklarla kontrol et
        trafficCheckTimer -= Time.deltaTime;
        if (trafficCheckTimer > 0f) return;
        trafficCheckTimer = TrafficCheckInterval;

        float screenBottomY = mainCam.transform.position.y - mainCam.orthographicSize;
        if (!om.HasTrafficAbove(screenBottomY)) StartDriftPhase();
    }

    // --- 2) DRIFT + ALTINLAR ---

    void StartDriftPhase()
    {
        CarController2D car = CarController2D.instance;
        if (car == null)
        {
            CancelBoost();
            return;
        }

        // Öfke Modu ile aynı anda çalışmasın
        if (car.isBoostActive) car.ForceEndBoost();

        car.BeginDrift();
        if (!car.IsDrifting)
        {
            CancelBoost();
            return;
        }

        phase = Phase.Drifting;

        minX = car.DriftMinX;
        maxX = car.DriftMaxX;

        // İlk altın ekranın üstünden gelirken geçen süre boost süresinden yenmesin:
        // altın toplama kısmı tam boostDuration sürsün diye yolculuk süresini ekliyoruz
        float speed = Mathf.Max(ObstacleManager.scrollSpeed, 0.1f);
        float travelTime = Mathf.Max(0f, (GetSpawnY() - car.transform.position.y) / speed);
        Duration = boostDuration + travelTime;
        TimeRemaining = Duration;

        // Altınlar engellerle aynı yerden, ekranın üstündeki spawn çizgisinden gelir (ekranın ortasında belirmez).
        // Zikzak arabanın şeridinden başlar, önce sağa (basılı tutma yönü) gider.
        nextCoinX = Mathf.Clamp(car.transform.position.x, minX, maxX);
        nextCoinY = GetSpawnY();
        zigzagDirection = nextCoinX < maxX - 0.01f ? 1 : -1;
        coinsFinished = false;

        // Bu boost'ta toplananların sayımı sıfırdan başlar
        collectedValue = 0;
        collectedCount = 0;

        SpawnPendingCoins();

        // Yol boşaldı, drift kontrolü açıldı: oyuncu ekrana basana kadar nasıl oynanacağını göster
        ShowHintText();
    }

    // Engellerin spawn olduğu yükseklik: kameranın üst kenarı + ObstacleManager.spawnYOffset
    float GetSpawnY()
    {
        float offset = ObstacleManager.instance != null ? ObstacleManager.instance.spawnYOffset : 0f;
        return mainCam.transform.position.y + mainCam.orthographicSize + offset;
    }

    void UpdateDrifting()
    {
        // Time.deltaTime kullanıldığı için oyun duraklatılınca (timeScale = 0) sayaç da durur
        TimeRemaining -= Time.deltaTime;

        // Bir sonraki altının "sanal" konumu yolla birlikte aşağı kayar
        nextCoinY -= ObstacleManager.scrollSpeed * Time.deltaTime;
        SpawnPendingCoins();

        if (TimeRemaining <= 0f) EndBoost();
    }

    // --- 3) BİTİŞ ---

    void EndBoost()
    {
        phase = Phase.None;
        TimeRemaining = 0f;

        // Gizli başarım: Coin Rush süresi, oyuncu hiç drift yapmadan (ekrana dokunmadan) doldu.
        // Yalnızca süre doğal olarak bitince sayılır; kaza/iptal (CancelBoost) buraya gelmez.
        if (CarController2D.instance != null && !CarController2D.instance.SteeredDuringDrift)
            MissionsManager.AddGameplayProgress(MissionType.FinishCoinRushWithoutDrifting, 1);

        if (CarController2D.instance != null) CarController2D.instance.EndDrift();
        ClearBoostCoins();

        // Boost boyunca toplanan altın miktarını göster
        ShowResultText();

        if (resumeTrafficCoroutine != null) StopCoroutine(resumeTrafficCoroutine);
        resumeTrafficCoroutine = StartCoroutine(ResumeTrafficAfterDelay());
    }

    IEnumerator ResumeTrafficAfterDelay()
    {
        // WaitForSeconds ölçeklenmiş zaman kullanır -> oyun duraklatılırsa bekleme de durur
        yield return new WaitForSeconds(trafficResumeDelay);

        if (ObstacleManager.instance != null) ObstacleManager.instance.UnlockTraffic();
        resumeTrafficCoroutine = null;
    }

    // Kaza / oyun sonu / reklamla devam: boost'a ait her şeyi anında temizler ve trafiği serbest bırakır
    public void CancelBoost()
    {
        if (resumeTrafficCoroutine != null)
        {
            StopCoroutine(resumeTrafficCoroutine);
            resumeTrafficCoroutine = null;
        }

        if (phase == Phase.Drifting && CarController2D.instance != null) CarController2D.instance.EndDrift();
        phase = Phase.None;

        TimeRemaining = 0f;
        ClearBoostCoins();
        HideCoinRushText();
        HideHintText();
        HideResultText();

        if (ObstacleManager.instance != null) ObstacleManager.instance.UnlockTraffic();
    }

    // --- ZİKZAK ALTINLAR ---

    void SpawnPendingCoins()
    {
        if (coinsFinished) return;

        ObstacleManager om = ObstacleManager.instance;
        if (om == null || mainCam == null || CarController2D.instance == null) return;

        GameObject prefab = coinPrefab != null ? coinPrefab : om.coinPrefab;
        if (prefab == null) return;

        float spawnY = GetSpawnY();
        float carY = CarController2D.instance.transform.position.y;
        float speed = Mathf.Max(ObstacleManager.scrollSpeed, 0.1f);

        // Zikzak eğimi: araba bu eğimi en yüksek drift hızının zigzagFollowFactor kadarıyla takip edebilir.
        // Hıza göre hesaplandığı için kilitlenen hız ne olursa olsun dizi takip edilebilir kalır.
        float carDriftSpeed = CarController2D.instance.DriftSpeed;
        float xStep = carDriftSpeed * zigzagFollowFactor * coinSpacing / speed;

        while (nextCoinY <= spawnY)
        {
            // Boost bitmeden arabaya ulaşamayacaksa artık altın çıkarma
            float arrivalTime = (nextCoinY - carY) / speed;
            if (arrivalTime > TimeRemaining - coinArrivalMargin)
            {
                coinsFinished = true;
                return;
            }

            GameObject coin = PoolManager.Spawn(prefab, new Vector3(nextCoinX, nextCoinY, 0f), Quaternion.identity);
            if (coin != null) boostCoins.Add(coin);

            nextCoinY += coinSpacing;
            nextCoinX += zigzagDirection * xStep;

            // Yol sınırında sekerek yön değiştir
            if (nextCoinX > maxX) { nextCoinX = 2f * maxX - nextCoinX; zigzagDirection = -1; }
            else if (nextCoinX < minX) { nextCoinX = 2f * minX - nextCoinX; zigzagDirection = 1; }
            nextCoinX = Mathf.Clamp(nextCoinX, minX, maxX);
        }
    }

    void ClearBoostCoins()
    {
        // Toplanan altınlar zaten havuza dönmüş (pasif) olur; yalnızca hâlâ yolda olanları kaldır
        foreach (GameObject coin in boostCoins)
        {
            if (coin != null && coin.activeInHierarchy) PoolManager.Despawn(coin);
        }
        boostCoins.Clear();
    }

    // --- COIN RUSH YAZISI ---

    void ShowCoinRushText()
    {
        if (coinRushText == null) coinRushText = CreateOverlayText("CoinRushText", coinRushPositionY, coinRushFontSize, coinRushColor, 250f);
        if (coinRushText == null) return;

        if (coinRushTextCoroutine != null) StopCoroutine(coinRushTextCoroutine);
        coinRushTextCoroutine = StartCoroutine(CoinRushTextRoutine());
    }

    void HideCoinRushText()
    {
        if (coinRushTextCoroutine != null)
        {
            StopCoroutine(coinRushTextCoroutine);
            coinRushTextCoroutine = null;
        }
        if (coinRushText != null) coinRushText.gameObject.SetActive(false);
    }

    IEnumerator CoinRushTextRoutine()
    {
        Transform tr = coinRushText.transform;
        coinRushText.text = coinRushLabel;
        coinRushText.gameObject.SetActive(true);
        tr.SetAsLastSibling();

        // Ölçeklenmiş zaman: oyun duraklatılırsa animasyon da durur
        float t = 0f;
        while (t < coinRushPopInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(coinRushPopInDuration, 0.01f));
            tr.localScale = Vector3.one * EaseOutBack(p);
            SetTextAlpha(p);
            yield return null;
        }

        t = 0f;
        while (t < coinRushHoldDuration)
        {
            t += Time.deltaTime;
            tr.localScale = Vector3.one * (1f + Mathf.Sin(t * 10f) * 0.04f);
            SetTextAlpha(1f);
            yield return null;
        }

        t = 0f;
        while (t < coinRushFadeOutDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(coinRushFadeOutDuration, 0.01f));
            tr.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, p);
            SetTextAlpha(1f - p);
            yield return null;
        }

        coinRushText.gameObject.SetActive(false);
        coinRushTextCoroutine = null;
    }

    void SetTextAlpha(float a)
    {
        Color c = coinRushText.color;
        c.a = a;
        coinRushText.color = c;
    }

    // --- KONTROL İPUCU: "HOLD AND SWIPE" ---

    void ShowHintText()
    {
        if (hintText == null) hintText = CreateOverlayText("CoinRushHintText", coinRushPositionY - hintOffsetBelowTitle, hintFontSize, hintColor, 120f);
        if (hintText == null) return;

        if (hintTextCoroutine != null) StopCoroutine(hintTextCoroutine);
        hintTextCoroutine = StartCoroutine(HintTextRoutine());
    }

    void HideHintText()
    {
        if (hintTextCoroutine != null)
        {
            StopCoroutine(hintTextCoroutine);
            hintTextCoroutine = null;
        }
        if (hintText != null) hintText.gameObject.SetActive(false);
    }

    IEnumerator HintTextRoutine()
    {
        Transform tr = hintText.transform;
        hintText.text = hintLabel;
        hintText.gameObject.SetActive(true);
        tr.SetAsLastSibling();

        // Oyuncu ekrana basana (ya da boost bitene) kadar hafifçe nabız atarak durur
        float t = 0f;
        float alpha = 0f;
        while (phase == Phase.Drifting && CarController2D.instance != null && !CarController2D.instance.SteeredDuringDrift)
        {
            t += Time.deltaTime;
            alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime / Mathf.Max(hintFadeDuration, 0.01f));
            tr.localScale = Vector3.one * (1f + Mathf.Sin(t * 6f) * 0.05f);
            SetAlpha(hintText, alpha);
            yield return null;
        }

        while (alpha > 0f)
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime / Mathf.Max(hintFadeDuration, 0.01f));
            SetAlpha(hintText, alpha);
            yield return null;
        }

        hintText.gameObject.SetActive(false);
        hintTextCoroutine = null;
    }

    static float EaseOutBack(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);
    }

    // --- SONUÇ YAZISI: "COIN RUSH +N" ---

    // Collectable, bir altın toplandığında çağırır. Yalnızca bu boost'un spawn ettiği altınlar sayılır.
    public void NotifyCoinCollected(GameObject coin, int value)
    {
        if (phase != Phase.Drifting || !boostCoins.Contains(coin)) return;
        collectedValue += value;
        collectedCount++;
    }

    void ShowResultText()
    {
        if (resultText == null) resultText = CreateOverlayText("CoinRushResultText", resultPositionY, resultFontSize, resultColor, 400f);
        if (resultText == null) return;

        if (resultTextCoroutine != null) StopCoroutine(resultTextCoroutine);
        resultTextCoroutine = StartCoroutine(ResultTextRoutine(collectedValue));
    }

    void HideResultText()
    {
        if (resultTextCoroutine != null)
        {
            StopCoroutine(resultTextCoroutine);
            resultTextCoroutine = null;
        }
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    void SetResultText(int amount)
    {
        resultText.text = $"<size=55%>{resultTitle}</size>\n+{amount}";
    }

    IEnumerator ResultTextRoutine(int total)
    {
        RectTransform rt = resultText.rectTransform;
        rt.anchoredPosition = new Vector2(0f, resultPositionY);
        resultText.gameObject.SetActive(true);
        rt.SetAsLastSibling();
        SetResultText(0);
        SetAlpha(resultText, 1f);

        // Ölçeklenmiş zaman: oyun duraklatılırsa animasyon da durur
        // 1) Beliriş
        float t = 0f;
        while (t < resultPopInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(resultPopInDuration, 0.01f));
            rt.localScale = Vector3.one * EaseOutBack(p);
            SetAlpha(resultText, p);
            yield return null;
        }

        // 2) Sayı 0'dan toplanan miktara kadar yükselir; her artışta küçük bir "pat" büyümesi ve sayaç tıkı
        int shown = 0;
        float lastTickTime = -1f;
        t = 0f;
        while (t < resultCountUpDuration && total > 0)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(resultCountUpDuration, 0.01f));
            int value = Mathf.RoundToInt(total * (1f - Mathf.Pow(1f - p, 3f))); // sona doğru yavaşlar
            if (value != shown)
            {
                shown = value;
                SetResultText(shown);
                rt.localScale = Vector3.one * 1.12f;

                // Sayı hızlı artarken her karede tık çalmasın; sayım yavaşladıkça tıklar da seyrekleşir
                if (t - lastTickTime >= resultTickInterval && AudioManager.instance != null)
                {
                    lastTickTime = t;
                    AudioManager.instance.PlayCoinRushCountTick();
                }
            }
            rt.localScale = Vector3.Lerp(rt.localScale, Vector3.one, Time.deltaTime * 12f);
            yield return null;
        }
        SetResultText(total);
        if (total > 0 && AudioManager.instance != null) AudioManager.instance.PlayCoinRushTotal();

        // 3) Son değerde kısa bir vurgu ve bekleme
        t = 0f;
        while (t < resultHoldDuration)
        {
            t += Time.deltaTime;
            float punch = 1f + Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI) * 0.2f;
            rt.localScale = Vector3.one * punch;
            yield return null;
        }
        rt.localScale = Vector3.one;

        // 4) Yukarı doğru yükselip kaybolur
        t = 0f;
        Vector2 start = rt.anchoredPosition;
        while (t < resultRiseDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(resultRiseDuration, 0.01f));
            rt.anchoredPosition = start + Vector2.up * (resultRiseDistance * p * (2f - p)); // ease-out
            SetAlpha(resultText, 1f - p * p);
            yield return null;
        }

        resultText.gameObject.SetActive(false);
        resultTextCoroutine = null;
    }

    static void SetAlpha(TextMeshProUGUI text, float a)
    {
        Color c = text.color;
        c.a = a;
        text.color = c;
    }

    // Sahnede hazır bir yazı atanmadıysa, oyun içi skor yazısının Canvas'ına ve fontuna göre bir tane oluşturur
    TextMeshProUGUI CreateOverlayText(string objName, float positionY, float fontSize, Color color, float height)
    {
        TextMeshProUGUI reference = GameManager.instance != null ? GameManager.instance.scoreText : null;
        Canvas canvas = reference != null && reference.canvas != null ? reference.canvas.rootCanvas : FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        var go = new GameObject(objName, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(canvas.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, positionY);
        rt.sizeDelta = new Vector2(1000f, height);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (reference != null)
        {
            text.font = reference.font;
            text.fontSharedMaterial = reference.fontSharedMaterial;
        }
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = color;
        text.raycastTarget = false; // dokunmaları engellemesin (swipe / drift için basılı tutma)

        go.SetActive(false);
        return text;
    }
}
