using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PickableObject : MonoBehaviour
{
    private Rigidbody rb;
    private AudioSource audioSource;
    private bool wasThrown;
    private bool impactPlayedSinceThrow;

    [Header("Audio")]
    [SerializeField] private AudioClip thrownImpactClip;
    [SerializeField] private float minImpactVelocity = 1.5f;

    public bool IsHeld { get; private set; }

    public Rigidbody Rigidbody
    {
        get { return rb; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        if (rb == null)
        {
            Debug.LogError("PickableObject requires a Rigidbody.");
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    public void OnPickedUp()
    {
        IsHeld = true;
        wasThrown = false;
        impactPlayedSinceThrow = false;
    }

    public void OnDropped()
    {
        IsHeld = false;
    }

    public void MarkThrown()
    {
        wasThrown = true;
        impactPlayedSinceThrow = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!wasThrown || impactPlayedSinceThrow || thrownImpactClip == null)
        {
            return;
        }

        if (collision.collider == null || collision.collider.isTrigger)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactVelocity)
        {
            return;
        }

        audioSource.PlayOneShot(thrownImpactClip);
        impactPlayedSinceThrow = true;
        wasThrown = false;
    }
}
