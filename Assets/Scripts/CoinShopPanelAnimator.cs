using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoinShopPanelAnimator : MonoBehaviour
{
    public Image dimBackground;
    public RectTransform box;
    public float fadeDuration = 0.25f;
    public float slideSpeed = 4f;

    float targetAlpha;
    Coroutine fadeRoutine;
    Coroutine slideRoutine;

    void Awake()
    {
        if (dimBackground != null) targetAlpha = dimBackground.color.a;
    }

    public void Open()
    {
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        if (dimBackground != null)
        {
            Color c = dimBackground.color;
            c.a = 0f;
            dimBackground.color = c;
        }

        if (box != null) box.anchoredPosition = new Vector2(0, CanvasHeight());

        Restart(ref fadeRoutine, FadeRoutine(targetAlpha));
        Restart(ref slideRoutine, SlideRoutine(Vector2.zero, null));
    }

    public void Close()
    {
        Restart(ref fadeRoutine, FadeRoutine(0f));
        Restart(ref slideRoutine, SlideRoutine(new Vector2(0, CanvasHeight()), () => gameObject.SetActive(false)));
    }

    void Restart(ref Coroutine routine, IEnumerator body)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(body);
    }

    float CanvasHeight()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.GetComponent<RectTransform>().rect.height : Screen.height;
    }

    IEnumerator FadeRoutine(float target)
    {
        if (dimBackground == null) yield break;
        float start = dimBackground.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(fadeDuration, 0.0001f);
            Color c = dimBackground.color;
            c.a = Mathf.Lerp(start, target, t);
            dimBackground.color = c;
            yield return null;
        }
        Color final = dimBackground.color;
        final.a = target;
        dimBackground.color = final;
    }

    IEnumerator SlideRoutine(Vector2 targetPos, System.Action onComplete)
    {
        if (box == null) yield break;
        Vector2 start = box.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * slideSpeed;
            float easeT = t * t * (3f - 2f * t);
            box.anchoredPosition = Vector2.Lerp(start, targetPos, easeT);
            yield return null;
        }
        box.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }
}
