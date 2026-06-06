using UnityEngine;

namespace GUIPack2DFree
{
    public class Panel : MonoBehaviour
    {
        public void DestroyPanel()
        {
            GetComponent<Animator>().Play("PanelHide");
            Invoke("DestroyObject", .2f);
        }

        void DestroyObject()
        {
            Destroy(gameObject);
        }
    }
}
