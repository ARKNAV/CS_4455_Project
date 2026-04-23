using System.Collections;
using UnityEngine;

public class OnWinTriggerScript : MonoBehaviour
{
    public CanvasGroup winCanvas;
    void OnEnable()
    {
        //print("enabled");
        BlueprintConsoleController.WinTriggerEvent += UnhideCanvas;
    }

    void OnDisable()
    {
        //print("disabled");
        BlueprintConsoleController.WinTriggerEvent -= UnhideCanvas;
    }

//This is a little silly looking but i had to let the door finish its animation so im using WaitForSeconds()
    void UnhideCanvas()
    {
        StartCoroutine(WaitUnhideCanvas());
    }

    IEnumerator WaitUnhideCanvas()
    {
        yield return new WaitForSeconds(2);

        winCanvas.interactable = true;
        winCanvas.blocksRaycasts = true;
        winCanvas.alpha = 1f;
        Time.timeScale = 0f;
    }
}
