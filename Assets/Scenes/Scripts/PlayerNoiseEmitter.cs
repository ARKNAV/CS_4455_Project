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
    [SerializeField] float emissionInterval = 0.4f;
    [SerializeField] float minMoveMagnitude = 0.05f;

    private CharacterInputController _cinput;
    private float _nextEmitTime;

    void Awake()
    {
        _cinput = GetComponent<CharacterInputController>();
    }

    void Update()
    {
        if (_cinput == null) return;

        float moveMag = Mathf.Abs(_cinput.Forward) + Mathf.Abs(_cinput.Turn);
        if (moveMag < minMoveMagnitude) return;

        if (Time.time < _nextEmitTime) return;
        _nextEmitTime = Time.time + emissionInterval;

        bool isCrouching = _cinput.IsCrouching;
        bool isRunning = _cinput.IsSprinting;
        float radius = baseNoiseRadius;
        if (isCrouching)
            radius *= crouchNoiseMultiplier;
        else if (isRunning)
            radius *= runNoiseMultiplier;

        EventManager.TriggerEvent<NoiseEmittedEvent, Vector3, float>(transform.position, radius);
    }

}
