using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Jenerik nesne havuzu. Oyun boyunca sürekli Instantiate/Destroy edilen objeleri
// (engeller, coinler, efektler, uçan yazılar) yeniden kullanarak GC (çöp toplama)
// duraksamalarını azaltır. Yüksek yenileme hızlı ekranlarda (90/120Hz) her karenin
// bütçesi çok kısa olduğu için bu duraksamalar özellikle fark edilir hale gelir.
//
// Havuzlanan objelerin scriptleri, Start() yerine OnEnable() içinde "spawn'da
// sıfırlanması gereken" durumu (pozisyona bağlı değerler, flag'ler vb.) ayarlamalı;
// Start() havuzdan tekrar kullanılan objelerde ikinci kez ÇALIŞMAZ.
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;
        EnsureInstance();

        GameObject obj = Instance.GetFromPool(prefab);
        Transform t = obj.transform;
        t.SetParent(parent, false);

        // 🔥 ÖNEMLİ: Bir obje bir kez PoolManager'ın altına (DontDestroyOnLoad) girdikten
        // sonra SetParent(null) onu normal sahneye GERİ TAŞIMAZ; Unity'de obje "kalıcı"
        // sahnede takılı kalır. Ebeveyni yoksa (kök obje), aktif sahneye açıkça taşımazsak
        // obje bir sonraki sahne yüklemesinde de aktif ve çarpışabilir halde hayatta kalır
        // -> bu da restart sonrası oyuncunun anında "hayalet" bir engele çarpmasına yol açar.
        if (parent == null)
        {
            SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
        }

        t.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        return obj;
    }

    public static void Despawn(GameObject instanceObj)
    {
        if (instanceObj == null) return;

        if (Instance == null || !Instance.instanceToPrefab.TryGetValue(instanceObj, out GameObject prefab))
        {
            // Havuza ait değil (örn. pooling dışında yaratıldı) -> güvenli şekilde yok et
            Destroy(instanceObj);
            return;
        }

        instanceObj.SetActive(false);
        instanceObj.transform.SetParent(Instance.transform, false);
        Instance.pools[prefab].Enqueue(instanceObj);
    }

    // Belirli bir süre sonra havuza iade eder (örn. parçacık efektleri için).
    public static void DespawnAfter(GameObject instanceObj, float delay)
    {
        if (instanceObj == null) return;
        EnsureInstance();
        Instance.StartCoroutine(Instance.DespawnAfterRoutine(instanceObj, delay));
    }

    private IEnumerator DespawnAfterRoutine(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Despawn(obj);
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        while (queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            if (obj != null) return obj;
        }

        // 🔥 ÖNEMLİ: Instantiate() prefab aktifse objeyi HEMEN aktif olarak yaratır ve
        // Awake/OnEnable/Start bu satırda, biz konumunu düzeltmeden ÖNCE çalışır. Bu da
        // ilk kez havuzlanan objelerin (örn. bir prefab'ın ilk spawn'ı) yanlış (prefab'ın
        // kendi orijinal) pozisyonuyla "OnEnable sıfırlaması" yapmasına ve şeritten dışarı
        // kaymasına yol açıyordu. Hemen deaktive edip Spawn() içinde asıl pozisyon
        // atandıktan SONRA tekrar aktive ederek OnEnable'ın her zaman doğru konumla
        // çalışmasını garantiliyoruz.
        GameObject created = Instantiate(prefab);
        created.SetActive(false);
        instanceToPrefab[created] = prefab;
        return created;
    }

    private static void EnsureInstance()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("PoolManager");
        Instance = go.AddComponent<PoolManager>();
    }
}
