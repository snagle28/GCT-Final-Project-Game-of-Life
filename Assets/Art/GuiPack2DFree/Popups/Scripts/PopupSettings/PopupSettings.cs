namespace GUIPack2DFree
{
    public class PopupSettings : Popup
    {
        public void ButtonRemoveAdsPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            // add code for remove ads
        }

        public void ButtonRestorePressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            // add code for restore purchases
        }

        public void ButtonNotificationsPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            // add code for notifications
        }

        public void ButtonTutorialPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

        }
    }
}
