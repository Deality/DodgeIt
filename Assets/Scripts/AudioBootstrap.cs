using UnityEngine;

// AudioManager eskiden yalnızca oyun sahnesindeydi; uygulama ilk açıldığında (yükleme ekranı -> ana menü)
// hiç ses çalmıyordu. Bu sınıf, ilk sahne yüklenmeden önce Resources/AudioManager prefabından bir tane
// oluşturur; AudioManager kendini DontDestroyOnLoad yaptığı için tüm sahnelerde aynı nesne kullanılır.
public static class AudioBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateAudioManager()
    {
        if (AudioManager.instance != null) return;

        GameObject prefab = Resources.Load<GameObject>("AudioManager");
        if (prefab == null)
        {
            Debug.LogError("Resources/AudioManager prefabı bulunamadı - sesler çalmayacak.");
            return;
        }

        GameObject go = Object.Instantiate(prefab);
        go.name = "AudioManager";
    }
}
