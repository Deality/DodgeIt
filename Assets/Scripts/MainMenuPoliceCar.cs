using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Ana menüde arkadan geçen polis arabası: dokunulunca boost patlamasıyla patlar ve gizli görevi tamamlar.
// Patlatıldıktan sonra (bu ve sonraki açılışlarda) onun yerine oyuncunun sahip olduğu arabalar sırayla,
// biri ekrandan tamamen çıkınca diğeri olacak şekilde aşağıdan yukarı doğru geçer.
[RequireComponent(typeof(ArabaHareketi))]
public class MainMenuPoliceCar : MonoBehaviour
{
    [Tooltip("Oyundaki boost çarpma patlaması (BoostEffectPuf). Ana menü ölçeğine göre büyütülür.")]
    public GameObject explosionPrefab;
    [Tooltip("Patlamanın, oyundaki boyutuna (arabaya oranla) göre ek büyütmesi; menüde daha belirgin olsun.")]
    public float explosionSizeMultiplier = 2f;
    [Tooltip("Patlatıldıktan sonra geçen oyuncu arabalarının hızı (dünya birimi/sn). 0 = polis arabasının hızı.")]
    public float paradeSpeed = 0f;

    public const string DestroyedKey = "MenuPoliceCarDestroyed";

    private ArabaHareketi mover;
    private SpriteRenderer sr;
    private Camera cam;
    private bool exploded;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    void Awake()
    {
        mover = GetComponent<ArabaHareketi>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        cam = Camera.main;
        if (PlayerPrefs.GetInt(DestroyedKey, 0) == 1)
        {
            exploded = true;
            HidePoliceCar();
            StartCoroutine(ParadeRoutine());
        }
    }

    void Update()
    {
        if (exploded || cam == null || sr == null) return;

        var pointer = Pointer.current;
        if (pointer == null || !pointer.press.wasPressedThisFrame) return;

        Vector2 screen = pointer.position.ReadValue();
        if (IsOverUI(screen)) return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Bounds b = sr.bounds;
        b.Expand(new Vector3(b.size.x * 0.3f, b.size.y * 0.1f, 1000f)); // parmakla vurmak kolay olsun
        if (b.Contains(new Vector3(world.x, world.y, b.center.z))) Explode();
    }

    bool IsOverUI(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screen };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }

    void Explode()
    {
        exploded = true;
        PlayerPrefs.SetInt(DestroyedKey, 1);
        PlayerPrefs.Save();

        SpawnExplosion(sr.bounds.center, sr.bounds.size.y);
        if (AudioManager.instance != null && AudioManager.instance.boostDestroySound != null)
            AudioManager.instance.PlaySFX(AudioManager.instance.boostDestroySound);

        if (MissionsManager.Instance != null) MissionsManager.Instance.AddProgress(MissionType.DestroyPoliceCar, 1);
        else MissionsManager.AddGameplayProgress(MissionType.DestroyPoliceCar, 1);

        HidePoliceCar();
        StartCoroutine(ParadeRoutine());
    }

    void HidePoliceCar()
    {
        mover.enabled = false;
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled = false;
        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }

    // Oyundaki patlama efekti oyun kamerasının ölçeğine göre yapılmış; arabanın boyuna oranlanıp büyütülür
    void SpawnExplosion(Vector3 pos, float carHeight)
    {
        if (explosionPrefab == null) return;
        float gameCarHeight = GameCarHeight();
        float scale = (gameCarHeight > 0f ? carHeight / gameCarHeight : 20f) * explosionSizeMultiplier;

        GameObject fx = Instantiate(explosionPrefab, pos, Quaternion.identity);
        fx.transform.localScale = explosionPrefab.transform.localScale * scale;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }
        foreach (var r in fx.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerID = sr.sortingLayerID;
            r.sortingOrder = sr.sortingOrder + 5;
        }
        Destroy(fx, 5f);
    }

    static float GameCarHeight()
    {
        if (CarDatabase.instance == null) return 0f;
        GameObject car = CarDatabase.instance.GetCar(0);
        var csr = car != null ? car.GetComponentInChildren<SpriteRenderer>(true) : null;
        if (csr == null || csr.sprite == null) return 0f;
        return csr.sprite.bounds.size.y * Mathf.Abs(csr.transform.lossyScale.y);
    }

    // Oyuncunun sahip olduğu arabalar sırayla aşağıdan yukarı geçer; biri ekrandan çıkmadan diğeri başlamaz
    IEnumerator ParadeRoutine()
    {
        yield return new WaitForSeconds(1.2f); // patlama bitsin

        List<Sprite> sprites = OwnedCarSprites();
        if (sprites.Count == 0 || cam == null) yield break;

        float height = sr.bounds.size.y;
        float speed = paradeSpeed > 0f ? paradeSpeed : mover.hareketHizi;
        float[] lanes = mover.seritXKoordinatlari != null && mover.seritXKoordinatlari.Length > 0
            ? mover.seritXKoordinatlari : new[] { transform.position.x };

        var go = new GameObject("MenuParadeCar");
        go.layer = gameObject.layer;
        var car = go.AddComponent<SpriteRenderer>();
        car.sortingLayerID = sr.sortingLayerID;
        car.sortingOrder = sr.sortingOrder;

        int next = 0;
        while (true)
        {
            car.sprite = sprites[next % sprites.Count];
            next++;
            float spriteH = car.sprite.bounds.size.y;
            go.transform.localScale = Vector3.one * (spriteH > 0f ? height / spriteH : 1f);

            float camY = cam.transform.position.y, half = cam.orthographicSize;
            float x = lanes[Random.Range(0, lanes.Length)];
            go.transform.position = new Vector3(x, camY - half - height, transform.position.z);

            // Ekranın üstünden tamamen çıkana kadar yukarı gider
            while (car.bounds.min.y < cam.transform.position.y + cam.orthographicSize)
            {
                go.transform.position += Vector3.up * speed * Time.deltaTime;
                yield return null;
            }
        }
    }

    static List<Sprite> OwnedCarSprites()
    {
        var list = new List<Sprite>();
        if (CarDatabase.instance == null || CarDatabase.instance.carPrefabs == null) return list;
        for (int i = 0; i < CarDatabase.instance.carPrefabs.Length; i++)
        {
            bool owned = i == 0 || PlayerPrefs.GetInt(MarketItemType.Car + "_Purchased_" + i, 0) == 1;
            if (!owned) continue;
            var csr = CarDatabase.instance.carPrefabs[i] != null ? CarDatabase.instance.carPrefabs[i].GetComponentInChildren<SpriteRenderer>(true) : null;
            if (csr != null && csr.sprite != null) list.Add(csr.sprite);
        }
        return list;
    }
}
