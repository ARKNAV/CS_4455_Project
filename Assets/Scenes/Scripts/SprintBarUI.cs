using UnityEngine;
using UnityEngine.UI;

public class SprintBarUI : MonoBehaviour
{
    public CharacterInputController inputController;
    public RectTransform fillBar;

    [Header("Fade Settings")]
    public bool autoFade = true;
    public float fadeSpeed = 3f;

    private Image fillImage;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (inputController == null)
            inputController = FindFirstObjectByType<CharacterInputController>();

        if (fillBar != null)
            fillImage = fillBar.GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        if (inputController == null) return;

        float ratio = inputController.CurrentStamina / inputController.MaxStamina;
        ratio = Mathf.Clamp01(ratio);

        if (fillBar != null)
        {
            Vector3 scale = fillBar.localScale;
            scale.x = ratio;
            fillBar.localScale = scale;
        }

        if (fillImage != null)
        {
            if (ratio < 0.01f)
                fillImage.color = new Color(0.9f, 0.1f, 0.1f, 1f);
            else if (ratio < 0.25f)
                fillImage.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f, 1f), new Color(1f, 0.6f, 0f, 1f), ratio / 0.25f);
            else
                fillImage.color = new Color(0.2f, 0.7f, 1f, 1f);
        }

        if (autoFade && canvasGroup != null)
        {
            bool shouldShow = inputController.IsSprinting || ratio < 0.999f;
            float targetAlpha = shouldShow ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }
}
