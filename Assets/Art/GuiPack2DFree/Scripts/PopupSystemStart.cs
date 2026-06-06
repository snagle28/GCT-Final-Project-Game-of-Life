namespace GUIPack2DFree
{
    public class PopupSystemStart : PopupSystem
    {
        PopupOpener popupOpener;

        void Awake()
        {
            popupOpener = GetComponent<PopupOpener>();
        }

        void Start()
        {
            Invoke("OpenIntro", 1);
        }

        void OpenIntro()
        {
            popupOpener.OpenPopup();
        }
    }
}
