using UnityEngine;

public class FadeInMenu : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Canvas canvasToDisable;
    [SerializeField] private float fadeDuration = 2f;

    private float elapsedTime;
    private bool finishedFading;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;

        elapsedTime = 0f;
        finishedFading = false;
    }

    private void Update()
    {
        if (finishedFading)
        {
            return;
        }

        if (elapsedTime >= fadeDuration)
        {
            SetAlpha(1f);

            if (canvasToDisable != null)
            {
                canvasToDisable.gameObject.SetActive(false);
            }

            finishedFading = true;
            return;
        }

        elapsedTime += Time.deltaTime;
        SetAlpha(elapsedTime / fadeDuration);
    }

    private void SetAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }
}
