using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Clicks on finger-down instead of finger-up, which feels faster.
// GameManager adds this to every button at startup.
[RequireComponent(typeof(Button))]
public class ButtonPressSound : MonoBehaviour, IPointerDownHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // A finger went down on this button. Plays the click.
    public void OnPointerDown(PointerEventData eventData)
    {
        // Unity only checks a button is switched on at finger-UP, so
        // without this the greyed-out BUY button clicks when you are broke.
        if (button == null || !button.interactable) return;

        if (GameManager.Instance != null) GameManager.Instance.PlayClick();
    }
}
