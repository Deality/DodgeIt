using UnityEngine;
using TMPro;
using System.Collections;

// Runs a short, first-run-only tutorial during gameplay. Other scripts call the
// NotifyXxx() hooks when the relevant gameplay event happens (swipe, boost, shield
// pickup, speed reducer pickup); this manager decides whether that event is currently
// relevant to the tutorial and reacts.
//
// The core sequence: "SWIPE LEFT OR RIGHT" shows live (unpaused) right at the start.
// Once the player actually swipes and that text has fully faded out, the game pauses and
// explains the near miss/multiplier mechanic (small textbox, tap anywhere to continue).
// After that, a few seconds of live play pass before "DOUBLE TAP" appears (unpaused);
// once boost activates, the player gets a couple of live seconds to feel it, then the
// game pauses to explain what boost does (tap anywhere to continue), completing the
// sequence. It runs once ever, gated by Tutorial_MainDone.
//
// The shield/speed-reducer explanations are a separate, independent one-time tip each
// (their own PlayerPrefs flags), using the same paused/tap-to-continue textbox: the
// first time a pickup is collected, whenever that happens to be.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    private enum Step
    {
        InitialSwipeIntro,
        NearMissExplain,
        WaitingToPromptBoost,
        DoubleTapPrompt,
        BoostExplain,
        Done
    }

    [Header("Live On-Screen Prompt - short commands (SWIPE / DOUBLE TAP), never pauses")]
    public GameObject promptTextObject;
    public TextMeshProUGUI promptText;
    public CanvasGroup promptCanvasGroup;
    public float promptFadeDuration = 0.4f;

    [Header("Paused Textbox - near miss/boost explanations + shield/reducer tips (tap anywhere to continue)")]
    public GameObject explanationOverlay;
    public GameObject explanationBox;
    public TextMeshProUGUI explanationText;

    [Header("Messages")]
    [TextArea] public string nearMissCombinedText = "Dodge close to a car for a NEAR MISS - chain them together to build a bigger score multiplier!";
    [TextArea] public string boostExplainText = "Boost gives you a burst of speed and smashes through any car in your way!";
    [TextArea] public string shieldExplainText = "Shield makes you invincible for a few seconds - crash through anything safely!";
    [TextArea] public string reducerExplainText = "This slows down traffic for a while, giving you room to breathe.";

    public string swipePromptText = "SWIPE LEFT OR RIGHT";
    public string doubleTapPromptText = "DOUBLE TAP";

    [Tooltip("Near miss explained - how long to wait before the DOUBLE TAP prompt appears.")]
    public float delayBeforeBoostPrompt = 3f;

    [Tooltip("Boost activated - how long the player gets to feel it live before the game pauses to explain it.")]
    public float delayBeforeBoostExplain = 2f;

    private const string MainDoneKey = "Tutorial_MainDone";
    private const string ShieldDoneKey = "Tutorial_ShieldDone";
    private const string ReducerDoneKey = "Tutorial_ReducerDone";

    private Step step = Step.InitialSwipeIntro;
    private bool mainDone;
    private bool oneShotShowing = false;
    private Coroutine pendingRoutine;
    private Coroutine promptFadeRoutine;

    private bool IsMainActive => !mainDone && step != Step.Done;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        mainDone = PlayerPrefs.GetInt(MainDoneKey, 0) == 1;
        step = mainDone ? Step.Done : Step.InitialSwipeIntro;

        if (explanationOverlay != null) explanationOverlay.SetActive(false);
        if (promptTextObject != null) promptTextObject.SetActive(false);
        if (promptCanvasGroup != null) promptCanvasGroup.alpha = 0f;
        if (explanationBox != null) explanationBox.SetActive(true);
    }

    void Start()
    {
        if (step == Step.InitialSwipeIntro)
        {
            ShowPrompt(swipePromptText);
        }
    }

    // --- Called by CarController2D after a successful lane change ---
    public void NotifyPlayerSwiped()
    {
        if (!IsMainActive || step != Step.InitialSwipeIntro) return;

        step = Step.NearMissExplain;

        // Wait for the "SWIPE LEFT OR RIGHT" text to fully fade out before pausing to
        // explain the near miss mechanic - the two never overlap.
        HidePrompt(() =>
        {
            if (!IsMainActive || step != Step.NearMissExplain) return;
            if (GameManager.instance != null && GameManager.instance.IsGameOver) return;

            ShowExplanation(nearMissCombinedText);
        });
    }

    private IEnumerator ShowBoostPromptAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeBoostPrompt);
        if (!IsMainActive || step != Step.WaitingToPromptBoost) yield break;

        step = Step.DoubleTapPrompt;
        ShowPrompt(doubleTapPromptText);
    }

    // --- Called by CarController2D right after Boost activates ---
    public void NotifyBoostActivated()
    {
        if (!IsMainActive || step != Step.DoubleTapPrompt) return;

        step = Step.BoostExplain;
        HidePrompt();

        if (pendingRoutine != null) StopCoroutine(pendingRoutine);
        pendingRoutine = StartCoroutine(ExplainBoostAfterDelay());
    }

    private IEnumerator ExplainBoostAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeBoostExplain);
        if (!IsMainActive || step != Step.BoostExplain) yield break;
        if (GameManager.instance != null && GameManager.instance.IsGameOver) yield break;

        ShowExplanation(boostExplainText);
    }

    private void CompleteMain()
    {
        step = Step.Done;
        mainDone = true;
        PlayerPrefs.SetInt(MainDoneKey, 1);
        PlayerPrefs.Save();
    }

    // --- Called by Invincibility on first pickup ---
    public void NotifyShieldCollected()
    {
        if (PlayerPrefs.GetInt(ShieldDoneKey, 0) == 1) return;
        PlayerPrefs.SetInt(ShieldDoneKey, 1);
        PlayerPrefs.Save();
        ShowOneShotExplanation(shieldExplainText);
    }

    // --- Called by SpeedReducer on first pickup ---
    public void NotifyReducerCollected()
    {
        if (PlayerPrefs.GetInt(ReducerDoneKey, 0) == 1) return;
        PlayerPrefs.SetInt(ReducerDoneKey, 1);
        PlayerPrefs.Save();
        ShowOneShotExplanation(reducerExplainText);
    }

    private void ShowOneShotExplanation(string message)
    {
        if (oneShotShowing) return;

        oneShotShowing = true;
        ShowExplanation(message);
    }

    // --- Called by the full-screen overlay Button ---
    public void OnTapToContinue()
    {
        if (oneShotShowing)
        {
            oneShotShowing = false;
            HideExplanation();
            return;
        }

        if (!IsMainActive) return;

        switch (step)
        {
            case Step.NearMissExplain:
                HideExplanation();
                step = Step.WaitingToPromptBoost;

                if (pendingRoutine != null) StopCoroutine(pendingRoutine);
                pendingRoutine = StartCoroutine(ShowBoostPromptAfterDelay());
                break;

            case Step.BoostExplain:
                HideExplanation();
                CompleteMain();
                break;
        }
    }

    private void ShowExplanation(string message)
    {
        if (explanationBox != null) explanationBox.SetActive(true);
        if (explanationText != null) explanationText.text = message;
        if (explanationOverlay != null) explanationOverlay.SetActive(true);
        Time.timeScale = 0f;
    }

    private void HideExplanation()
    {
        if (explanationOverlay != null) explanationOverlay.SetActive(false);
        Time.timeScale = 1f;
    }

    private void ShowPrompt(string message)
    {
        if (promptText != null) promptText.text = message;
        if (promptTextObject != null) promptTextObject.SetActive(true);
        FadePrompt(1f);
    }

    private void HidePrompt(System.Action onComplete = null)
    {
        FadePrompt(0f, onComplete);
    }

    private void FadePrompt(float targetAlpha, System.Action onComplete = null)
    {
        if (promptCanvasGroup == null)
        {
            // No CanvasGroup assigned - fall back to an instant toggle.
            if (promptTextObject != null) promptTextObject.SetActive(targetAlpha > 0.5f);
            onComplete?.Invoke();
            return;
        }

        if (promptFadeRoutine != null) StopCoroutine(promptFadeRoutine);
        promptFadeRoutine = StartCoroutine(FadePromptRoutine(targetAlpha, onComplete));
    }

    private IEnumerator FadePromptRoutine(float targetAlpha, System.Action onComplete)
    {
        float startAlpha = promptCanvasGroup.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / promptFadeDuration;
            promptCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t));
            yield return null;
        }

        promptCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f && promptTextObject != null)
            promptTextObject.SetActive(false);

        onComplete?.Invoke();
    }

    // --- Called by GameManager right before the Game Over panel appears ---
    public void HideAllLiveUI()
    {
        if (pendingRoutine != null) StopCoroutine(pendingRoutine);
        HidePrompt();
    }
}
