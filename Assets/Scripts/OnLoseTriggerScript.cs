using System.Collections;
using UnityEngine;

public class OnLoseTriggerScript : MonoBehaviour
{
    public CanvasGroup loseCanvas;

    [Tooltip("Brief pause before showing the lose canvas. CatchPlayerRoutine already waits for the clip, so keep this small.")]
    public float loseScreenDelay = 0.3f;

    void OnEnable()
    {
        GameManager.LoseTriggerEvent += OnLose;
    }

    void OnDisable()
    {
        GameManager.LoseTriggerEvent -= OnLose;
    }

    void OnLose()
    {
        // Freeze time immediately so no second takedown can trigger
        Time.timeScale = 0f;
        StartCoroutine(ShowLoseScreenAfterDelay());
    }

    private IEnumerator ShowLoseScreenAfterDelay()
    {
        // WaitForSecondsRealtime is unaffected by timeScale=0
        yield return new WaitForSecondsRealtime(loseScreenDelay);

        loseCanvas.interactable = true;
        loseCanvas.blocksRaycasts = true;
        loseCanvas.alpha = 1f;
    }
}
