using UnityEngine;

public class ReturnFromInstructions : MonoBehaviour
{

    public GameObject Instructions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if user presses space button
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instructions.SetActive(false);
        }
    }
}
