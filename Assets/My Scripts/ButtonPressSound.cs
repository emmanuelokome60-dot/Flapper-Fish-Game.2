using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Clicks on finger-down instead of finger-up, which feels faster. GameManager adds this to every
// button at startup.
[RequireComponent(typeof(Button))]
public class ButtonPressSound : MonoBehaviour, IPointerDownHandler
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Unity only checks "is this button switched on?" on finger-UP. Nothing checks it on the
        // way down, so without this the shop's greyed-out BUY buttons would click when broke.
        if (button == null || !button.interactable) return;

        if (GameManager.Instance != null) GameManager.Instance.PlayClick();
    }
}
