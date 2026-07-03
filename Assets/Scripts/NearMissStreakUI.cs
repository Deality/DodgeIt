using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NearMissStreakUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI multiplierText;
    public Image timerBarFill;
    public CanvasGroup canvasGroup;

    [Header("Colors")]
    public Color color1 = new Color(1f, 0.85f, 0f, 1f);
    public Color color2 = new Color(1f, 0.50f, 0f, 1f);
    public Color color3 = new Color(1f, 0.15f, 0f, 1f);

    [Header("Fade")]
    public float fadeSpeed = 8f;

    private static readonly string[] Labels = { "", "x1.5", "x2", "x4" };

    void Update()
    {
        if (GameManager.instance == null) return;

        bool active = GameManager.instance.NearMissStreakCount > 0
                   && GameManager.instance.isGameActive
                   && !GameManager.instance.IsGameOver;

        float targetAlpha = active ? 1f : 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

        if (!active) return;

        int count    = GameManager.instance.NearMissStreakCount;
        float timer  = GameManager.instance.NearMissStreakTimer;
        float total  = GameManager.instance.NearMissCurrentDuration;

        Color c = count == 1 ? color1 : count == 2 ? color2 : color3;

        if (multiplierText != null)
        {
            multiplierText.text  = count <= 3 ? Labels[count] : Labels[3];
            multiplierText.color = c;
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = Mathf.Clamp01(timer / total);
            timerBarFill.color      = c;
        }
    }
}
