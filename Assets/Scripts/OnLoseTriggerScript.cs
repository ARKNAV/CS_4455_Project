using UnityEngine;

public class OnLoseTriggerScript : MonoBehaviour
{
    public CanvasGroup loseCanvas;
    void OnEnable()
    {
        //print("enabled");
        GameManager.LoseTriggerEvent += UnhideCanvas;
    }

    // Update is called once per frame
    void OnDisable()
    {
        //print("disabled");
        GameManager.LoseTriggerEvent -= UnhideCanvas;
    }

    void UnhideCanvas()
    {
        //print("We got to the trigger");
        loseCanvas.interactable = true;
        loseCanvas.blocksRaycasts = true;
        loseCanvas.alpha = 1f;
        Time.timeScale = 0f;
    }
}
