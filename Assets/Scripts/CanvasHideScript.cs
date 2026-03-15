using UnityEngine;

public class CanvasHideScript : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f)
        {
            canvasGroup.alpha = 0f;
        } else
        {
            canvasGroup.alpha = 1f;
        }
    }
}
