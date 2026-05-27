using UnityEngine;

public class InitializeGame : MonoBehaviour
{
    public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Play the right-to-left swoop from its first frame immediately on load.
        // (SetTrigger relied on an empty default state with exit time, which left
        // the scene parked in its final position for ~0.75s before sliding.)
        animator.Play("GameSwoop", 0, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
