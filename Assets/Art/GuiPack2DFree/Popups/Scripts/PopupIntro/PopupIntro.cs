using UnityEngine;

namespace GUIPack2DFree
{
    public class PopupIntro : Popup
    {
        public void ButtonClick()
        {
            // go to next scene
            SceneTransition.LoadLevel("2. Menu", 1f, Color.black);

            Close();
        }
    }
}
