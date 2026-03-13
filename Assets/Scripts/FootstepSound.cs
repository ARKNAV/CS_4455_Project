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
        if(source != null)
        {
            if(source.clip != null)
            {
                source.PlayOneShot(source.clip);
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

        var emitter = GetComponent<PlayerNoiseEmitter>();
        if (emitter != null)
        {
            emitter.EmitFootstepNoise();
        }
    }
}
