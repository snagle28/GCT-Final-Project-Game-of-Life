using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Simple back button component that returns to the selection screen.
// Attach this to a GameObject with a Button component.
public class BackButton : MonoBehaviour
{
    private const string SelectScene = "Intro Sound";

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(GoBack);
        }
    }

    public void GoBack()
    {
        SceneManagement.skipIntro = true;
        var sm = Object.FindFirstObjectByType<SceneManagement>();
        if (sm != null) sm.MoveToSelect();
        else SceneManager.LoadScene(SelectScene);
    }
}

