using UnityEngine;

public class InitializeGame : MonoBehaviour
{
    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator.SetTrigger("Start");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
