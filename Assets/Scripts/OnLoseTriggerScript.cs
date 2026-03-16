using System.Collections;
using UnityEngine;

public class OnLoseTriggerScript : MonoBehaviour
{
    public CanvasGroup loseCanvas;

    [Tooltip("Delay in seconds before showing the lose screen, to allow animations to finish.")]
    public float loseScreenDelay = 4.67f;

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
        StartCoroutine(ShowLoseScreenAfterDelay());
    }

    private IEnumerator ShowLoseScreenAfterDelay()
    {
        yield return new WaitForSeconds(loseScreenDelay);

        loseCanvas.interactable = true;
        loseCanvas.blocksRaycasts = true;
        loseCanvas.alpha = 1f;
        Time.timeScale = 0f;
    }
}
