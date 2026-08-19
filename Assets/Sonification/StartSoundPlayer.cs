using UnityEngine;
using UnityEngine.SceneManagement;

// AD4: start/stop effect sound triggered on the play/pause toggle (space bar).
//
// Position- and pitch-independent: a single clip per environment, played on every
// toggle (both resume and pause). The clip depends on the active soundscape:
//   Soundscape1 = Forest (env 1)  -> Resources/StartSounds/Forest
//   Soundscape2 = Ocean  (env 2)  -> Resources/StartSounds/Ocean
//
// The clip is auto-loaded from Resources so no Inspector wiring is required; the
// component is created on demand by GameOfLifeManager.
public class StartSoundPlayer : MonoBehaviour
{
    private AudioClip clip;
    private AudioSource source;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f; // 2D — AD4 is position-independent

        clip = LoadClipForActiveScene();
        if (clip == null)
            Debug.LogWarning("StartSoundPlayer: no start clip found for scene " +
                             SceneManager.GetActiveScene().name);
    }

    AudioClip LoadClipForActiveScene()
    {
        // Soundscape2 = Ocean (env 2); everything else falls back to Forest (env 1).
        string scene = SceneManager.GetActiveScene().name;
        string folder = scene.Contains("2") ? "StartSounds/Ocean" : "StartSounds/Forest";
        AudioClip[] all = Resources.LoadAll<AudioClip>(folder);
        return (all != null && all.Length > 0) ? all[0] : null;
    }

    // Plays the start/stop effect at the given volume (read fresh each call so the
    // Inspector slider responds live, even in Play). No pitch or pan mapping.
    public void Play(float volume)
    {
        if (clip != null) source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }
}
