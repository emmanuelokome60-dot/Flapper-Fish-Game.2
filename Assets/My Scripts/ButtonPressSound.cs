using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plays the click on the press instead of the release. Added at runtime by GameManager to every
// button under the Canvas.
//
// This exists instead of Unity's EventTrigger, which was the obvious one line and was wrong.
// EventTrigger implements EVERY handler interface, drag included, so the EventSystem stopped
// looking for a drag handler the moment it reached a button and never reached the ScrollRect
// above it: dragging the fish shelf by a BUY or SELECT badge -- the middle third of every card
// -- scrolled nothing. Handling only IPointerDownHandler leaves the drag to bubble, which is
// exactly what Unity's own Button does.
//
// Class and file name must stay in step, or Unity reports this as a missing script on the next
// reimport. Same trap SpotLight2D.cs documents.
[RequireComponent(typeof(Button))]
public class ButtonPressSound : MonoBehaviour, IPointerDownHandler
{
    private Button button;

    // - Caches the Button this sits on, so the press handler does not look it up every tap.
    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // - Plays the click the moment the finger lands, rather than on release.
    // - Refuses when the button is not interactable, which is what keeps a greyed-out BUY silent.
    public void OnPointerDown(PointerEventData eventData)
    {
        // Button.onClick checks this itself; on the press there is nothing checking it for us,
        // and without it the shop's greyed-out BUY badges would click when the player is broke.
        if (button == null || !button.interactable) return;

        if (GameManager.Instance != null) GameManager.Instance.PlayClick();
    }
}
