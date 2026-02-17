using UnityEngine;

public class FootstepSound : MonoBehaviour
{
    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();

        if(source == null)
            Debug.LogError("FootstepSound needs an AudioSource!");
        else
            Debug.Log("FootstepSound initialized. AudioSource clip: " + (source.clip != null ? source.clip.name : "NULL"));
    }

    // THIS FUNCTION will be called by the animation
    public void EmitFootstep()
    {
        Debug.Log("EmitFootstep() called!");
        if(source != null)
        {
            if(source.clip != null)
            {
                source.PlayOneShot(source.clip);
                Debug.Log("Playing footstep sound: " + source.clip.name);
            }
            else
            {
                Debug.LogWarning("AudioSource has no clip assigned!");
            }
        }
        else
        {
            Debug.LogError("AudioSource is null!");
        }
    }
}
