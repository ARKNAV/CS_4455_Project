using UnityEngine;

public class OptionButtonToggle : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public CanvasGroup optionsGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Awake()
    {
    }

    public void OptionToggleOnClick()
    {
        if (canvasGroup.interactable == true)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;

            optionsGroup.interactable = true;
            optionsGroup.blocksRaycasts = true;
            optionsGroup.alpha = 1f;
        } else {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            optionsGroup.interactable = false;
            optionsGroup.blocksRaycasts = false;
            optionsGroup.alpha = 0f;
        }

        
    }
}
