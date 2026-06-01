using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionToTitle : MonoBehaviour
{
    private bool readyToTransition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(FadeToTitle());
        if (readyToTransition)
        {
            SceneManager.LoadScene("Intro Sound");
        }
    }

    private IEnumerator FadeToTitle()
    {
        yield return new WaitForSeconds(4f);
        readyToTransition = true;
    }
}
