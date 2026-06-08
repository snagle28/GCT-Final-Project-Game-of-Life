using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleUIToggle : MonoBehaviour
{
    public GameObject targetObject;

    public void ShowTarget()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }

    public void HideTarget()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(false);
        }
    }

    public void ToggleTarget()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(!targetObject.activeSelf);
        }
    }
}
