using UnityEngine;
using UnityEngine.UI;

namespace GUIPack2DFree
{

    public class MusicButtons : MonoBehaviour
    {
        [Header("Text colors")]
        public Color enabledColor;
        public Color disabledColor;

        [Header("Audio")]
        public Sprite buttonOn;
        public Sprite buttonOff;
        public Image buttonMusic, buttonEffects;
        public Outline musicIconOutline, effectsIconOutline;
        public Shadow musicIconShadow, effectsIconShadow;

        void OnEnable()
        {
            EffectsButton();
            MusicButton();
        }

        void EffectsButton()
        {
            if (AudioManager.Instance.IsEfxMute())
            {
                buttonEffects.sprite = buttonOff;
                effectsIconOutline.effectColor = disabledColor;
                effectsIconShadow.effectColor = disabledColor;
            }
            else
            {
                buttonEffects.sprite = buttonOn;
                effectsIconOutline.effectColor = enabledColor;
                effectsIconShadow.effectColor = enabledColor;
            }
        }

        void MusicButton()
        {
            if (AudioManager.Instance.IsMusicMute())
            {
                buttonMusic.sprite = buttonOff;
                musicIconOutline.effectColor = disabledColor;
                musicIconShadow.effectColor = disabledColor;
            }
            else
            {
                buttonMusic.sprite = buttonOn;
                musicIconOutline.effectColor = enabledColor;
                musicIconShadow.effectColor = enabledColor;
            }
        }

        public void ButtonMusicPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);
            AudioManager.Instance.MuteMusic();
            MusicButton();
        }

        public void ButtonEffectsPressed()
        {
            AudioManager.Instance.PlayEffects(AudioManager.Instance.buttonClick);
            AudioManager.Instance.MuteEfx();
            EffectsButton();
        }

    }
}
