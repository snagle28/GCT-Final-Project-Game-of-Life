using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeIn : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 0.8f;

    void Start()
    {
        StartCoroutine(FadeFromWhite());
    }

    private IEnumerator FadeFromWhite()
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color c = fadePanel.color;
        c.a = 1f;
        fadePanel.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            fadePanel.color = c;
            yield return null;
        }

        // ensure it's fully transparent when done
        c.a = 0f;
        fadePanel.color = c;
    }
}