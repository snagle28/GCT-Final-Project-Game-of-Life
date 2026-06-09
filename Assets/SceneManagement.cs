using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneManagement : MonoBehaviour
{
    public Transform myWindow;
    public PlayableDirector director;
    public bool buttonClicked = false;
    public Image fadePanel;                  // assign the white UI Image here

    [Tooltip("Seconds to wait after clicking before loading the next scene.")]
    public float loadDelay = 0.6f;

    [Tooltip("How long the fade takes before/after switching scenes.")]
    public float fadeDuration = 0.4f;

    private bool lerpingUp;
    private bool hasJumpedToEnd = false;

    // When the player uses "Back" from a Soundscape, skip the "click to begin"
    // stage and land directly on the Environment-select screen. Setting buttonClicked
    // replicates pressing "click to begin": the Update branch below then jumps the
    // director to the selection state (time = 6) and plays it.
    public static bool skipIntro = false;

    void Start()
    {
        if (fadePanel != null) 
        {
            fadePanel.sprite = null;
            fadePanel.raycastTarget = true; // Block input while fading in
            StartCoroutine(FadeFromWhite());
        }
        
        // Intro Sound has several SceneManagement components; only the one that owns
        // the timeline director drives the screen state. Gate on director != null so
        // a director-less instance doesn't consume the flag before the real one runs.
        if (skipIntro && SceneManager.GetActiveScene().buildIndex == 1 && director != null)
        {
            buttonClicked = true;   // == "click to begin" already pressed
            skipIntro = false;      // consume only when the right instance handles it
        }
    }

    private IEnumerator FadeFromWhite()
    {
        if (fadePanel == null) yield break;

        fadePanel.sprite = null;
        float elapsed = 0f;
        Color c = Color.white;
        c.a = 1f;
        fadePanel.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }
        
        c.a = 0f;
        fadePanel.color = c;
        fadePanel.raycastTarget = false;
    }

    void Update()
    {
        if (lerpingUp && myWindow != null)
        {
            Vector3 target = new Vector3(myWindow.position.x, 60f, myWindow.position.z);
            myWindow.position = Vector3.Lerp(myWindow.position, target, Time.deltaTime * 0.2f);
        }

        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            if (!buttonClicked)
            {
                if (director.time >= 5.416667f)
                    director.time = 2.916667;
            }
            else if (!hasJumpedToEnd)
            {
                hasJumpedToEnd = true;
                director.time = 6f;
                director.Play();
            }
        }
    }

    // ── Faded scene loads ──────────────────────────────────────────
    private static string lastSoundscape = "";

    public void MoveToSoundscape1()
    {
        StartCoroutine(FadeAndLoad("Soundscape1"));
    }

    public void MoveToSoundscape2()
    {
        StartCoroutine(FadeAndLoad("Soundscape2"));
    }

    public void MoveToInstructions()
    {
        lastSoundscape = SceneManager.GetActiveScene().name;
        StartCoroutine(FadeAndLoad("Instructions"));
    }

    public void MoveToPreviousScene()
    {
        string target = string.IsNullOrEmpty(lastSoundscape) ? "Intro Sound" : lastSoundscape;
        StartCoroutine(FadeAndLoad(target));
    }

    // Back button: return to the Environment-select scene ("Intro Sound", build
    // index 1) so the player can pick a different soundscape. Reuses the same
    // fade-to-black as the forward loads.
    public void MoveToSelect()
    {
        skipIntro = true;   // land on the selection loop, not the replayed intro
        StartCoroutine(FadeAndLoad("Intro Sound"));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadePanel != null) fadePanel.raycastTarget = true;
        yield return StartCoroutine(FadeToWhite());
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator FadeToWhite()
    {
        if (fadePanel == null) yield break;

        fadePanel.sprite = null;
        float elapsed = 0f;
        Color c = Color.white;
        c.a = 0f;
        fadePanel.color = c;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }
    }

    // ── Everything else unchanged ──────────────────────────────────
    public void windowUp()        { lerpingUp = true; }
    public void SetButtonClickedTrue() { buttonClicked = true; }

    public void nextScene()
    {
        lerpingUp = true;
        Invoke(nameof(LoadNextScene), loadDelay);
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void enterLibrary()
    {
        if (director != null) { director.Play(); }
    }

    public void playClickSound() { print("play click sound"); }
}