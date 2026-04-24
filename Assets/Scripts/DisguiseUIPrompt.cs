using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component that shows an interaction prompt when the player is near a disguise box.
/// Displays "Press [F] to put on Engineer Uniform" style text with clearance info.
/// Supports rich text for sub-labels.
/// Tracks which box requested the prompt so overlapping triggers don't cancel each other.
/// </summary>
public class DisguiseUIPrompt : MonoBehaviour
{
    [Header("UI References")]
    public Text promptText;
    public Image backgroundPanel;

    [Header("Settings")]
    public float fadeSpeed = 8f;

    [Header("Pulse (while showing)")]
    [Tooltip("Alpha oscillates while the prompt is visible to draw attention")]
    public bool pulseWhenShowing = true;
    [Tooltip("Pulse cycles per second")]
    public float pulseFrequency = 2f;
    [Tooltip("Minimum alpha during pulse")]
    [Range(0f, 1f)]
    public float pulseMinAlpha = 0.65f;

    private CanvasGroup canvasGroup;
    private bool isShowing = false;

    /// <summary>The object that currently owns the prompt display.</summary>
    private Object currentRequester;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // Ensure rich text is enabled on the prompt text
        if (promptText != null)
            promptText.supportRichText = true;
    }

    void Update()
    {
        if (isShowing)
        {
            if (pulseWhenShowing && canvasGroup.alpha > 0.1f)
            {
                // Pulse between pulseMinAlpha and 1 once fully faded in
                float pulse = (Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) * 0.5f + 0.5f)
                              * (1f - pulseMinAlpha) + pulseMinAlpha;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, pulse, Time.deltaTime * pulseFrequency * 2f);
            }
            else
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            }
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0f, Time.deltaTime * fadeSpeed);
        }
    }

    /// <summary>Show the prompt. The requester is tracked so only it can hide it.</summary>
    public void ShowPrompt(string text, Object requester = null)
    {
        isShowing = true;
        currentRequester = requester;
        if (promptText != null)
            promptText.text = text;
    }

    /// <summary>
    /// Legacy overload without requester — always shows.
    /// </summary>
    public void ShowPrompt(string text)
    {
        ShowPrompt(text, null);
    }

    /// <summary>
    /// Hide the prompt. If a requester is provided, only hides if that requester
    /// is the one currently showing the prompt (prevents overlapping trigger issues).
    /// </summary>
    public void HidePrompt(Object requester = null)
    {
        // If no requester tracking, always hide
        if (requester == null || currentRequester == null || currentRequester == requester)
        {
            isShowing = false;
            currentRequester = null;
        }
    }

    /// <summary>Legacy overload — always hides.</summary>
    public void HidePrompt()
    {
        HidePrompt(null);
    }
}
