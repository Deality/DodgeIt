using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ödüllü reklam gösterimi için tek giriş noktası; reklamı AdMobRewarded gösterir.
// İnternet yoksa reklam açılmaz; ekranda kısa bir "NO INTERNET CONNECTION" uyarısı çıkar.
// Reklam henüz yüklenmediyse "AD NOT READY" uyarısı çıkar ve yükleme yeniden denenir.
public static class RewardedAd
{
    public static bool IsAvailable => Application.internetReachability != NetworkReachability.NotReachable;

    // host: çağıran aktif bileşen (kapanmışsa reklam açılmaz). onRewarded yalnızca reklam sonuna kadar izlenirse çağrılır.
    public static void Show(MonoBehaviour host, Action onRewarded, Action onFailed = null)
    {
        if (!IsAvailable)
        {
            ShowNoInternetNotice();
            onFailed?.Invoke();
            return;
        }
        if (host == null || !host.isActiveAndEnabled) { onFailed?.Invoke(); return; }
        if (!AdMobRewarded.IsReady)
        {
            ShowAdNotReadyNotice();
            AdMobRewarded.Load();
            onFailed?.Invoke();
            return;
        }
        AdMobRewarded.Show(earned =>
        {
            if (earned) onRewarded?.Invoke();
            else onFailed?.Invoke();
        });
    }

    // --- İNTERNET YOK UYARISI ---
    // Her şeyin önünde, ekranın ortasında kısa süre görünen yazı. Kendi kanvasında çalışır; dokunuşları engellemez.
    private static NoticeView notice;

    public static void ShowNoInternetNotice() => ShowNotice("NO INTERNET CONNECTION");
    public static void ShowAdNotReadyNotice() => ShowNotice("AD NOT READY, TRY AGAIN");

    public static void ShowNotice(string message)
    {
        if (notice == null)
        {
            var go = new GameObject("NoInternetNotice", typeof(Canvas), typeof(CanvasScaler));
            UnityEngine.Object.DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32001;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;
            notice = go.AddComponent<NoticeView>();
            notice.Build();
        }
        notice.Play(message);
    }

    private class NoticeView : MonoBehaviour
    {
        private CanvasGroup group;
        private TextMeshProUGUI label;
        private Coroutine routine;

        public void Build()
        {
            var box = new GameObject("Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            box.transform.SetParent(transform, false);
            var img = box.GetComponent<Image>();
            img.color = new Color32(3, 0, 92, 250);
            img.raycastTarget = false;
            var rt = (RectTransform)box.transform;
            rt.sizeDelta = new Vector2(820f, 150f);
            var outline = box.AddComponent<Outline>();
            outline.effectColor = new Color32(230, 70, 70, 255);
            outline.effectDistance = new Vector2(5f, -5f);
            group = box.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(box.transform, false);
            var t = label = textGo.GetComponent<TextMeshProUGUI>();
            // Oyunun yazı tipi (Akira) yüklüyse onu kullan
            foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (f.name.StartsWith("Akira")) { t.font = f; break; }
            t.text = "NO INTERNET CONNECTION";
            t.fontSize = 44f;
            t.enableAutoSizing = true; t.fontSizeMin = 20f; t.fontSizeMax = 44f;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.white;
            t.raycastTarget = false;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(30f, 10f); trt.offsetMax = new Vector2(-30f, -10f);

            group.alpha = 0f;
        }

        public void Play(string message)
        {
            label.text = message;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Routine());
        }

        IEnumerator Routine()
        {
            var rt = (RectTransform)group.transform;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.15f)
            {
                group.alpha = t;
                rt.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, t);
                yield return null;
            }
            group.alpha = 1f; rt.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(1.6f);
            for (float t = 1f; t > 0f; t -= Time.unscaledDeltaTime / 0.3f)
            {
                group.alpha = t;
                yield return null;
            }
            group.alpha = 0f;
            routine = null;
        }
    }
}
