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
        var sm = Object.FindAnyObjectByType<SceneManagement>();
        
        if (sm != null) sm.MoveToSelect(); // 매니저가 있으면 페이드아웃 효과와 함께 정상 작동!
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name.Contains("_R") ? "Intro Sound_R" : "Intro Sound"); // 비상시 목적지 분기
    }
}

