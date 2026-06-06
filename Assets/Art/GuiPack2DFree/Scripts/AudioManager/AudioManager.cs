using UnityEngine;

namespace GUIPack2DFree
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance = null;

        // these strings are used for storing if music or efx effects are muted
        public const string MUTE_MUSIC = "MUTE_MUSIC";
        public const string MUTE_EFX = "MUTE_EFX";

        [Header("Audio Sources")]
        public AudioSource efxSource;
        public AudioSource musicSource;

        [Header("Background Music")]
        public AudioClip menuMusic;

        [Header("Sound Effects")]
        public AudioClip buttonClick;
        public AudioClip notAvaliable;


        bool muteMusic;
        bool muteEfx;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        // initialization
        void Start()
        {
            if (!PlayerPrefs.HasKey(MUTE_MUSIC))
                PlayerPrefs.SetInt(MUTE_MUSIC, 0);

            if (!PlayerPrefs.HasKey(MUTE_EFX))
                PlayerPrefs.SetInt(MUTE_EFX, 0);

            muteMusic = PlayerPrefs.GetInt(MUTE_MUSIC) == 1 ? true : false;
            muteEfx = PlayerPrefs.GetInt(MUTE_EFX) == 1 ? true : false;

            // play menu music on initialization
            PlayMusic(menuMusic);
        }

        // play clip
        public void PlayMusic(AudioClip clip)
        {
            if (muteMusic)
                return;

            if (clip == null)
                return;

            musicSource.clip = clip;
            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        // stop background music
        void StopMusic()
        {
            musicSource.Stop();
        }

        // play efx
        public void PlayEffects(AudioClip clip)
        {
            if (muteEfx)
                return;

            if (clip == null)
                return;

            efxSource.PlayOneShot(clip);
        }

        // mute/unmute background music
        public void MuteMusic()
        {
            if (muteMusic)
            {
                muteMusic = false;
                PlayMusic(menuMusic);
                PlayerPrefs.SetInt(MUTE_MUSIC, 0);
            }
            else
            {
                muteMusic = true;
                StopMusic();
                PlayerPrefs.SetInt(MUTE_MUSIC, 1);
            }
        }

        // mute/unmute efx
        public void MuteEfx()
        {
            if (muteEfx)
                PlayerPrefs.SetInt(MUTE_EFX, 0);
            else
                PlayerPrefs.SetInt(MUTE_EFX, 1);

            muteEfx = !muteEfx;
        }

        // check if music is muted
        public bool IsMusicMute()
        {
            return muteMusic;
        }

        // check if efx are muted
        public bool IsEfxMute()
        {
            return muteEfx;
        }
    }
}