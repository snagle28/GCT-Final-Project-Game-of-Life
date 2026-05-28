using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public Transform myWindow;

    [Tooltip("Seconds to wait after clicking before loading the next scene (lets the click sound / window animation play).")]
    public float loadDelay = 0.6f;

    private bool lerpingUp;

    // Update is called once per frame
    void Update()
    {
        if (lerpingUp && myWindow != null)
        {
            Vector3 target = new Vector3(myWindow.position.x, 60f, myWindow.position.z);
            myWindow.position = Vector3.Lerp(myWindow.position, target, Time.deltaTime * 0.2f);
        }
    }

    public void windowUp()
    {
        lerpingUp = true;
    }

    public void nextScene()
    {
        lerpingUp = true;                          // animate the window rising
        Invoke(nameof(LoadNextScene), loadDelay);  // then load, regardless of window position
        print("next scene");
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void playClickSound()
    {
        print("play click sound");
    }
    
}
