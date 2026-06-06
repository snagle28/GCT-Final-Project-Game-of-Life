using UnityEngine;

namespace GUIPack2DFree
{
    public class PopupPrivacy : Popup
    {
        [Space(15)]
        [Header("Custom Settings")]
        public string privacyUrl = "http://unity3d.com/";

        public void ButtonReadMorePressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            Application.OpenURL(privacyUrl);

        }
    }
}
