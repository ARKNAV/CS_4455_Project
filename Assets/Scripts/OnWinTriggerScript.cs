using UnityEngine;

public class OnWinTriggerScript : MonoBehaviour
{
    public CanvasGroup winCanvas;
    void OnEnable()
    {
        //print("enabled");
        KeycardReaderController.WinTriggerEvent += UnhideCanvas;
    }

    // Update is called once per frame
    void OnDisable()
    {
        //print("disabled");
        KeycardReaderController.WinTriggerEvent -= UnhideCanvas;
    }

    void UnhideCanvas()
    {
        //print("We got to the trigger");
        winCanvas.interactable = true;
        winCanvas.blocksRaycasts = true;
        winCanvas.alpha = 1f;
        Time.timeScale = 0f;
    }
}
