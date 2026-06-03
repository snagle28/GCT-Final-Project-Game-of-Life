using UnityEngine;
using UnityEngine.SceneManagement;

// AD3: single click effect sound played when the user paints a cell.
//
// Position- and pitch-independent: each trigger plays one clip randomly sampled
// from a population of footstep sounds. The population depends on the active
// soundscape environment:
//   Soundscape1 = Forest (env 1)  -> Resources/Footsteps/Forest
//   Soundscape2 = Ocean  (env 2)  -> Resources/Footsteps/Ocean
//
// Clips are auto-loaded from Resources so no Inspector wiring is required; the
// component is created on demand by GameOfLifeManager.
public class FootstepPlayer : MonoBehaviour
{
    private AudioClip[] clips;
    private AudioSource source;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D — AD3 is position-independent

        clips = LoadClipsForActiveScene();
        if (clips == null || clips.Length == 0)
            Debug.LogWarning("FootstepPlayer: no footstep clips found for scene " +
                             SceneManager.GetActiveScene().name);
    }

    AudioClip[] LoadClipsForActiveScene()
    {
        // Soundscape2 = Ocean (env 2); everything else falls back to Forest (env 1).
        string scene = SceneManager.GetActiveScene().name;
        string folder = scene == "Soundscape2" ? "Footsteps/Ocean" : "Footsteps/Forest";
        return Resources.LoadAll<AudioClip>(folder);
    }

    // Plays one randomly-sampled footstep from the population at the given volume
    // (read fresh each call so the Inspector slider responds live, even in Play).
    // No pitch or pan mapping — purely a single click effect.
    public void Play(float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
