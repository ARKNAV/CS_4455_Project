using UnityEngine;
using UnityEngine.InputSystem;

//used this from m3
[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuToggle : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public bool isPauseMenu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //canvasGroup=GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        //canvasGroup=GetComponent<CanvasGroup>();
        //if(canvasGroup == null)
       // {
         //   print("Canvas not found");
       // }
    }
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && isPauseMenu == true) {
            if (canvasGroup.interactable) {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0f;
                Time.timeScale = 1f;
            } else if (Time.timeScale != 0f) {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
                Time.timeScale = 0f;
            }
        }
    }

    public void MenuToggle()
    {
        if (canvasGroup.interactable) {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0f;
                Time.timeScale = 1f;
            } else {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
                Time.timeScale = 0f;
            }
    }
}
