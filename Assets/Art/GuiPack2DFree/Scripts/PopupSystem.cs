using System.Collections.Generic;
using UnityEngine;

namespace GUIPack2DFree
{
    public class PopupSystem : MonoBehaviour
    {
        [SerializeField]
        Canvas popupCanvas;
        [SerializeField]
        GameObject panelPrefab;

        Stack<GameObject> panels = new Stack<GameObject>();
        Stack<GameObject> popups = new Stack<GameObject>();

        public void OpenPopup(GameObject popupPrefab)
        {
            if (HasOpenPopups())
            {
                return;
            }

            GameObject panel = Instantiate(panelPrefab);
            panel.name = "Panel";

            panel.transform.SetParent(popupCanvas.transform, false);

            panels.Push(panel);

            GameObject popup = Instantiate(popupPrefab, popupCanvas.transform, false);
            popup.GetComponent<Popup>().Initialize(this);

            popups.Push(popup);
        }

        public void ClosePopup()
        {
            var topmostPopup = popups.Pop();
            if (topmostPopup == null)
            {
                return;
            }

            var topmostPanel = panels.Pop();

            if (topmostPanel != null)
            {
                var canvasGroup = topmostPanel.GetComponent<CanvasGroup>();
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = false;
                topmostPanel.GetComponent<Panel>().DestroyPanel();
            }
        }

        public bool HasOpenPopups()
        {
            return popups.Count > 0;
        }
    }
}