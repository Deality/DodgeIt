using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NearMissDetector : MonoBehaviour
{
    [Header("Görsel Ayarlar (Yazı)")]
    public Vector3 textOffset = new Vector3(0, 3.0f, 0);
    public GameObject customTextPrefab;
    public TMP_FontAsset customFont;
    public Color textColor = new Color(1f, 0.5f, 0f);

    [Header("Makas Mesajları")]
    public string[] compliments = new string[] {
        "Nice Weaving!",
        "Awesome!",
        "Close Call!",
        "Perfect!",
        "Insane!",
        "Wow!"
    };

    [Header("Hayalet (Ghost) Efekti")]
    public bool enableGhostEffect = true;
    public float ghostFadeDuration = 0.5f;
    public float ghostScaleMultiplier = 1.3f;
    public Color ghostColor = new Color(1f, 1f, 1f, 0.6f);
    public float ghostDriftMultiplier = 1.0f;
    public float ghostSpawnDelay = 0.1f;

    [Header("Gecikme Ayarı (Crash Önlemi)")]
    public float displayDelay = 0.2f;

    // Yanından geçilmekte olan araçlar. Near miss görev ilerlemesi araç sensörden ÇIKINCA (yani gerçekten
    // sıyrılıp geçilince) yazılır; arada kaza olduysa hiç yazılmaz.
    private class PendingNearMiss
    {
        public int crashCountAtEnter;
        public bool isTruck;
        public int collidersInside; // aracın birden fazla collider'ı olabilir; hepsi çıkınca "geçildi" sayılır
    }
    // Anahtar: aracın kök objesi (Obstacle / FastMover script'inin olduğu obje)
    private Dictionary<GameObject, PendingNearMiss> processedObstacles = new Dictionary<GameObject, PendingNearMiss>();

    [Tooltip("Araç geçildikten sonra near miss'in görevlere yazılması için kazasız geçmesi gereken süre (saniye). Kazadan hemen önceki near miss sayılmaz.")]
    public float confirmDelay = 0.6f;

    // Bir collider'dan aracın kök objesini bulur
    private static GameObject ObstacleRoot(Collider2D other)
    {
        Obstacle o = other.GetComponentInParent<Obstacle>();
        if (o != null) return o.gameObject;
        FastMover f = other.GetComponentInParent<FastMover>();
        if (f != null) return f.gameObject;
        return other.gameObject;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (GameManager.instance != null && !GameManager.instance.isGameActive) return;

        if (GameManager.instance != null)
        {
            bool isGrace = false;
            if (CarController2D.instance != null)
            {
                isGrace = CarController2D.instance.isBoostGracePeriod;
            }

            if (GameManager.instance.IsInvincible || GameManager.instance.isBoosting || isGrace)
            {
                return;
            }
        }

        // 🔥 GÜNCELLEME: Çarpan nesnenin engel olduğunu sadece Tag ile değil, Obstacle veya FastMover scriptinin
        // varlığı ile de teyit ediyoruz. Bu sayede Unity editöründeki sinsi etiket hataları Nearmiss'i engelleyemez.
        bool isObstacle = other.CompareTag("Obstacle") ||
                          other.GetComponent<Obstacle>() != null ||
                          other.GetComponent<FastMover>() != null;

        if (isObstacle)
        {
            GameObject root = ObstacleRoot(other);
            if (processedObstacles.TryGetValue(root, out PendingNearMiss existing))
            {
                existing.collidersInside++; // aynı aracın başka bir collider'ı
            }
            else
            {
                TriggerMessage();
                StartCoroutine(TriggerGhostEffectRoutine());
                processedObstacles.Add(root, new PendingNearMiss
                {
                    crashCountAtEnter = GameManager.instance != null ? GameManager.instance.CrashCount : 0,
                    isTruck = other.GetComponentInParent<BigTruck>() != null,
                    collidersInside = 1
                });

                StartCoroutine(PlayNearMissSoundWithDelay());
                StartCoroutine(TriggerStreakWithDelay());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Girişte sayılmayan collider'lar (engel olmayan alt parçalar) çıkışta da sayılmaz
        bool isObstacle = other.CompareTag("Obstacle") ||
                          other.GetComponent<Obstacle>() != null ||
                          other.GetComponent<FastMover>() != null;
        if (!isObstacle) return;

        GameObject root = ObstacleRoot(other);
        if (!processedObstacles.TryGetValue(root, out PendingNearMiss pending)) return;
        if (--pending.collidersInside > 0) return; // aracın bir parçası hâlâ yanımızda
        processedObstacles.Remove(root);

        // Araç tamamen geçildi. Görevlere hemen yazmıyoruz: kısa bir süre daha kazasız geçmeli.
        // Sayaç GameManager üzerinde çalışır: sensör objesi kapanırken (ör. kaza anında araba gizlenirken)
        // kendi üzerinde coroutine başlatılamaz.
        if (GameManager.instance != null) GameManager.instance.StartCoroutine(ConfirmNearMissRoutine(pending));
    }

    // Near miss'i ancak araç geçildikten sonra confirmDelay boyunca kaza olmazsa görevlere yazar.
    // Kaza anındaki ya da kazadan hemen önceki near miss böylece hiç sayılmaz.
    IEnumerator ConfirmNearMissRoutine(PendingNearMiss pending)
    {
        yield return new WaitForSeconds(confirmDelay);

        GameManager gm = GameManager.instance;
        if (gm != null && (!gm.isGameActive || gm.IsGameOver || gm.CrashCount != pending.crashCountAtEnter)) yield break;

        MissionsManager.AddGameplayProgress(MissionType.TriggerNearmiss, 1);
        // Gizli görev: tıra near miss
        if (pending.isTruck) MissionsManager.AddGameplayProgress(MissionType.NearMissTruck, 1);
    }

    IEnumerator TriggerStreakWithDelay()
    {
        yield return new WaitForSeconds(displayDelay);
        // Bu sürede kaza olduysa seri de sayılmaz
        if (GameManager.instance != null && GameManager.instance.isGameActive && !GameManager.instance.IsGameOver)
            GameManager.instance.TriggerNearMissStreak();
    }

    // 🔥 Near miss sesi hafifçe geciktirilir; eğer bu süre içinde oyuncu asıl çarpışmayı
    // yaparsa (Game Over), near miss sesi çalınmaz. Böylece kaza sesi ve efektiyle
    // üst üste binip birbirine karışmaz.
    IEnumerator PlayNearMissSoundWithDelay()
    {
        yield return new WaitForSeconds(displayDelay);

        if (GameManager.instance != null && (!GameManager.instance.isGameActive || GameManager.instance.IsGameOver))
            yield break;

        if (AudioManager.instance != null && AudioManager.instance.nearMissSound != null)
            AudioManager.instance.PlayFeedback(AudioManager.instance.nearMissSound);
    }

    void TriggerMessage()
    {
        if (compliments.Length > 0)
        {
            int randomIndex = Random.Range(0, compliments.Length);
            string selectedMessage = compliments[randomIndex];
            StartCoroutine(ShowMessageWithDelay(selectedMessage));
        }
    }

    IEnumerator TriggerGhostEffectRoutine()
    {
        if (!enableGhostEffect) yield break;

        yield return new WaitForSeconds(ghostSpawnDelay);

        if (GameManager.instance != null && (!GameManager.instance.isGameActive || GameManager.instance.IsGameOver))
            yield break;

        SpriteRenderer playerSprite = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : GetComponent<SpriteRenderer>();

        if (playerSprite != null)
        {
            GameObject ghostObj = new GameObject("NearMissGhost");
            ghostObj.transform.position = playerSprite.transform.position;
            ghostObj.transform.rotation = playerSprite.transform.rotation;
            ghostObj.transform.localScale = playerSprite.transform.lossyScale;

            ghostObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
            ghostSr.sprite = playerSprite.sprite;
            ghostSr.flipX = playerSprite.flipX;
            ghostSr.flipY = playerSprite.flipY;
            ghostSr.color = ghostColor;

            ghostSr.sortingLayerID = playerSprite.sortingLayerID;
            ghostSr.sortingOrder = playerSprite.sortingOrder - 1;

            StartCoroutine(FadeGhost(ghostObj, ghostSr));
        }
    }

    IEnumerator FadeGhost(GameObject ghostObj, SpriteRenderer ghostSr)
    {
        float timer = 0f;
        Vector3 startScale = ghostObj.transform.localScale;
        Vector3 endScale = startScale * ghostScaleMultiplier;
        Color startColor = ghostSr.color;

        while (timer < ghostFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / ghostFadeDuration;

            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, Mathf.Lerp(startColor.a, 0f, t));

            if (ghostObj != null)
            {
                ghostObj.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                float currentSpeed = 0f;
                if (ObstacleManager.instance != null)
                {
                    currentSpeed = ObstacleManager.scrollSpeed;
                }

                ghostObj.transform.Translate(Vector3.down * currentSpeed * ghostDriftMultiplier * Time.deltaTime, Space.World);
            }

            yield return null;
        }

        if (ghostObj != null) Destroy(ghostObj);
    }

    IEnumerator ShowMessageWithDelay(string message)
    {
        yield return new WaitForSeconds(displayDelay);

        if (GameManager.instance != null && GameManager.instance.isGameActive && !GameManager.instance.IsGameOver)
        {
            Vector3 basePosition = transform.parent != null ? transform.parent.position : transform.position;
            Vector3 spawnPos = basePosition + textOffset;

            if (customTextPrefab != null)
            {
                GameObject textObj = PoolManager.Spawn(customTextPrefab, spawnPos, Quaternion.identity);

                TextMeshPro[] tmps = textObj.GetComponentsInChildren<TextMeshPro>();
                foreach (var tmp in tmps)
                {
                    tmp.text = message;
                    if (customFont != null) tmp.font = customFont;
                    tmp.color = textColor;
                }

                TextMeshProUGUI[] tmpUIs = textObj.GetComponentsInChildren<TextMeshProUGUI>();
                foreach (var tmpUI in tmpUIs)
                {
                    tmpUI.text = message;
                    if (customFont != null) tmpUI.font = customFont;
                    tmpUI.color = textColor;
                }
            }
            else
            {
                GameManager.instance.ShowFloatingText(message, spawnPos);
            }
        }
    }
}