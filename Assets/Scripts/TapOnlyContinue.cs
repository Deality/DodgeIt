using UnityEngine;
using UnityEngine.EventSystems;

// Unity's Button fires OnClick on pointer-up over the same object regardless of how far the
// finger moved in between - so a swipe across the paused explanation overlay was dismissing it
// just like a tap. This component gates that: only a pointer down/up pair that stayed within
// maxTapMovement counts as a tap and advances the tutorial; a swipe is ignored.
[RequireComponent(typeof(UnityEngine.UI.Button))]
public class TapOnlyContinue : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float maxTapMovement = 40f;

    private Vector2 pointerDownPos;
    private bool isPointerDown;

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPos = eventData.position;
        isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPointerDown) return;
        isPointerDown = false;

        if (Vector2.Distance(pointerDownPos, eventData.position) <= maxTapMovement)
        {
            TutorialManager.instance?.OnTapToContinue();
        }
    }
}
