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
        float targetAlpha = isShowing ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
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
