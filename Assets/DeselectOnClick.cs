using UnityEngine;
using UnityEngine.EventSystems;

// Drops the EventSystem selection as soon as a click finishes.
//
// Unity UI keeps a clicked button selected, and this project's Submit axis lists
// `space` as its alternate button — the same key the game uses for play/pause.
// Without this, clicking RANDOM and then pressing space re-fires the button: the
// board re-randomises and re-pauses, so the simulation looks stuck.
//
// Added by SavePanelBuilder to every button it creates.
public class DeselectOnClick : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        EventSystem events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject == gameObject)
            events.SetSelectedGameObject(null);
    }
}
