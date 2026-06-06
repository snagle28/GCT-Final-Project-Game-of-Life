using UnityEngine;

namespace GUIPack2DFree
{
    public class PopupOpener : MonoBehaviour
    {
        public GameObject Popup;

        public void OpenPopup()
        {
            var screen = FindObjectOfType<PopupSystem>();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);
            }
            
            if (screen != null)
            {
                screen.OpenPopup(Popup);
            }
        }
    }
}