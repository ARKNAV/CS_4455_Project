using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterInputController))]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [SerializeField] float baseNoiseRadius = 8f;
    [SerializeField] float crouchNoiseMultiplier = 0.35f;
    [SerializeField] float runNoiseMultiplier = 1.5f;
    private CharacterInputController _cinput;
    private float _nextEmitTime;

    void Awake()
    {
        _cinput = GetComponent<CharacterInputController>();
    }

    public void EmitFootstepNoise()
    {
        if (_cinput == null) return;

        bool isCrouching = _cinput.IsCrouching;
        bool isRunning = _cinput.IsSprinting;
        float radius = baseNoiseRadius;
        if (isCrouching)
            radius *= crouchNoiseMultiplier;
        else if (isRunning)
            radius *= runNoiseMultiplier;

        if (Time.time < _nextEmitTime) return;
        _nextEmitTime = Time.time + 0.01f;

        EventManager.TriggerEvent<NoiseEmittedEvent, Vector3, float>(transform.position, radius);
    }

}
