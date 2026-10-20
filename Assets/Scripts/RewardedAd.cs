using System;
using System.Collections;
using UnityEngine;

// Ödüllü reklam gösterimi için tek giriş noktası.
// TODO: gerçek bir reklam SDK'sı (ör. AdMob) eklenince Show içi o SDK'nın ödüllü reklam çağrısıyla değiştirilecek.
// Şimdilik GameManager'daki revive reklamı gibi kısa bir beklemeden sonra her zaman başarılı sayılır.
public static class RewardedAd
{
    // host: bekleme coroutine'ini çalıştıracak aktif bir bileşen. onRewarded yalnızca reklam sonuna kadar izlenirse çağrılır.
    public static void Show(MonoBehaviour host, Action onRewarded, Action onFailed = null)
    {
        if (host == null || !host.isActiveAndEnabled) { onFailed?.Invoke(); return; }
        host.StartCoroutine(SimulateRoutine(onRewarded));
    }

    static IEnumerator SimulateRoutine(Action onRewarded)
    {
        yield return new WaitForSecondsRealtime(0.3f);
        onRewarded?.Invoke();
    }
}
