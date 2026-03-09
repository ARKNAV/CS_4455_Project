using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickableObject : MonoBehaviour
{
    private Rigidbody rb;

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
        IsHeld = true;
    }

    public void OnDropped()
    {
        IsHeld = false;
    }
}
