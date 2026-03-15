using UnityEngine;
using UnityEngine.AI;

public enum FootstepMode
{
    Player = 0,
    AIGuard = 1
}

public class FootstepSound : MonoBehaviour
{
    private AudioSource source;
    private CharacterInputController characterInput;
    private BasicControlScript movementController;
    private NavMeshAgent navMeshAgent;
    private float nextSprintFootstepTime;
    private float lastEmitTime;
    private bool sprintStepPairFirst = true;

    [Header("Mode")]
    [SerializeField] private FootstepMode mode = FootstepMode.Player;

    [Header("Footstep Clips")]
    [SerializeField] private AudioClip[] walkClips;
    [SerializeField] private AudioClip[] runClips;

    [Header("Mix")]
    [SerializeField] private float walkVolume = 1f;
    [SerializeField] private float runVolume = 1.35f;

    [Header("Sprint Fallback")]
    [SerializeField] private float sprintFootstepInterval = 0.32f;
    [SerializeField] private float sprintStepPairSpacing = 0.12f;
    [SerializeField] private float sprintPairRecoveryDelay = 0.16f;
    [SerializeField] private float sprintMinSpeed = 1.25f;
    [SerializeField] private float minimumEmitSpacing = 0.08f;

    [Header("AI Guard")]
    [SerializeField] private AudioClip guardFootstepClip;
    [SerializeField] private float aiBaseVolume = 1f;
    [SerializeField] private float aiMinSpeed = 0.05f;
    [SerializeField] private float aiNearDistance = 2f;
    [SerializeField] private float aiFarDistance = 18f;
    [SerializeField] private float aiMinDistanceVolume = 0f;
    [SerializeField] private float aiFootstepInterval = 0.42f;
    [SerializeField] private Transform listenerTarget;
    [SerializeField] private Transform aiDistanceTarget;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        characterInput = GetComponent<CharacterInputController>();
        movementController = GetComponent<BasicControlScript>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        if(source == null)
            Debug.LogError("FootstepSound needs an AudioSource!");
        else
            Debug.Log("FootstepSound initialized. AudioSource clip: " + (source.clip != null ? source.clip.name : "NULL"));

        if (mode == FootstepMode.AIGuard && source != null)
        {
            ConfigureGuardAudioSource(source);
        }
    }

    void Update()
    {
        if (mode == FootstepMode.AIGuard)
        {
            UpdateGuardFootsteps();
            return;
        }

        if (!ShouldUseSprintFallback())
        {
            nextSprintFootstepTime = 0f;
            sprintStepPairFirst = true;
            return;
        }

        if (Time.time >= nextSprintFootstepTime)
        {
            EmitFootstep();
            nextSprintFootstepTime = Time.time + GetNextSprintDelay();
        }
    }

    // THIS FUNCTION will be called by the animation
    public void EmitFootstep()
    {
        if (Time.time < lastEmitTime + minimumEmitSpacing)
        {
            return;
        }

        if(source != null)
        {
            AudioClip clipToPlay = GetFootstepClip();
            float volume = GetPlaybackVolume();

            if(clipToPlay != null)
            {
                source.PlayOneShot(clipToPlay, volume);
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

        var emitter = GetComponentInParent<PlayerNoiseEmitter>();
        if (emitter != null)
        {
            emitter.EmitFootstepNoise();
        }

        lastEmitTime = Time.time;
    }

    private AudioClip GetFootstepClip()
    {
        if (mode == FootstepMode.AIGuard)
        {
            return guardFootstepClip;
        }

        AudioClip[] clipSet = IsRunning() ? runClips : walkClips;

        if (clipSet != null && clipSet.Length > 0)
        {
            return clipSet[Random.Range(0, clipSet.Length)];
        }

        return source != null ? source.clip : null;
    }

    private bool IsRunning()
    {
        if (mode == FootstepMode.AIGuard)
        {
            return false;
        }

        return characterInput != null && characterInput.IsSprinting;
    }

    private bool ShouldUseSprintFallback()
    {
        if (!IsRunning() || movementController == null)
        {
            return false;
        }

        if (!movementController.HasGroundSupport)
        {
            return false;
        }

        return movementController.CurrentPlanarVelocity.magnitude >= sprintMinSpeed;
    }

    private float GetSprintInterval()
    {
        if (movementController == null)
        {
            return sprintFootstepInterval;
        }

        float speed = movementController.CurrentPlanarVelocity.magnitude;
        float ratio = Mathf.Max(1f, speed / Mathf.Max(0.01f, sprintMinSpeed));
        return Mathf.Max(minimumEmitSpacing, sprintFootstepInterval / ratio);
    }

    private float GetNextSprintDelay()
    {
        float cycleInterval = GetSprintInterval();
        float pairSpacing = Mathf.Min(sprintStepPairSpacing, cycleInterval * 0.5f);
        float recoveryDelay = Mathf.Max(minimumEmitSpacing, cycleInterval - pairSpacing + sprintPairRecoveryDelay);

        float nextDelay = sprintStepPairFirst ? pairSpacing : recoveryDelay;
        sprintStepPairFirst = !sprintStepPairFirst;
        return Mathf.Max(minimumEmitSpacing, nextDelay);
    }

    private void UpdateGuardFootsteps()
    {
        if (navMeshAgent == null || source == null)
        {
            return;
        }

        float speed = navMeshAgent.velocity.magnitude;
        if (speed < aiMinSpeed)
        {
            nextSprintFootstepTime = 0f;
            return;
        }

        if (nextSprintFootstepTime <= 0f)
        {
            nextSprintFootstepTime = Time.time;
        }

        if (Time.time >= nextSprintFootstepTime)
        {
            EmitFootstep();
            nextSprintFootstepTime = Time.time + Mathf.Max(minimumEmitSpacing, aiFootstepInterval);
        }
    }

    private float GetPlaybackVolume()
    {
        if (mode == FootstepMode.AIGuard)
        {
            return aiBaseVolume * GetGuardDistanceVolume();
        }

        return IsRunning() ? runVolume : walkVolume;
    }

    private float GetGuardDistanceVolume()
    {
        Transform target = listenerTarget;
        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        if (target == null)
        {
            return 1f;
        }

        Transform distanceSource = aiDistanceTarget != null ? aiDistanceTarget : transform;
        float distance = Vector3.Distance(distanceSource.position, target.position);
        float t = Mathf.InverseLerp(aiNearDistance, aiFarDistance, distance);
        return Mathf.Lerp(1f, aiMinDistanceVolume, t);
    }

    private void ConfigureGuardAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }
}
