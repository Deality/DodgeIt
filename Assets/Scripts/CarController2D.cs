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

    [Header("Hassasiyet Ayarları")]
    public float swipeRange = 50f;

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
        float swipeDuration = laneDistance / Mathf.Max(moveSpeed, 0.01f);
        float forwardTravel = Mathf.Max(ObstacleManager.scrollSpeed * swipeDuration, tireTrackWidth * 4f);
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

        HandleInput();
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

        if (GameManager.instance.UseBoostItem())
        {
            isBoostActive = true;
            currentBoostTimer = boostDuration;
            GameManager.instance.isBoosting = true;

            MissionsManager.AddGameplayProgress(MissionType.UseBoost, 1);

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
                if (image != null && image.color.a > 0.01f) return true;
            }
        }
        return false;
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
            foreach (var touch in touchscreen.touches)
            {
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;

                Vector2 pos = touch.position.ReadValue();
                int fingerId = touch.touchId.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    if (!IsPointerOverUI(pos))
                        touchStartPositions[fingerId] = pos;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                         phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    if (controlMode == 0 && touchStartPositions.ContainsKey(fingerId))
                    {
                        Vector2 direction = pos - touchStartPositions[fingerId];
                        if (direction.magnitude >= swipeRange)
                        {
                            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                            {
                                if (direction.x > 0) MoveRight();
                                else MoveLeft();
                                touchStartPositions.Remove(fingerId);
                            }
                            else
                            {
                                touchStartPositions[fingerId] = pos;
                            }
                        }
                    }
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (touchStartPositions.ContainsKey(fingerId))
                    {
                        if (controlMode == 0) ProcessTap(pos);
                        touchStartPositions.Remove(fingerId);
                    }
                    else if (controlMode == 1 && !IsPointerOverUI(pos))
                    {
                        ProcessTap(pos);
                    }
                }
            }
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
        if (Mathf.Abs(logicalX - targetX) < 0.01f) logicalX = targetX;
        else logicalX = Mathf.MoveTowards(logicalX, targetX, moveSpeed * Time.deltaTime);

        transform.position = new Vector3(logicalX, currentY, transform.position.z);
    }
}