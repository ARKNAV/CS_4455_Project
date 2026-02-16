using UnityEngine;

public class FootstepSound : MonoBehaviour
{
    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();

        if(source == null)
            Debug.LogError("FootstepSound needs an AudioSource!");
    }

    // THIS FUNCTION will be called by the animation
    public void EmitFootstep()
    {
        if(source != null)
        {
            source.PlayOneShot(source.clip);
        }
    }
}
