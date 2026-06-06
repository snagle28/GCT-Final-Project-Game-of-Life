using UnityEngine;

namespace GUIPack2DFree
{
    public class PopupPause : Popup
    {
        void OnEnable()
        {
            Time.timeScale = 0;
        }

        public void Resume()
        {
            Time.timeScale = 1;
        }

        // add own code here for play on button press
        public void ButtonQuitPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);
            Resume();
            Close();
        }
    }
}
