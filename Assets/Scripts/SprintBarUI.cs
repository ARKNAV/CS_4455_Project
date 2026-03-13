using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the sprint bar fill inside the unified StatusPanel.
/// Scales the fill RectTransform and optionally updates a percentage label.
/// </summary>
public class SprintBarUI : MonoBehaviour
{
    public CharacterInputController inputController;
    public RectTransform fillBar;
    public Text valueLabel;

    [Header("Fade Settings")]
    public bool autoFade = true;
    public float fadeSpeed = 3f;

    private CanvasGroup canvasGroup;

    void Start()
    {
        if (inputController == null)
            inputController = FindFirstObjectByType<CharacterInputController>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (autoFade && canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Update()
    {
        if (inputController == null) return;

        float ratio = Mathf.Clamp01(inputController.CurrentStamina / inputController.MaxStamina);

        if (fillBar != null)
        {
            Vector3 scale = fillBar.localScale;
            scale.x = ratio;
            fillBar.localScale = scale;
        }

        if (valueLabel != null)
        {
            valueLabel.text = $"{Mathf.RoundToInt(ratio * 100)}%";
        }

        if (autoFade && canvasGroup != null)
        {
            bool shouldShow = inputController.IsSprinting || ratio < 0.999f;
            float targetAlpha = shouldShow ? 1f : 0f;
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }
}
