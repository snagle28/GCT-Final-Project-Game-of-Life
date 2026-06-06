using UnityEngine;

namespace GUIPack2DFree
{
    public class ChangeScene : MonoBehaviour
    {
        [SerializeField]
        private string sceneName;
        [SerializeField]
        private float transitionDuration = 1.0f;

        public void GoToScene()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);

            SceneTransition.LoadLevel(sceneName, transitionDuration, Color.black);
        }
    }
}