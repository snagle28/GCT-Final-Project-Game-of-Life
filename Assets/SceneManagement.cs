using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagement : MonoBehaviour
{
    public Transform myWindow;
    private Vector3 position;
    private bool lerpingUp;
    private bool loadNextScene;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (lerpingUp)
        {
            Vector3 target = new Vector3(myWindow.transform.position.x, 60f, transform.position.z);
            myWindow.transform.position = Vector3.Lerp(myWindow.transform.position, target, Time.deltaTime * 0.2f);
        }

        if (loadNextScene)
        {
            if (myWindow.transform.position.y >= 10f)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
        
    }

    public void windowUp()
    {
        lerpingUp = true;
    }

    public void nextScene()
    {
       loadNextScene = true;
       print("next scene");
    }
    
}
