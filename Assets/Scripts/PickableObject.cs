using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickableObject : MonoBehaviour
{
    private Rigidbody rb;
    public bool winTrigger = false;
    public CanvasGroup winCanvas;
    public bool IsHeld { get; private set; }

    public Rigidbody Rigidbody
    {
        get { return rb; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PickableObject requires a Rigidbody.");
        }
    }

    public void OnPickedUp()
    {
        if (winTrigger && winCanvas != null)
        {
            winCanvas.interactable = true;
            winCanvas.blocksRaycasts = true;
            winCanvas.alpha = 1f;
            Time.timeScale = 0f;
        }
        IsHeld = true;
    }

    public void OnDropped()
    {
        IsHeld = false;
    }
}
