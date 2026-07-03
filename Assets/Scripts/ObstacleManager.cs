using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager instance;
    public static float scrollSpeed;
    public static float InitialScrollSpeedReference;

    public bool canSpawn = false;

    // Diğer scriptler (CarController2D) okuyabilsin diye public
    public bool isSpeedReduced = false;
    private Coroutine speedReductionCoroutine;

    // 🔥 YENİ: Yavaşlatıcının UI'da yuvarlak sayaca dönüşebilmesi için dışarı açılan süreler
    [HideInInspector] public float currentReducerTimer = 0f;
    [HideInInspector] public float reducerMaxDuration = 1f;

    private float baseDifficultySpeed;

    [Header("Debug Modu")]
    public bool enableDebugLogs = true;

    // 🔥 YENİ: SPAWN YÜKSEKLİĞİ AYARI
    [Header("Spawn Yükseklik Ayarı")]
    [Tooltip("Arabalar ve coinler kameranın üst sınırından ne kadar uzakta spawn olsun? (Altınların ekranda aniden belirmemesi için 5 veya daha yüksek bir değer yapın)")]
    public float spawnYOffset = 6f;

    [Header("🚀 Dinamik Boost Ayarları")]
    public float maxBoostMultiplier = 2.0f;
    public float minBoostMultiplier = 1.1f;
    public float boostMinSpeed = 60f;
    public float boostMaxSpeed = 180f;
    public float boostTransitionSpeed = 3.0f;
    private float currentBoostMultiplier = 1.0f;

    [Header("Görsel Efektler")]
    public GameObject speedEffectUnderCarPrefab;
    public Image speedEffectOverlay;
    [Range(0f, 1f)] public float maxOverlayAlpha = 0.3f;
    private GameObject currentEffectInstance;

    public int effectSpawnCount = 5;
    public float spawnSafeRadius = 150f;
    private List<GameObject> activeEffects = new List<GameObject>();

    [Header("Prefabs (Normal Trafik)")]
    public List<GameObject> obstaclePrefabs;

    [Header("🏎️ Maganda (Hızlı) Araç Ayarları")]
    public List<GameObject> magandaPrefabs;
    [Range(0f, 1f)] public float magandaSpawnChance = 0.15f;
    public float magandaBonusSpeed = 30f;

    [Header("Prefabs (Diğer)")]
    public GameObject coinPrefab;
    public GameObject speedReducerPrefab;
    public GameObject invincibilityPrefab;

    [Header("🔥 Büyük Araç (Tır) Ayarları")]
    public GameObject bigTruckPrefab;
    public GameObject warningSignPrefab;
    public float truckSpawnSpeedThreshold = 20f;
    public float truckBonusSpeed = 10f;
    public float truckSpawnYOffset = 15f;
    public float truckStartDelay = 30f;

    public float maxWarningDuration = 3.0f;
    public float minWarningDuration = 0.5f;
    public float maxTruckPassDuration = 4.0f;
    public float minTruckPassDuration = 1.5f;
    public float minSpeedForScaling = 10f;
    public float maxSpeedForScaling = 40f;

    public float maxTruckCooldown = 20.0f;
    public float minTruckCooldown = 8.0f;

    private float lastTruckTime = -100f;
    private bool isTruckEventActive = false;
    private int blockedLaneIndex = -1;

    [Header("Lanes")]
    public Transform[] lanes;

    [Header("Settings")]
    public float initialSpawnInterval = 2f;
    public float initialScrollSpeed = 5f;

    [Header("Coin (Altın) Spawn Ayarları")]
    [Tooltip("Normal bir arabanın önünde altın çıkma ihtimali (Örn: 0.3 = %30)")]
    [Range(0, 1)] public float coinSpawnChance = 0.3f;
    [Tooltip("Altın, arabanın ne kadar önünde belirecek? (Aşağı akış olduğu için eksi bir değer girin)")]
    public float coinSpawnOffset = -3.5f;
    [Tooltip("Arabanın önünde art arda en fazla kaç altın çıkabilsin?")]
    public int maxCoinInRow = 3;
    [Tooltip("Peş peşe çıkan altınlar arasındaki mesafe.")]
    public float coinSpacing = 1.5f;

    [Header("Difficulty - Scroll Speed")]
    public float speedIncreaseRate = 0.3f;
    public float maxScrollSpeed = 20f;
    public float targetSpeedForRateChange = 15f;

    [Header("Difficulty - Spawn Interval Stages")]
    public float stage1Time = 5f;
    public float stage2Time = 12f;
    public float stage3Time = 20f;

    public float intervalStage1 = 1.25f;
    public float intervalStage2 = 1.0f;
    public float intervalStage3 = 0.75f;

    [Header("Dynamic Spawn Settings")]
    public float minDistanceBetweenObstacles = 5f;
    public float maxDistanceBetweenObstacles = 15f;
    public float minimumSpawnCooldown = 0.5f;
    public float sharedPowerUpCooldown = 5.0f;

    [Header("Reducer Settings")]
    [Range(0, 1)] public float reducerSpawnChance = 0.05f;
    [Range(0, 1)] public float maxReducerChance = 0.25f;
    public float minSpeedForReducer = 10f;
    public float reducerCooldownTime = 10.0f;
    public float reductionDuration = 8f;
    public float reductionAmount = 5f;
    public float reducerStartDelay = 10.0f;
    public float effectFadeDuration = 2.0f;
    public float reducerPityRate = 0.01f;

    [Header("Invincibility Settings")]
    [Range(0, 1)] public float invincibilitySpawnChance = 0.10f;
    [Range(0, 1)] public float maxInvincibilityChance = 0.40f;
    public float invincibilityCooldownTime = 5.0f;
    public float invincibilityStartDelay = 10.0f;
    public float invincibilityPityRate = 0.05f;

    private float currentInvincibilityChance;
    private float currentReducerChance;

    private float timer = 0f;
    private float totalTimeElapsed = 0f;
    private float currentSpawnInterval;
    private int currentDifficultyStage = 0;

    private float lastReducerSpawnTime = 0f;
    private float lastInvincibilitySpawnTime = 0f;
    private float lastPowerUpSpawnTime = -100f;

    private int lastLaneIndex = -1;

    void Awake()
    {
        if (instance == null) { instance = this; }
        else if (instance != this) { Destroy(gameObject); }
    }

    void Start()
    {
        canSpawn = false;

        if (initialScrollSpeed <= 0) initialScrollSpeed = 5f;
        InitialScrollSpeedReference = initialScrollSpeed;

        baseDifficultySpeed = initialScrollSpeed;
        scrollSpeed = baseDifficultySpeed;

        currentSpawnInterval = initialSpawnInterval;

        timer = 0f;

        totalTimeElapsed = 0f;
        currentDifficultyStage = 0;

        lastReducerSpawnTime = -reducerCooldownTime;
        lastInvincibilitySpawnTime = -invincibilityCooldownTime;
        lastPowerUpSpawnTime = -sharedPowerUpCooldown;

        lastTruckTime = Time.time;

        currentInvincibilityChance = invincibilitySpawnChance;
        currentReducerChance = reducerSpawnChance;

        if (speedEffectOverlay != null)
        {
            if (speedEffectOverlay.GetComponent<CanvasGroup>() == null)
            {
                speedEffectOverlay.gameObject.AddComponent<CanvasGroup>();
            }
            SetCanvasGroupAlpha(0f);
            speedEffectOverlay.gameObject.SetActive(false);
        }

        if (lanes == null || lanes.Length == 0) Debug.LogError("❌ HATA: Lanes listesi boş!");
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0) Debug.LogError("❌ HATA: Obstacle Prefabs boş!");
    }

    public void StartGameLoop()
    {
        canSpawn = true;
        timer = 0f;
        Debug.Log("🏁 Araba yerine yerleşti, engeller başlıyor!");
    }

    private float GetRequiredSpawnInterval()
    {
        float safeSpeed = Mathf.Max(scrollSpeed, 0.1f);
        float minInterval = minDistanceBetweenObstacles / safeSpeed;

        if (maxDistanceBetweenObstacles <= minDistanceBetweenObstacles)
            maxDistanceBetweenObstacles = minDistanceBetweenObstacles * 2f;

        float maxInterval = maxDistanceBetweenObstacles / safeSpeed;
        float lowerBound = Mathf.Max(minInterval, minimumSpawnCooldown);
        float finalInterval = Mathf.Clamp(currentSpawnInterval, lowerBound, maxInterval);

        return finalInterval;
    }

    void Update()
    {
        if (!canSpawn) return;

        totalTimeElapsed += Time.deltaTime;

        // --- HIZ YÖNETİMİ ---
        if (!isSpeedReduced)
        {
            if (baseDifficultySpeed < maxScrollSpeed)
            {
                baseDifficultySpeed += speedIncreaseRate * Time.deltaTime;

                if (baseDifficultySpeed >= targetSpeedForRateChange && speedIncreaseRate != 1f)
                {
                    speedIncreaseRate = 1f;
                }
                baseDifficultySpeed = Mathf.Min(baseDifficultySpeed, maxScrollSpeed);
            }

            float targetMultiplier = 1.0f;

            if (GameManager.instance != null && GameManager.instance.isBoosting)
            {
                float tBoost = Mathf.InverseLerp(boostMinSpeed, boostMaxSpeed, baseDifficultySpeed);
                targetMultiplier = Mathf.Lerp(maxBoostMultiplier, minBoostMultiplier, tBoost);
            }

            currentBoostMultiplier = Mathf.Lerp(currentBoostMultiplier, targetMultiplier, Time.deltaTime * boostTransitionSpeed);
            scrollSpeed = baseDifficultySpeed * currentBoostMultiplier;
        }

        // Zorluk Aşamaları
        if (currentDifficultyStage == 0 && totalTimeElapsed >= stage1Time) { currentSpawnInterval = intervalStage1; currentDifficultyStage = 1; }
        else if (currentDifficultyStage == 1 && totalTimeElapsed >= stage2Time) { currentSpawnInterval = intervalStage2; currentDifficultyStage = 2; }
        else if (currentDifficultyStage == 2 && totalTimeElapsed >= stage3Time) { currentSpawnInterval = intervalStage3; currentDifficultyStage = 3; }

        // 🔥 DİNAMİK TIR KONTROLÜ
        float t = Mathf.InverseLerp(minSpeedForScaling, maxSpeedForScaling, scrollSpeed);
        float currentTruckCooldown = Mathf.Lerp(maxTruckCooldown, minTruckCooldown, t);

        if (!isTruckEventActive
            && scrollSpeed >= truckSpawnSpeedThreshold
            && Time.time >= lastTruckTime + currentTruckCooldown
            && totalTimeElapsed >= truckStartDelay)
        {
            StartCoroutine(StartTruckEvent());
        }

        // 🔥 NORMAL SPAWN
        float spawnTimeRequired = GetRequiredSpawnInterval();
        timer += Time.deltaTime;

        if (timer >= spawnTimeRequired)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    // 🔥 TIR ETKİNLİĞİ FONKSİYONU
    IEnumerator StartTruckEvent()
    {
        isTruckEventActive = true;

        yield return new WaitForSeconds(1.0f);

        int targetLane = (Random.Range(0, 2) == 0) ? 0 : lanes.Length - 1;
        blockedLaneIndex = targetLane;

        float t = Mathf.InverseLerp(minSpeedForScaling, maxSpeedForScaling, scrollSpeed);
        float currentWarnDuration = Mathf.Lerp(maxWarningDuration, minWarningDuration, t);
        float currentTruckPassDuration = Mathf.Lerp(maxTruckPassDuration, minTruckPassDuration, t);

        if (warningSignPrefab != null && lanes.Length > targetLane)
        {
            Vector3 warningPos = new Vector3(lanes[targetLane].position.x, 3f, 0f);
            GameObject warning = Instantiate(warningSignPrefab, warningPos, Quaternion.identity);

            yield return new WaitForSeconds(currentWarnDuration);
            if (warning != null) Destroy(warning);
        }
        else
        {
            yield return new WaitForSeconds(currentWarnDuration);
        }

        if (bigTruckPrefab != null)
        {
            Camera cam = Camera.main;
            float camHeight = 2f * cam.orthographicSize;
            // 🔥 YENİ: Tırın da spawn yüksekliğini dinamik ofsetimize göre ayarlıyoruz
            float normalSpawnY = cam.transform.position.y + (camHeight / 2f) + spawnYOffset;
            float truckSpawnY = normalSpawnY + truckSpawnYOffset;

            Vector3 spawnPos = new Vector3(lanes[targetLane].position.x, truckSpawnY, 0f);
            GameObject truck = Instantiate(bigTruckPrefab, spawnPos, Quaternion.identity);

            FastMover mover = truck.GetComponent<FastMover>();
            if (mover != null)
            {
                mover.bonusSpeed = truckBonusSpeed;
            }

            if (AudioManager.instance != null && AudioManager.instance.truckHornSound != null)
                AudioManager.instance.PlaySFX(AudioManager.instance.truckHornSound);
        }

        yield return new WaitForSeconds(currentTruckPassDuration);

        blockedLaneIndex = -1;
        lastTruckTime = Time.time;
        isTruckEventActive = false;
    }

    public void ActivateSpeedReduction()
    {
        if (speedReductionCoroutine != null) StopCoroutine(speedReductionCoroutine);
        isSpeedReduced = true;
        currentBoostMultiplier = 1.0f;
        speedReductionCoroutine = StartCoroutine(SpeedReductionCoroutine(reductionDuration, reductionAmount));
    }

    IEnumerator SpeedReductionCoroutine(float duration, float amount)
    {
        reducerMaxDuration = duration;
        currentReducerTimer = duration;

        if (speedEffectOverlay != null)
        {
            speedEffectOverlay.gameObject.SetActive(true);
            SetCanvasGroupAlpha(0f);
        }

        ClearActiveEffects();

        if (speedEffectUnderCarPrefab != null && speedEffectOverlay != null)
        {
            RectTransform panelRect = speedEffectOverlay.GetComponent<RectTransform>();
            float width = panelRect.rect.width;
            float height = panelRect.rect.height;
            List<Vector2> usedPositions = new List<Vector2>();

            for (int i = 0; i < effectSpawnCount; i++)
            {
                Vector2 finalPos = Vector2.zero;
                bool positionFound = false;
                int attempts = 0;

                while (attempts < 10 && !positionFound)
                {
                    float randomX = Random.Range(-width * 0.4f, width * 0.4f);
                    float randomY = Random.Range(-height * 0.4f, height * 0.4f);
                    Vector2 candidatePos = new Vector2(randomX, randomY);

                    bool overlap = false;
                    foreach (Vector2 pos in usedPositions)
                    {
                        if (Vector2.Distance(candidatePos, pos) < spawnSafeRadius) { overlap = true; break; }
                    }

                    if (!overlap) { finalPos = candidatePos; positionFound = true; }
                    attempts++;
                }

                if (positionFound)
                {
                    usedPositions.Add(finalPos);
                    GameObject effect = Instantiate(speedEffectUnderCarPrefab, speedEffectOverlay.transform);
                    activeEffects.Add(effect);

                    RectTransform rect = effect.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                        rect.anchoredPosition = finalPos;
                        rect.localScale = Vector3.one;
                    }
                }
            }
            SetEffectsAlpha(1f);
        }

        float startSpeed = scrollSpeed;
        float targetSpeed = Mathf.Max(baseDifficultySpeed - amount, InitialScrollSpeedReference);

        float fadeInTimer = 0f;
        while (fadeInTimer < effectFadeDuration)
        {
            fadeInTimer += Time.deltaTime;
            currentReducerTimer -= Time.deltaTime; // 🔥 Zamanı Azalt
            float t = fadeInTimer / effectFadeDuration;

            SetCanvasGroupAlpha(Mathf.Lerp(0f, maxOverlayAlpha, t));
            SetEffectsAlpha(Mathf.Lerp(0f, 1f, t));

            scrollSpeed = Mathf.Lerp(startSpeed, targetSpeed, t);

            yield return null;
        }

        SetCanvasGroupAlpha(maxOverlayAlpha);
        SetEffectsAlpha(1f);
        scrollSpeed = targetSpeed;
        baseDifficultySpeed = targetSpeed;

        float waitTime = Mathf.Max(0, duration - (effectFadeDuration * 2));

        // 🔥 BURASI DEĞİŞTİ: Süreyi UI için kare kare hesaplamak zorundayız (Eskiden WaitForSeconds vardı)
        float waitT = 0f;
        while (waitT < waitTime)
        {
            waitT += Time.deltaTime;
            currentReducerTimer -= Time.deltaTime; // 🔥 Zamanı Azalt
            yield return null;
        }

        float fadeOutTimer = 0f;
        while (fadeOutTimer < effectFadeDuration)
        {
            fadeOutTimer += Time.deltaTime;
            currentReducerTimer -= Time.deltaTime; // 🔥 Zamanı Azalt
            float t = fadeOutTimer / effectFadeDuration;
            SetCanvasGroupAlpha(Mathf.Lerp(maxOverlayAlpha, 0f, t));
            SetEffectsAlpha(Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        currentReducerTimer = 0f;
        isSpeedReduced = false;
        ClearActiveEffects();

        if (speedEffectOverlay != null)
        {
            SetCanvasGroupAlpha(0f);
            speedEffectOverlay.gameObject.SetActive(false);
        }

        speedReductionCoroutine = null;
    }

    void SetCanvasGroupAlpha(float alpha)
    {
        if (speedEffectOverlay != null)
        {
            CanvasGroup cg = speedEffectOverlay.GetComponent<CanvasGroup>();
            if (cg == null) cg = speedEffectOverlay.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = alpha;
        }
    }

    void SetEffectsAlpha(float alpha)
    {
        foreach (GameObject effect in activeEffects)
        {
            if (effect == null) continue;
            RawImage raw = effect.GetComponent<RawImage>(); if (raw != null) { Color c = raw.color; c.a = alpha; raw.color = c; continue; }
            Image img = effect.GetComponent<Image>(); if (img != null) { Color c = img.color; c.a = alpha; img.color = c; continue; }
        }
    }

    void ClearActiveEffects()
    {
        foreach (GameObject effect in activeEffects) { if (effect != null) Destroy(effect); }
        activeEffects.Clear();
    }

    void SpawnObject()
    {
        if (lanes.Length == 0 || obstaclePrefabs == null || obstaclePrefabs.Count == 0) return;
        Camera cam = Camera.main; if (cam == null) return;
        float camHeight = 2f * cam.orthographicSize;

        // 🔥 YENİ: Artık sabit +1 değil, inspector'dan ayarlanabilen spawnYOffset kullanıyoruz.
        float spawnY = cam.transform.position.y + (camHeight / 2f) + spawnYOffset;

        // ŞERİT SEÇİMİ
        int laneIndex = -1;
        List<int> availableLanes = new List<int>();

        for (int i = 0; i < lanes.Length; i++)
        {
            if (i != blockedLaneIndex)
            {
                if (blockedLaneIndex == -1 && i == lastLaneIndex) continue;
                availableLanes.Add(i);
            }
        }

        if (availableLanes.Count == 0)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                if (i != blockedLaneIndex) availableLanes.Add(i);
            }
        }

        if (availableLanes.Count == 0) return;

        laneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
        lastLaneIndex = laneIndex;

        float xPos = lanes[laneIndex].position.x;
        Vector3 spawnPos = new Vector3(xPos, spawnY, 0f);

        // --- SPAWN TİPİ SEÇİMİ ---
        bool spawnReducer = false;
        bool spawnInvincibility = false;
        bool canSpawnAnyPowerUp = Time.time >= lastPowerUpSpawnTime + sharedPowerUpCooldown;

        if (canSpawnAnyPowerUp)
        {
            if (speedReducerPrefab != null && scrollSpeed >= minSpeedForReducer)
            {
                if (totalTimeElapsed < reducerStartDelay) { }
                else if (isSpeedReduced) { }
                else if (Time.time < lastReducerSpawnTime + reducerCooldownTime) { }
                else
                {
                    float rng = Random.value;
                    if (rng < currentReducerChance)
                    {
                        spawnReducer = true;
                        currentReducerChance = reducerSpawnChance;
                    }
                    else
                    {
                        currentReducerChance += reducerPityRate;
                        if (currentReducerChance > maxReducerChance) currentReducerChance = maxReducerChance;
                    }
                }
            }

            if (!spawnReducer && invincibilityPrefab != null)
            {
                bool isCurrentlyInvincible = false;
                if (GameManager.instance != null) isCurrentlyInvincible = GameManager.instance.IsInvincible;

                if (totalTimeElapsed < invincibilityStartDelay) { }
                else if (isCurrentlyInvincible) { }
                else if (Time.time < lastInvincibilitySpawnTime + invincibilityCooldownTime) { }
                else
                {
                    float rng = Random.value;
                    if (rng < currentInvincibilityChance)
                    {
                        spawnInvincibility = true;
                        currentInvincibilityChance = invincibilitySpawnChance;
                    }
                    else
                    {
                        currentInvincibilityChance += invincibilityPityRate;
                        if (currentInvincibilityChance > maxInvincibilityChance) currentInvincibilityChance = maxInvincibilityChance;
                    }
                }
            }
        }

        if (spawnReducer)
        {
            Instantiate(speedReducerPrefab, spawnPos, Quaternion.identity);
            lastReducerSpawnTime = Time.time;
            lastPowerUpSpawnTime = Time.time;
        }
        else if (spawnInvincibility)
        {
            Instantiate(invincibilityPrefab, spawnPos, Quaternion.identity);
            lastInvincibilitySpawnTime = Time.time;
            lastPowerUpSpawnTime = Time.time;
        }
        else
        {
            bool spawnMaganda = (!isTruckEventActive && magandaPrefabs != null && magandaPrefabs.Count > 0 && Random.value < magandaSpawnChance);

            if (spawnMaganda)
            {
                int prefabIndex = Random.Range(0, magandaPrefabs.Count);
                if (magandaPrefabs[prefabIndex] != null)
                {
                    GameObject magandaObj = Instantiate(magandaPrefabs[prefabIndex], spawnPos, Quaternion.identity);

                    Obstacle obs = magandaObj.GetComponent<Obstacle>();
                    if (obs != null)
                    {
                        obs.bonusSpeed = magandaBonusSpeed;
                    }
                }
            }
            else
            {
                int prefabIndex = Random.Range(0, obstaclePrefabs.Count);
                if (obstaclePrefabs[prefabIndex] != null)
                {
                    Instantiate(obstaclePrefabs[prefabIndex], spawnPos, Quaternion.identity);

                    if (!isTruckEventActive && coinPrefab != null && Random.value < coinSpawnChance)
                    {
                        // 1 ile maxCoinInRow (örn: 3) arasında rastgele sayıda altın çıkar
                        int coinCount = Random.Range(1, maxCoinInRow + 1);

                        for (int j = 0; j < coinCount; j++)
                        {
                            // Her bir altını "coinSpacing" kadar daha aşağı (oyuncuya doğru) kaydırarak spawn et
                            Vector3 coinPos = spawnPos + new Vector3(0, coinSpawnOffset - (j * coinSpacing), 0);
                            Instantiate(coinPrefab, coinPos, Quaternion.identity);
                        }
                    }
                }
            }
        }
    }
}