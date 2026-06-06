using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GUIPack2DFree
{
    public class SceneTransition : MonoBehaviour
    {
        static GameObject canvasObject;

        GameObject canvasOverlay;

        void Awake()
        {
            canvasObject = new GameObject("TransitionCanvas");

            var canvas = canvasObject.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasObject);
        }

        public static void LoadLevel(string level, float duration, Color fadeColor)
        {
            var fade = new GameObject("SceneTransition");

            fade.AddComponent<SceneTransition>();
            fade.GetComponent<SceneTransition>().StartFade(level, duration, fadeColor);
            fade.transform.SetParent(canvasObject.transform, false);
            fade.transform.SetAsLastSibling();
        }

        void StartFade(string level, float duration, Color fadeColor)
        {
            StartCoroutine(RunFade(level, duration, fadeColor));
        }

        IEnumerator RunFade(string level, float duration, Color fadeColor)
        {
            var overlayTexture = new Texture2D(1, 1);

            overlayTexture.SetPixel(0, 0, fadeColor);
            overlayTexture.Apply();

            canvasOverlay = new GameObject();

            var image = canvasOverlay.AddComponent<Image>();
            var rect = new Rect(0, 0, overlayTexture.width, overlayTexture.height);
            var sprite = Sprite.Create(overlayTexture, rect, new Vector2(0.5f, 0.5f), 1);

            image.material.mainTexture = overlayTexture;
            image.sprite = sprite;

            var newColor = image.color;

            image.color = newColor;
            image.canvasRenderer.SetAlpha(0.0f);

            canvasOverlay.transform.localScale = new Vector3(1, 1, 1);
            canvasOverlay.GetComponent<RectTransform>().sizeDelta = canvasObject.GetComponent<RectTransform>().sizeDelta;
            canvasOverlay.transform.SetParent(canvasObject.transform, false);
            canvasOverlay.transform.SetAsFirstSibling();

            var time = 0.0f;
            var halfDuration = duration / 2.0f;

            while (time < halfDuration)
            {
                time += Time.deltaTime;
                image.canvasRenderer.SetAlpha(Mathf.InverseLerp(0, 1, time / halfDuration));
                yield return new WaitForEndOfFrame();
            }

            image.canvasRenderer.SetAlpha(1.0f);

            yield return new WaitForEndOfFrame();

            SceneManager.LoadScene(level);

            time = 0.0f;

            while (time < halfDuration)
            {
                time += Time.deltaTime;
                image.canvasRenderer.SetAlpha(Mathf.InverseLerp(1, 0, time / halfDuration));
                yield return new WaitForEndOfFrame();
            }

            image.canvasRenderer.SetAlpha(0.0f);

            yield return new WaitForEndOfFrame();

            Destroy(canvasObject);
        }
    }
}
