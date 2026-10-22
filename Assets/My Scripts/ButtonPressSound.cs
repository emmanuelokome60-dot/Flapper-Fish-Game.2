using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Plays the click on the press instead of the release. Added at runtime by GameManager to every
// button under the Canvas.
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
