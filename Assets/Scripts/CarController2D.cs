using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class CarController2D : MonoBehaviour
{
    public static CarController2D instance;

    [Header("Hareket Ayarları")]
    public float laneDistance = 30f;
    public float moveSpeed = 50f;

    [Header("Boost (Öfke Modu) Ayarları")]
    public float forwardOffset = 5f;
    public float forwardSpeed = 10f;

    [Tooltip("Öfke Modu kaç saniye sürecek?")]
    public float boostDuration = 3.0f;

    [Tooltip("Öfke Modu bittikten sonra tekrar kullanabilmek için gereken bekleme süresi")]
    public float boostCooldown = 5.0f;

    [Tooltip("Öfke Modu bittikten sonra arabanın normal hıza düşene kadar kaza yapmamasını sağlayan gizli koruma süresi.")]
    public float boostGraceDuration = 1.5f;
    [HideInInspector] public bool isBoostGracePeriod = false;

    [HideInInspector] public bool isBoostOnCooldown = false;
    [HideInInspector] public float currentCooldownTimer = 0f;

    [Header("Boost Görsel Efektleri")]
    public ParticleSystem frontFireEffect;
    public float maxShakeMagnitude = 0.3f;

    [Header("Şerit Değişimi Lastik İzi Ayarları")]
    [Tooltip("Atanmazsa Resources/Materials/TireTrackTrail otomatik yüklenir.")]
    public Material tireTrackMaterial;
    [Tooltip("Araba sprite'ının gövde genişliği ~6.8 birim (kök transform ölçeği 6.8x, ham sprite 1 birim). Varsayılanlar buna göre ayarlandı; farklı boyutlu araba prefabları için Inspector'dan ince ayar yapılabilir.")]
    public float rearWheelOffsetX = 2.2f;
    public float rearWheelOffsetY = -6.5f;
    public float tireTrackWidth = 0.7f;
    public int tireTrackSortingOrder = 1;

    private static GameObject tireMarkTemplate;
    private Coroutine leftTireMarkRoutine;
    private Coroutine rightTireMarkRoutine;

    [HideInInspector] public bool isBoostActive = false;
    [HideInInspector] public float currentBoostTimer = 0f;

    private float lastTapTime = 0f;
    private const float doubleTapThreshold = 0.4f;

    private Vector2 mouseStartPos;
    private bool isMouseDragging = false;
    private Dictionary<int, Vector2> touchStartPositions = new Dictionary<int, Vector2>();
    // Swipe'ı zaten tetiklemiş veya UI üzerinde başlamış parmaklar; kaldırılana kadar tekrar işlenmez
    private readonly HashSet<int> handledTouchIds = new HashSet<int>();

    [Header("Hassasiyet Ayarları")]
    public float swipeRange = 50f;

    [Header("Drift Boost Ayarları")]
    [Tooltip("Drift sırasında ulaşılan en yüksek yatay hız (birim/sn).")]
    [SerializeField] private float driftSpeed = 70f;
    [Tooltip("Yön değiştirirken yatay hızın ne kadar çabuk değiştiği (birim/sn²). Düşük = daha kaygan.")]
    [SerializeField] private float driftAcceleration = 350f;
    [Tooltip("Drift sırasında arabanın yana dönme açısı (derece). Araba drift yönüne doğru bu açıyla yan durur.")]
    [SerializeField] private float driftMaxTilt = 25f;
    [Tooltip("Eğilme açısının hedefe ne kadar hızlı yumuşakça ulaştığı.")]
    [SerializeField] private float driftTiltSmoothing = 10f;
    [Tooltip("Dış şerit merkezlerinin ötesine ne kadar kayılabilsin? (0 = dış şerit merkezleri sınırdır)")]
    [SerializeField] private float driftBoundsPadding = 0f;

    [Header("Drift Lastik İzi")]
    [Tooltip("İz noktaları arasındaki en az mesafe. Küçük = daha pürüzsüz kavis.")]
    [SerializeField] private float driftMarkPointSpacing = 1.5f;
    [Tooltip("Tek bir iz parçasındaki en fazla nokta; dolunca iz kesintisiz olarak yeni bir parçayla devam eder.")]
    [SerializeField] private int driftMarkMaxPoints = 60;

    [Header("Drift Dumanı")]
    [Tooltip("Boş bırakılırsa Resources/Materials/DriftSmoke (beyaz bulut) kullanılır.")]
    [SerializeField] private Material driftSmokeMaterial;
    [Tooltip("Tam yan kayarken tekerlek başına saniyede çıkan duman bulutu sayısı.")]
    [SerializeField] private float driftSmokeRate = 55f;
    [Tooltip("Duman bulutlarının başlangıç boyutu (dünya birimi).")]
    [SerializeField] private float driftSmokeSize = 4f;
    [Tooltip("Bir duman bulutunun ekranda kalma süresi (saniye).")]
    [SerializeField] private float driftSmokeLifetime = 0.7f;
    [Tooltip("Dumanın yanlara savrulma hızı.")]
    [SerializeField] private float driftSmokeSpread = 3f;
    [SerializeField] private Color driftSmokeColor = new Color(0.86f, 0.86f, 0.88f, 0.65f);
    [Tooltip("Duman arabanın altında, lastik izinin üstünde kalsın diye.")]
    [SerializeField] private int driftSmokeSortingOrder = 1;

    private ParticleSystem leftDriftSmoke;
    private ParticleSystem rightDriftSmoke;

    public bool IsDrifting { get; private set; } = false;
    // Son (ya da süren) drift boyunca oyuncu ekrana basıp yön verdiyse true; BeginDrift'te sıfırlanır
    public bool SteeredDuringDrift { get; private set; } = false;
    public float DriftSpeed => driftSpeed;
    private float driftVelocity = 0f;
    private float driftSteerDirection = 0f;
    private DriftMark leftDriftMark;
    private DriftMark rightDriftMark;

    // Drift boyunca tekerleği takip eden, yolla birlikte kayan tek bir iz parçası
    private class DriftMark
    {
        public LineRenderer line;
        public Vector3[] points;
        public int count;
    }
    private float currentTilt = 0f;
    private bool isTiltApplied = false;
    private readonly Dictionary<int, int> driftHoldFingers = new Dictionary<int, int>(); // parmak id -> basılma sırası
    private readonly List<int> releasedFingerIds = new List<int>();
    private int driftFingerCounter = 0;
    private readonly HashSet<int> driftIgnoredFingers = new HashSet<int>();
    private readonly List<int> pressedFingerIds = new List<int>();
    private bool driftMouseStartedOverUI = false;

    private int currentLane = 1;
    private float targetX;
    private float centerLaneX;
    private float logicalX;

    private float fixedY;
    private float targetY;
    private float currentY;

    private int controlMode = 0;
    private const string ControlModeKey = "ControlMode";

    private Animator carAnimator;
    private bool isInitialized = false;

    private Camera mainCam;
    private Vector3 originalCamPos;
    private bool camPosSaved = false;

    void Awake()
    {
        if (instance == null) instance = this;

        if (frontFireEffect == null || !frontFireEffect.gameObject.scene.IsValid())
        {
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in allParticles)
            {
                if (ps.gameObject.name.Contains("FrontFire") || ps.gameObject.name.Contains("Fire"))
                {
                    frontFireEffect = ps;
                    break;
                }
            }
        }

        if (frontFireEffect != null)
        {
            frontFireEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            frontFireEffect.gameObject.SetActive(false);
        }

        if (tireTrackMaterial == null)
        {
            tireTrackMaterial = Resources.Load<Material>("Materials/TireTrackTrail");
        }
    }

    // 🔥 ÖNEMLİ: Arabaya bağlı bir TrailRenderer KULLANMIYORUZ. Bu oyunda araba neredeyse
    // sabit durur, hareket illüzyonu yolun/zeminin aşağı kaymasıyla (Scroller, ObstacleManager.scrollSpeed)
    // yaratılır. Arabaya bağlı bir trail'in zaten çizilmiş noktaları arabayla birlikte SABİT kalır ve
    // yolla birlikte aşağı kaymaz. Bunun yerine, diğer tüm zemin objeleri (engeller, yol parçaları) gibi
    // şerit değişimi sırasında iz izlerini WORLD SPACE'te tek seferlik bir eğrisel çizgi (LineRenderer)
    // olarak oluşturup üzerine Scroller ekliyoruz; böylece iz de yolla birlikte kayıp ekrandan çıkınca
    // kendini yok eder. LineRenderer.useWorldSpace = false olduğu için noktalar objenin LOCAL uzayında
    // tutulur ve Scroller'ın transform.Translate çağrısı çizginin TAMAMINI birlikte aşağı kaydırır.
    static GameObject GetTireMarkTemplate()
    {
        if (tireMarkTemplate != null) return tireMarkTemplate;

        tireMarkTemplate = new GameObject("TireMarkTemplate");
        tireMarkTemplate.SetActive(false);
        tireMarkTemplate.AddComponent<LineRenderer>();
        tireMarkTemplate.AddComponent<TireMarkScroller>();
        Object.DontDestroyOnLoad(tireMarkTemplate);

        return tireMarkTemplate;
    }

    void SpawnTireMarks(float fromX, float toX)
    {
        if (tireTrackMaterial == null) return;

        // İz, şerit değişimi süresi boyunca yolun ne kadar kaydığı kadar UZUNLUKTA (dikey),
        // ve şeritler arası yatay mesafe kadar EĞİMLİ (yanal) çiziliyor -> "dikey ama az kavisli" görünüm.
        // İz, TireMarkScroller ile YOLUN hızında (ObstacleManager.scrollSpeed * scrollFactor)
        // kayıyor; bu yüzden izin baked uzunluğu da engellerin tam hızı yerine yolun gerçek
        // kayma hızına göre hesaplanmalı. Aksi halde iz, yoldan biraz daha hızlı/uzun görünür.
        float roadFactor = InfiniteRoad2D.Instance != null ? InfiniteRoad2D.Instance.scrollFactor : 1f;
        float swipeDuration = laneDistance / Mathf.Max(moveSpeed, 0.01f);
        float forwardTravel = Mathf.Max(ObstacleManager.scrollSpeed * roadFactor * swipeDuration, tireTrackWidth * 4f);
        float worldY = transform.position.y + rearWheelOffsetY;

        SpawnTireMark(fromX - rearWheelOffsetX, toX - rearWheelOffsetX, worldY, forwardTravel, swipeDuration, ref leftTireMarkRoutine);
        SpawnTireMark(fromX + rearWheelOffsetX, toX + rearWheelOffsetX, worldY, forwardTravel, swipeDuration, ref rightTireMarkRoutine);
    }

    const int TireMarkSegments = 10;

    void SpawnTireMark(float fromX, float toX, float worldY, float forwardTravel, float drawDuration, ref Coroutine wheelRoutine)
    {
        if (Mathf.Abs(toX - fromX) < 0.01f) return;

        // Önceki iz bu tekerlek için hâlâ çiziliyorsa (oyuncu şeridi çok hızlı değiştirdiyse),
        // onu olduğu yerde YARIM bırakıp durduruyoruz ve hemen yeni şerit değişimi için yeni
        // izi çizmeye başlıyoruz.
        if (wheelRoutine != null) StopCoroutine(wheelRoutine);

        Vector3 anchor = new Vector3((fromX + toX) * 0.5f, worldY, 0f);
        GameObject template = GetTireMarkTemplate();
        GameObject instance = PoolManager.Spawn(template, anchor, Quaternion.identity);

        LineRenderer lr = instance.GetComponent<LineRenderer>();
        lr.material = tireTrackMaterial;
        lr.sortingOrder = tireTrackSortingOrder;
        lr.startWidth = tireTrackWidth;
        lr.endWidth = tireTrackWidth;
        lr.useWorldSpace = false;
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.TransformZ;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.positionCount = TireMarkSegments + 1;

        float fromXLocal = fromX - anchor.x;
        float toXLocal = toX - anchor.x;
        wheelRoutine = StartCoroutine(DrawTireMarkRoutine(lr, fromXLocal, toXLocal, forwardTravel, drawDuration));
    }

    // 🔥 ÖNEMLİ: Bu obje kendi Scroller'ıyla spawn anından itibaren SÜREKLİ aşağı kayıyor.
    // Bir nokta İLK açığa çıktığı anda y = forwardTravel*ti olarak DONDURULUP bir daha asla
    // güncellenmezse, o anki gerçek dünya konumu HER ZAMAN tam olarak "arabanın o anki arka
    // teker konumuna" denk gelir (çünkü obje o ana kadar tam olarak forwardTravel*ti kadar
    // kaymıştır) -> iz gerçekten tekerlekten başlayıp büyüyormüş gibi görünür. Zaten donmuş
    // noktaları HER KAREDE yeniden hesaplamak, Scroller'ın kendi kaymasıyla ÇAKIŞIP izi iki
    // katı hızla kaydırır (önceki hata buydu); bu yüzden her nokta sadece BİR KEZ dondurulur.
    // Henüz sırası gelmemiş (gelecekteki) noktalar, şu anki büyüyen ucun DEĞERİNE kenetlenir.
    IEnumerator DrawTireMarkRoutine(LineRenderer lr, float fromXLocal, float toXLocal, float forwardTravel, float duration)
    {
        const int lastIndex = TireMarkSegments;
        duration = Mathf.Max(duration, 0.05f);

        bool[] frozen = new bool[lastIndex + 1];
        float[] frozenY = new float[lastIndex + 1];
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (lr == null) yield break;

            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            float liveY = forwardTravel * p;
            int revealedIndex = Mathf.FloorToInt(p * lastIndex);

            for (int i = 0; i <= revealedIndex; i++)
            {
                if (!frozen[i])
                {
                    frozen[i] = true;
                    frozenY[i] = forwardTravel * ((float)i / lastIndex);
                }
            }

            for (int i = 0; i <= lastIndex; i++)
            {
                float x = Mathf.SmoothStep(fromXLocal, toXLocal, frozen[i] ? (float)i / lastIndex : p);
                float y = frozen[i] ? frozenY[i] : liveY;
                lr.SetPosition(i, new Vector3(x, y, 0f));
            }

            yield return null;
        }

        if (lr != null)
        {
            for (int i = 0; i <= lastIndex; i++)
            {
                float ti = (float)i / lastIndex;
                float x = Mathf.SmoothStep(fromXLocal, toXLocal, ti);
                float y = forwardTravel * ti;
                lr.SetPosition(i, new Vector3(x, y, 0f));
            }
        }
    }

    void Start()
    {
        carAnimator = GetComponent<Animator>();
        controlMode = PlayerPrefs.GetInt(ControlModeKey, 0);
        mainCam = Camera.main;

        if (mainCam != null)
        {
            originalCamPos = mainCam.transform.localPosition;
            camPosSaved = true;
        }
    }

    void Update()
    {
        // 🔥 DÜZELTME: Bu kontrol tersti ("GameManager varsa VE oyun aktif değilse" yerine
        // "GameManager yoksa VEYA oyun aktif değilse" olmalıydı). Ters mantık, MainMenu
        // sahnesindeki önizleme arabasının (GameManager orada da mevcut olduğu için) input
        // işlemeye devam etmesine ve çift dokunuşla Öfke Modu'nu (boost) tetiklemesine yol
        // açıyordu. ObstacleManager kontrolü de yalnızca gerçek oyun sahnesinde (menüdeki
        // önizlemede değil) çalışmasını garanti eder.
        if (GameManager.instance == null || !GameManager.instance.isGameActive) return;
        if (ObstacleManager.instance == null) return;
        if (Time.timeScale == 0f) return;

        if (!isInitialized)
        {
            centerLaneX = transform.position.x;
            logicalX = centerLaneX;
            targetX = centerLaneX;

            fixedY = transform.position.y;
            currentY = fixedY;
            targetY = fixedY;

            if (carAnimator != null)
            {
                carAnimator.enabled = true;
                carAnimator.applyRootMotion = false;
            }

            isInitialized = true;
        }

        if (IsDrifting) HandleDriftInput();
        else HandleInput();
        HandleBoostLogic();
    }

    void LateUpdate()
    {
        if (isInitialized) MoveCar();
    }

    void ProcessTap(Vector2 screenPos)
    {
        if (Time.time - lastTapTime < doubleTapThreshold)
        {
            TryActivateBoost();
            lastTapTime = 0f;
        }
        else
        {
            lastTapTime = Time.time;
        }
    }

    void TryActivateBoost()
    {
        if (GameManager.instance == null || !GameManager.instance.isGameActive || ObstacleManager.instance == null || isBoostActive || isBoostOnCooldown) return;
        if (IsDrifting) return;
        if (DriftBoostManager.instance != null && DriftBoostManager.instance.IsBusy) return; // COIN RUSH sırasında Öfke Modu yok
        if (TutorialManager.instance != null && !TutorialManager.instance.CanUseBoost()) return;

        if (GameManager.instance.UseBoostItem())
        {
            isBoostActive = true;
            currentBoostTimer = boostDuration;
            GameManager.instance.isBoosting = true;

            MissionsManager.AddGameplayProgress(MissionType.UseBoost, 1);
            TutorialManager.instance?.NotifyBoostActivated();

            if (frontFireEffect != null)
            {
                frontFireEffect.gameObject.SetActive(false);
                frontFireEffect.gameObject.SetActive(true);
                frontFireEffect.Play(true);
            }

            if (AudioManager.instance != null && AudioManager.instance.boostSound != null)
            {
                AudioManager.instance.PlaySFX(AudioManager.instance.boostSound);
            }

            Debug.Log("🚀 ÖFKE MODU AKTİF!");
        }
        else
        {
            Debug.Log("❌ Envanterde hiç Boost kalmadı!");
        }
    }

    private void StopBoostEarly()
    {
        isBoostActive = false;
        currentBoostTimer = 0f;
    }

    // Öfke Modu'nu hemen bitirir ve normal bekleme süresini başlatır (Drift Boost toplandığında kullanılır).
    // Trafik zaten temizlendiği için ek koruma (grace) süresine gerek yok.
    public void ForceEndBoost()
    {
        if (!isBoostActive) return;
        StopBoostEarly();
        isBoostOnCooldown = true;
        currentCooldownTimer = boostCooldown;
        if (GameManager.instance != null) GameManager.instance.isBoosting = false;
    }

    // --- DRIFT BOOST ---

    public float DriftMinX => centerLaneX - laneDistance - driftBoundsPadding;
    public float DriftMaxX => centerLaneX + laneDistance + driftBoundsPadding;

    public void BeginDrift()
    {
        if (!isInitialized) return;

        IsDrifting = true;
        driftVelocity = 0f;
        driftSteerDirection = 0f;
        SteeredDuringDrift = false;

        // Kısa şerit değiştirme izleri hâlâ çiziliyorsa yarıda bırak; drift izi devralıyor
        if (leftTireMarkRoutine != null) { StopCoroutine(leftTireMarkRoutine); leftTireMarkRoutine = null; }
        if (rightTireMarkRoutine != null) { StopCoroutine(rightTireMarkRoutine); rightTireMarkRoutine = null; }

        // Yarım kalan swipe'lar drift bittikten sonra şerit değiştirmesin
        touchStartPositions.Clear();
        isMouseDragging = false;
        driftHoldFingers.Clear();
        driftIgnoredFingers.Clear();
        driftMouseStartedOverUI = false;
    }

    public void EndDrift()
    {
        if (!IsDrifting) return;

        IsDrifting = false;
        driftVelocity = 0f;
        driftSteerDirection = 0f;

        // İzler olduğu yerde kalır ve yolla birlikte kayıp ekrandan çıkınca kendini havuza iade eder
        leftDriftMark = null;
        rightDriftMark = null;

        // En yakın şeride otur; MoveCar arabayı normal şerit değiştirme hızıyla oraya taşır
        int nearestLane = Mathf.Clamp(Mathf.RoundToInt((logicalX - centerLaneX) / laneDistance) + 1, 0, 2);
        currentLane = nearestLane;
        targetX = centerLaneX + (currentLane - 1) * laneDistance;

        // Drift sırasında basılı tutulan parmak, bırakılınca tap/swipe olarak algılanmasın:
        // hâlâ basılı olan parmakları "işlendi" say, kaldırılana kadar yok sayılsınlar
        touchStartPositions.Clear();
        isMouseDragging = false;
        driftHoldFingers.Clear();
        driftIgnoredFingers.Clear();

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var touch in touchscreen.touches)
                if (touch.press.isPressed) handledTouchIds.Add(touch.touchId.ReadValue());
        }
    }

    // Ekranın SAĞ yarısına basılı tut = sağa drift, SOL yarısına basılı tut = sola drift,
    // dokunma yok = araba düzelip dümdüz gider. Birden fazla parmak varsa en son basılan yönetir.
    void HandleDriftInput()
    {
        float steer = 0f;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
        }

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            pressedFingerIds.Clear();
            int newestOrder = -1;
            float newestX = 0f;

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed) continue;

                int fingerId = touch.touchId.ReadValue();
                pressedFingerIds.Add(fingerId);

                // Her parmak ilk görüldüğünde bir kez sınıflandırılır: UI üzerinde başladıysa (ör. Pause butonu) sayılmaz
                if (!driftHoldFingers.ContainsKey(fingerId) && !driftIgnoredFingers.Contains(fingerId))
                {
                    if (IsPointerOverUI(touch.startPosition.ReadValue())) driftIgnoredFingers.Add(fingerId);
                    else driftHoldFingers[fingerId] = driftFingerCounter++;
                }

                if (driftHoldFingers.TryGetValue(fingerId, out int order) && order > newestOrder)
                {
                    newestOrder = order;
                    newestX = touch.position.ReadValue().x; // parmak ekranın diğer yarısına kaydırılırsa yön de değişir
                }
            }

            if (newestOrder >= 0) steer = newestX >= Screen.width * 0.5f ? 1f : -1f;

            // Bırakılan parmakları unut (duraklatma sırasında kaçırılan "Ended" olayları dahil)
            releasedFingerIds.Clear();
            foreach (int id in driftHoldFingers.Keys)
                if (!pressedFingerIds.Contains(id)) releasedFingerIds.Add(id);
            foreach (int id in releasedFingerIds) driftHoldFingers.Remove(id);
            driftIgnoredFingers.RemoveWhere(id => !pressedFingerIds.Contains(id));
        }
        else
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    driftMouseStartedOverUI = IsPointerOverUI(mouse.position.ReadValue());

                if (mouse.leftButton.isPressed && !driftMouseStartedOverUI)
                    steer = mouse.position.ReadValue().x >= Screen.width * 0.5f ? 1f : -1f;
            }
        }

        driftSteerDirection = Mathf.Clamp(steer, -1f, 1f);
        float targetVelocity = driftSteerDirection * driftSpeed;
        driftVelocity = Mathf.MoveTowards(driftVelocity, targetVelocity, driftAcceleration * Time.deltaTime);

        // Gizli başarım için: bu drift boyunca oyuncu bir kez bile yön verdi mi?
        if (driftSteerDirection != 0f) SteeredDuringDrift = true;
    }

    void HandleBoostLogic()
    {
        if (isBoostActive)
        {
            currentBoostTimer -= Time.deltaTime;
            targetY = fixedY + forwardOffset;

            float t = currentBoostTimer / boostDuration;

            if (camPosSaved && mainCam != null)
            {
                float currentShake = maxShakeMagnitude * t;
                float shakeX = Random.Range(-1f, 1f) * currentShake;
                float shakeY = Random.Range(-1f, 1f) * currentShake;

                mainCam.transform.localPosition = originalCamPos + new Vector3(shakeX, shakeY, 0);
            }

            if (currentBoostTimer <= 0)
            {
                StopBoostEarly();

                isBoostOnCooldown = true;
                currentCooldownTimer = boostCooldown;

                StartCoroutine(BoostGraceRoutine());
            }
        }
        else
        {
            targetY = fixedY;

            if (camPosSaved && mainCam != null && mainCam.transform.localPosition != originalCamPos)
            {
                mainCam.transform.localPosition = originalCamPos;
            }

            if (frontFireEffect != null)
            {
                if (frontFireEffect.gameObject.activeSelf)
                {
                    frontFireEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (isBoostOnCooldown)
            {
                currentCooldownTimer -= Time.deltaTime;
                if (currentCooldownTimer <= 0)
                {
                    isBoostOnCooldown = false;
                    currentCooldownTimer = 0f;
                }
            }
        }

        currentY = Mathf.MoveTowards(currentY, targetY, forwardSpeed * Time.deltaTime);

        if (GameManager.instance != null)
        {
            GameManager.instance.isBoosting = isBoostActive;
        }
    }

    // ARABALARIN ÜZERİNDEN SWIPE YAPABİLMEMİZİ SAĞLAYAN KURŞUN GEÇİRMEZ UI FILTRESI
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = screenPosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // 1. KORUMA: Eğer raycast edilen obje bir UI Canvas altında değilse, bu kesinlikle dünya objesidir (Araba, Engel vb.)
            if (result.gameObject.GetComponentInParent<Canvas>() == null)
            {
                continue;
            }

            // 2. KORUMA: Objenin katmanı (Layer) UI katmanı olmalı
            if (result.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                // 3. KORUMA: Nesne üzerinde 2D Collider varsa (örneğin Canvas tabanlı bir engel nesnesi), UI saymıyoruz
                if (result.gameObject.GetComponent<Collider2D>() != null)
                {
                    continue;
                }

                // 🔥 DÜZELTME: CanvasGroup ile gizlenmiş (alpha = 0) UI görünmez ama raycast almaya devam eder.
                // Örn. NearMissStreakUI ve BuffTimerUI kendilerini CanvasGroup.alpha ile gizliyor; içlerindeki
                // Image'ların kendi rengi hâlâ opak olduğu için ekranda hiçbir şey yokken swipe engelleniyordu.
                float groupAlpha = GetCanvasGroupAlpha(result.gameObject);
                if (groupAlpha <= 0.01f) continue;

                // 4. KORUMA: Etkileşimli UI bileşenleri (Button, Toggle vb.) swipe'ı engeller
                bool isInteractive = result.gameObject.GetComponent<Selectable>() != null ||
                                     result.gameObject.GetComponentInParent<Selectable>() != null ||
                                     result.gameObject.GetComponent<EventTrigger>() != null ||
                                     result.gameObject.GetComponentInParent<EventTrigger>() != null;
                if (isInteractive) return true;

                // 5. KORUMA: Görünür Image panelleri (Alt Bar, Üst Bar gibi HUD arkaplanları) swipe'ı engeller.
                // TextMeshPro elemanları (Countdown sayacı gibi) hariç tutulur — oyun alanındaki
                // metin katmanlarında swipe çalışmaya devam etsin.
                var image = result.gameObject.GetComponent<UnityEngine.UI.Image>();
                if (image != null && image.color.a * groupAlpha > 0.01f) return true;
            }
        }
        return false;
    }

    // Başlangıçtan bu yana yatay swipe eşiği geçildiyse şerit değiştirir ve true döner.
    // Dikey baskın hareketlerde başlangıç noktası güncellenir (dikey kaydırmadan sonra yatay swipe yine çalışsın).
    bool TrySwipe(int fingerId, Vector2 startPos, Vector2 pos)
    {
        Vector2 direction = pos - startPos;
        if (direction.magnitude < swipeRange) return false;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0) MoveRight();
            else MoveLeft();
            touchStartPositions.Remove(fingerId);
            return true;
        }

        touchStartPositions[fingerId] = pos;
        return false;
    }

    // Objenin üstündeki tüm CanvasGroup'ların alpha çarpımı (ekranda gerçekte ne kadar görünür olduğu)
    static float GetCanvasGroupAlpha(GameObject go)
    {
        float alpha = 1f;
        Transform t = go.transform;
        while (t != null)
        {
            CanvasGroup group = t.GetComponent<CanvasGroup>();
            if (group != null)
            {
                alpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            t = t.parent;
        }
        return alpha;
    }

    void HandleInput()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) MoveLeft();
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) MoveRight();
            if (kb.spaceKey.wasPressedThisFrame) TryActivateBoost();
        }

        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            // 🔥 DÜZELTME (swipe'ın "bazen" algılanmaması): Eskiden parmak yalnızca "Began" fazı görüldüğünde
            // kaydediliyordu. Input System'de hızlı bir swipe'ın Began ve Moved olayları aynı kareye düşebilir;
            // o karede faz doğrudan "Moved" okunur, Began hiç görülmez ve swipe tamamen yok sayılırdı.
            // Artık faza bakmıyoruz: basılı olan ve daha önce görmediğimiz her parmağı, dokunuşun gerçek
            // başlangıç noktasıyla (startPosition) kaydediyoruz.
            pressedFingerIds.Clear();

            foreach (var touch in touchscreen.touches)
            {
                bool pressed = touch.press.isPressed;
                bool released = touch.press.wasReleasedThisFrame;
                if (!pressed && !released) continue;

                int fingerId = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                if (pressed) pressedFingerIds.Add(fingerId);

                // Yeni parmak: UI üzerinde başladıysa (ör. Pause) yok say, değilse başlangıç noktasıyla kaydet
                if (!touchStartPositions.ContainsKey(fingerId) && !handledTouchIds.Contains(fingerId))
                {
                    Vector2 start = touch.startPosition.ReadValue();
                    if (IsPointerOverUI(start)) handledTouchIds.Add(fingerId);
                    else touchStartPositions[fingerId] = start;
                }

                if (!touchStartPositions.TryGetValue(fingerId, out Vector2 startPos))
                {
                    // Bu parmak zaten swipe yaptı veya UI'da başladı; bırakılınca unut
                    if (released) handledTouchIds.Remove(fingerId);
                    continue;
                }

                // 🔥 DÜZELTME: Swipe kontrolü bırakıldığı karede de yapılıyor. Eskiden parmak, mesafe eşiğini
                // geçtiği karede kalkarsa (hızlı "fiske") hareket "tap" sayılıp şerit değişmiyordu.
                if (controlMode == 0 && TrySwipe(fingerId, startPos, pos))
                {
                    if (released) handledTouchIds.Remove(fingerId);
                    else handledTouchIds.Add(fingerId); // aynı parmak kaldırılmadan ikinci kez swipe yapmasın
                    continue;
                }

                if (released)
                {
                    ProcessTap(pos);
                    touchStartPositions.Remove(fingerId);
                }
            }

            // Oyun duraklatılmışken (Update çalışmazken) kalkan parmakları unut
            releasedFingerIds.Clear();
            foreach (int id in touchStartPositions.Keys)
                if (!pressedFingerIds.Contains(id)) releasedFingerIds.Add(id);
            foreach (int id in releasedFingerIds) touchStartPositions.Remove(id);
            handledTouchIds.RemoveWhere(id => !pressedFingerIds.Contains(id));
            return;
        }

        // Mouse fallback for editor testing (only when no touchscreen device present)
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        if (controlMode == 0)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsPointerOverUI(mousePos))
                {
                    mouseStartPos = mousePos;
                    isMouseDragging = true;
                }
            }
            else if (mouse.leftButton.isPressed && isMouseDragging)
            {
                Vector2 direction = mousePos - mouseStartPos;
                if (direction.magnitude >= swipeRange)
                {
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                    {
                        if (direction.x > 0) MoveRight();
                        else MoveLeft();
                    }
                    isMouseDragging = false;
                }
            }
            else if (mouse.leftButton.wasReleasedThisFrame && isMouseDragging)
            {
                ProcessTap(mousePos);
                isMouseDragging = false;
            }
        }
        else
        {
            if (mouse.leftButton.wasReleasedThisFrame && !IsPointerOverUI(mousePos))
                ProcessTap(mousePos);
        }
    }

    private IEnumerator BoostGraceRoutine()
    {
        isBoostGracePeriod = true;
        yield return new WaitForSeconds(boostGraceDuration);
        isBoostGracePeriod = false;
    }

    void SetLane(int laneIndex, int direction)
    {
        if (currentLane == laneIndex) return;

        float fromX = logicalX;
        currentLane = laneIndex;
        targetX = centerLaneX + (currentLane - 1) * laneDistance;

        TutorialManager.instance?.NotifyPlayerSwiped();

        // Lastik izi: sınır şeritlerin ötesine geçilemediği için (MoveLeft/MoveRight zaten
        // kontrol ediyor) bu metod yalnızca geçerli bir şerit değişiminde çağrılır.
        SpawnTireMarks(fromX, targetX);

        if (AudioManager.instance != null) AudioManager.instance.PlayRandomSwipeSound();

        if (carAnimator != null && carAnimator.enabled)
        {
            if (direction > 0)
            {
                carAnimator.ResetTrigger("Sola Kayma");
                carAnimator.SetTrigger("Saga Kayma");
            }
            else if (direction < 0)
            {
                carAnimator.ResetTrigger("Saga Kayma");
                carAnimator.SetTrigger("Sola Kayma");
            }
        }
    }

    public void MoveLeft()
    {
        if (GameManager.instance != null && !GameManager.instance.isGameActive) return;
        int newLane = currentLane - 1;
        if (newLane >= 0) SetLane(newLane, -1);
    }

    public void MoveRight()
    {
        if (GameManager.instance != null && !GameManager.instance.isGameActive) return;
        int newLane = currentLane + 1;
        if (newLane <= 2) SetLane(newLane, 1);
    }

    void MoveCar()
    {
        float targetTilt = 0f;

        if (IsDrifting)
        {
            logicalX += driftVelocity * Time.deltaTime;

            // Yol sınırına dayanınca yatay hızı sıfırla ki araba duvara "yapışık" kalmasın
            if (logicalX >= DriftMaxX) { logicalX = DriftMaxX; if (driftVelocity > 0f) driftVelocity = 0f; }
            else if (logicalX <= DriftMinX) { logicalX = DriftMinX; if (driftVelocity < 0f) driftVelocity = 0f; }

            // Araba drift yönüne doğru yan durur: sağa drift = burun sağa (saat yönü = negatif Z).
            // Açı hıza değil yöne bağlı, böylece duvara dayanınca bile yan durmaya devam eder.
            targetTilt = -driftSteerDirection * driftMaxTilt;
        }
        else
        {
            if (Mathf.Abs(logicalX - targetX) < 0.01f) logicalX = targetX;
            else logicalX = Mathf.MoveTowards(logicalX, targetX, moveSpeed * Time.deltaTime);
        }

        transform.position = new Vector3(logicalX, currentY, transform.position.z);

        // Eğilme yalnızca drift sırasında ve drift bittikten sonra düzelene kadar uygulanır;
        // diğer zamanlarda rotasyona dokunmuyoruz (Animator'ın şerit değiştirme animasyonları bozulmasın)
        if (IsDrifting || isTiltApplied)
        {
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, 1f - Mathf.Exp(-driftTiltSmoothing * Time.deltaTime));

            if (!IsDrifting && Mathf.Abs(currentTilt) < 0.05f)
            {
                currentTilt = 0f;
                isTiltApplied = false;
            }
            else
            {
                isTiltApplied = true;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
        }

        // İz ve duman yalnızca oyuncu gerçekten yön verip kayarken çıkar; dümdüz giderken çıkmaz
        bool isSliding = IsDrifting && driftSteerDirection != 0f;
        if (isSliding)
        {
            UpdateDriftMarks();
        }
        else
        {
            // Kayma bitti: mevcut iz parçaları olduğu yerde kalır, tekrar kayınca yeni iz başlar
            leftDriftMark = null;
            rightDriftMark = null;
        }
        UpdateDriftSmoke(isSliding);

        // Lastik sesi de dumanla aynı koşulda: yalnızca basılı tutup kayarken. Çağrı kesilince ses kendiliğinden söner.
        if (isSliding && AudioManager.instance != null)
            AudioManager.instance.SetDriftSound(Mathf.Max(0.4f, Mathf.Abs(driftVelocity) / Mathf.Max(driftSpeed, 0.01f)));
    }

    void GetRearWheelPositions(out Vector3 leftWheel, out Vector3 rightWheel)
    {
        // Tekerlek konumları arabanın dönüşüyle birlikte döner
        Quaternion rot = Quaternion.Euler(0f, 0f, currentTilt);
        Vector3 carPos = transform.position;
        leftWheel = carPos + rot * new Vector3(-rearWheelOffsetX, rearWheelOffsetY, 0f);
        rightWheel = carPos + rot * new Vector3(rearWheelOffsetX, rearWheelOffsetY, 0f);
        leftWheel.z = 0f;
        rightWheel.z = 0f;
    }

    // --- DRIFT DUMANI ---
    // Arka tekerleklerden çıkan küçük duman bulutları. Parçacıklar dünya uzayında kalır ve yolla aynı
    // hızda aşağı kayar; böylece duman arabayı takip etmez, kaydığı yerde yol üzerinde kalıp dağılır.
    void UpdateDriftSmoke(bool isSliding)
    {
        if (leftDriftSmoke == null)
        {
            if (!isSliding) return;
            Material mat = GetDriftSmokeMaterial();
            if (mat == null) return;
            leftDriftSmoke = CreateDriftSmoke("DriftSmoke_L", mat);
            rightDriftSmoke = CreateDriftSmoke("DriftSmoke_R", mat);
        }

        GetRearWheelPositions(out Vector3 leftWheel, out Vector3 rightWheel);
        leftDriftSmoke.transform.position = leftWheel;
        rightDriftSmoke.transform.position = rightWheel;

        // Yan kayma hızı arttıkça duman yoğunlaşır; duvara dayanıp yön vermeye devam ederken de az da olsa çıkar
        float intensity = isSliding ? Mathf.Max(0.4f, Mathf.Abs(driftVelocity) / Mathf.Max(driftSpeed, 0.01f)) : 0f;
        float roadFactor = InfiniteRoad2D.Instance != null ? InfiniteRoad2D.Instance.scrollFactor : 1f;
        float roadSpeed = ObstacleManager.scrollSpeed * roadFactor;

        ApplyDriftSmoke(leftDriftSmoke, intensity, roadSpeed);
        ApplyDriftSmoke(rightDriftSmoke, intensity, roadSpeed);
    }

    void ApplyDriftSmoke(ParticleSystem ps, float intensity, float roadSpeed)
    {
        var emission = ps.emission;
        emission.rateOverTime = driftSmokeRate * intensity;

        var velocity = ps.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(-driftSmokeSpread, driftSmokeSpread);
        velocity.y = new ParticleSystem.MinMaxCurve(-roadSpeed, -roadSpeed);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    Material GetDriftSmokeMaterial()
    {
        if (driftSmokeMaterial != null) return driftSmokeMaterial;

        // Beyaz bulut materyali: egzoz materyalinin görseli alev renkli (kırmızı-sarı) olduğu için
        // onunla duman kahverengi/toprak gibi görünüyordu; bu materyalde renk tamamen driftSmokeColor'dan gelir.
        driftSmokeMaterial = Resources.Load<Material>("Materials/DriftSmoke");
        if (driftSmokeMaterial != null) return driftSmokeMaterial;

        // O da yoksa arabanın kendi egzoz dumanının materyaline düş
        foreach (var r in GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (r.sharedMaterial != null && r.gameObject.name.Contains("Egzoz"))
            {
                driftSmokeMaterial = r.sharedMaterial;
                break;
            }
        }
        return driftSmokeMaterial;
    }

    ParticleSystem CreateDriftSmoke(string objName, Material mat)
    {
        var go = new GameObject(objName);
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(driftSmokeLifetime * 0.7f, driftSmokeLifetime);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(driftSmokeSize * 0.7f, driftSmokeSize * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = driftSmokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 150;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.sharedMaterial = mat;
        psRenderer.sortingOrder = driftSmokeSortingOrder;

        ps.Play();
        return ps;
    }

    void OnDestroy()
    {
        // Duman objeleri sahnede bağımsız duruyor; araba yok olunca onları da temizle
        if (leftDriftSmoke != null) Destroy(leftDriftSmoke.gameObject);
        if (rightDriftSmoke != null) Destroy(rightDriftSmoke.gameObject);
    }

    // --- DRIFT LASTİK İZLERİ ---
    // Şerit değiştirme iziyle aynı yöntem: iz, yolla birlikte kayan (TireMarkScroller) bir LineRenderer'dır
    // ve noktaları objenin LOCAL uzayında tutulur. Her karede arka tekerleğin o anki dünya konumunu
    // local uzaya çevirip eklediğimizde nokta yol üzerinde "donar" ve yolla birlikte aşağı kayar;
    // böylece iz, arabanın drift ederken yol üzerinde çizdiği gerçek kavisi takip eder.
    void UpdateDriftMarks()
    {
        if (tireTrackMaterial == null) return;

        GetRearWheelPositions(out Vector3 leftWheel, out Vector3 rightWheel);

        UpdateDriftMark(ref leftDriftMark, leftWheel);
        UpdateDriftMark(ref rightDriftMark, rightWheel);
    }

    void UpdateDriftMark(ref DriftMark mark, Vector3 wheelWorld)
    {
        wheelWorld.z = 0f;

        // İz ekrandan çıkıp havuza döndüyse (ör. uzun süre aynı parçada kalındıysa) yenisini başlat
        if (mark != null && (mark.line == null || !mark.line.gameObject.activeInHierarchy)) mark = null;

        if (mark == null)
        {
            mark = StartDriftMark(wheelWorld, null);
        }
        else if (mark.count >= mark.points.Length)
        {
            // Parça doldu: yenisini son noktadan başlatarak iz kesintisiz devam etsin
            Vector3 lastWorld = mark.line.transform.position + mark.points[mark.count - 1];
            mark = StartDriftMark(wheelWorld, lastWorld);
        }

        Vector3 local = wheelWorld - mark.line.transform.position;

        // Son nokta "canlı uç"tur ve her karede tekerleği takip eder; bir önceki sabit noktadan
        // yeterince uzaklaşınca sabitlenip yeni bir canlı uç eklenir
        if (mark.count < 2 || Vector3.Distance(mark.points[mark.count - 2], local) >= driftMarkPointSpacing)
        {
            mark.points[mark.count] = local;
            mark.count++;
            mark.line.positionCount = mark.count;
        }
        else
        {
            mark.points[mark.count - 1] = local;
        }

        mark.line.SetPosition(mark.count - 1, local);
    }

    DriftMark StartDriftMark(Vector3 wheelWorld, Vector3? continueFrom)
    {
        GameObject template = GetTireMarkTemplate();
        GameObject instance = PoolManager.Spawn(template, wheelWorld, Quaternion.identity);

        LineRenderer lr = instance.GetComponent<LineRenderer>();
        lr.material = tireTrackMaterial;
        lr.sortingOrder = tireTrackSortingOrder;
        lr.startWidth = tireTrackWidth;
        lr.endWidth = tireTrackWidth;
        lr.useWorldSpace = false;
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.TransformZ;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.positionCount = 0;

        var mark = new DriftMark { line = lr, points = new Vector3[Mathf.Max(driftMarkMaxPoints, 4)], count = 0 };

        if (continueFrom.HasValue)
        {
            Vector3 start = continueFrom.Value - lr.transform.position;
            start.z = 0f;
            mark.points[0] = start;
            mark.count = 1;
            lr.positionCount = 1;
            lr.SetPosition(0, start);
        }

        return mark;
    }
}