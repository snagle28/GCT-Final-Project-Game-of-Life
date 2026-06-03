using UnityEngine;
using UnityEngine.SceneManagement;

// AD5: a continuously-looping background soundscape whose volume tracks the total
// number of alive cells (all colors summed). Always playing; only the volume moves.
//
//   total  = alive-cell count
//   ratio  = min(total / cap, 1)
//   target = minVol + (maxVol - minVol) * ratio
//   volume = lerp(volume, target, smoothing)   // per frame, prevents jumps
//
// The loop clip depends on the active soundscape:
//   Soundscape1 = Forest (env 1)  -> Resources/Soundscapes/Forest
//   Soundscape2 = Ocean  (env 2)  -> Resources/Soundscapes/Ocean
//
// Auto-loaded from Resources; the component is created on demand by GameOfLifeManager.
public class BackgroundLoopPlayer : MonoBehaviour
{
    private AudioSource source;
    private float currentVolume;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = LoadClipForActiveScene();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D — ambient background
        source.volume = 0f;

        if (source.clip == null)
            Debug.LogWarning("BackgroundLoopPlayer: no soundscape clip found for scene " +
                             SceneManager.GetActiveScene().name);
        else
            source.Play();
    }

    AudioClip LoadClipForActiveScene()
    {
        // Soundscape2 = Ocean (env 2); everything else falls back to Forest (env 1).
        string scene = SceneManager.GetActiveScene().name;
        string folder = scene == "Soundscape2" ? "Soundscapes/Ocean" : "Soundscapes/Forest";
        AudioClip[] all = Resources.LoadAll<AudioClip>(folder);
        return (all != null && all.Length > 0) ? all[0] : null;
    }

    // Called every frame by GameOfLifeManager with the current alive count and
    // tuning. cap = cell count at which max volume is reached.
    public void UpdateLevel(int aliveCount, int cap, float minVol, float maxVol, float smoothing)
    {
        if (source == null || source.clip == null) return;

        float ratio = cap > 0 ? Mathf.Min((float)aliveCount / cap, 1f) : 0f;
        float target = minVol + (maxVol - minVol) * ratio;
        currentVolume = Mathf.Lerp(currentVolume, target, Mathf.Clamp01(smoothing));
        source.volume = currentVolume;
    }
}
