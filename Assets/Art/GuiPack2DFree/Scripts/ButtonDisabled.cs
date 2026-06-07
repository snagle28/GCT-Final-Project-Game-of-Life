using UnityEngine;

namespace GUIPack2DFree { 
    public class ButtonDisabled : MonoBehaviour
    {
        public void ButtonClick()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.notAvaliable);
        }
    }
}
