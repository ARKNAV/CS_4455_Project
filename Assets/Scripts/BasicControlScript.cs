using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(Rigidbody), typeof(CapsuleCollider))]
[RequireComponent(typeof(CharacterInputController))]
public class BasicControlScript : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rbody;
    private CharacterInputController cinput;
    private AudioSource audioSource;

    public Transform cameraTransform;

    public float forwardMaxSpeed = 1f;
    public float turnMaxSpeed = 1f;
    public float maxTurnDegreesPerSecond = 360f;
    public float jumpableGroundNormalMaxAngle = 45f;
    public bool closeToJumpableGround;

    public float acceleration = 14f;
    public float deceleration = 18f;
    public float airControlMultiplier = 0.35f;
    public float groundCheckDistance = 1.1f;

    [Header("Jump Physics")]
    [SerializeField] private float jumpImpulse = 7f;
    [SerializeField] private bool useCustomAirGravity = true;
    [SerializeField] private float ascendingGravityMultiplier = 1.15f;
    [SerializeField] private float descendingGravityMultiplier = 2.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip jumpClip;

    [Header("Animation")]
    [SerializeField] private bool driveJumpAnimation = true;
    [SerializeField] private string jumpTriggerParameter = "Jump";
    [SerializeField] private string groundedBoolParameter = "IsGrounded";

    private readonly HashSet<int> groundContacts = new HashSet<int>();
    private Vector3 desiredPlanarVelocity;
    private Vector3 currentPlanarVelocity;
    private bool hasGroundSupport;
    private int jumpTriggerHash;
    private int groundedBoolHash;
    private bool hasJumpTrigger;
    private bool hasGroundedBool;

    public bool IsGrounded
    {
        get
        {
            return groundContacts.Count > 0;
        }
    }

    public bool HasGroundSupport
    {
        get
        {
            return hasGroundSupport;
        }
    }

    public Vector3 CurrentPlanarVelocity
    {
        get
        {
            return currentPlanarVelocity;
        }
    }

    void Awake()
    {
        anim = GetComponent<Animator>();
        rbody = GetComponent<Rigidbody>();
        cinput = GetComponent<CharacterInputController>();
        audioSource = GetComponent<AudioSource>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        anim.applyRootMotion = false;
        CacheAnimationParameters();
    }

    void Update()
    {
        float inputForward = 0f;
        float inputTurn = 0f;

        if (cinput.enabled)
        {
            inputForward = cinput.Forward;
            inputTurn = cinput.Turn;
        }

        Vector2 moveInput = Vector2.ClampMagnitude(new Vector2(inputTurn, inputForward), 1f);
        Vector3 moveForward;
        Vector3 moveRight;

        if (cameraTransform != null)
        {
            moveForward = cameraTransform.forward;
            moveRight = cameraTransform.right;
        }
        else
        {
            moveForward = transform.forward;
            moveRight = transform.right;
        }

        moveForward.y = 0f;
        moveRight.y = 0f;
        moveForward.Normalize();
        moveRight.Normalize();

        float effectiveForwardSpeed = forwardMaxSpeed;
        float effectiveTurnSpeed = turnMaxSpeed;

        if (cinput.IsSprinting)
        {
            effectiveForwardSpeed *= cinput.sprintSpeedMultiplier;
            effectiveTurnSpeed *= cinput.sprintSpeedMultiplier;
        }

        desiredPlanarVelocity =
            (moveForward * (moveInput.y * effectiveForwardSpeed)) +
            (moveRight * (moveInput.x * effectiveTurnSpeed));

        if (desiredPlanarVelocity.sqrMagnitude > 0.0001f)
        {
            desiredPlanarVelocity = Vector3.ClampMagnitude(desiredPlanarVelocity, Mathf.Max(effectiveForwardSpeed, effectiveTurnSpeed));
        }

        if (cinput.Jump && hasGroundSupport)
        {
            rbody.linearVelocity = new Vector3(rbody.linearVelocity.x, 0f, rbody.linearVelocity.z);
            rbody.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            PlayClip(jumpClip);
            TriggerJumpAnimation();
        }
    }

    void FixedUpdate()
    {
        UpdateGroundSupport();

        float controlMultiplier = hasGroundSupport ? 1f : airControlMultiplier;
        bool hasMoveInput = desiredPlanarVelocity.sqrMagnitude > 0.0001f;
        float moveRate = hasMoveInput ? acceleration : deceleration;
        float dragCompensation = 1f + rbody.linearDamping * Time.fixedDeltaTime;
        float maxStep = moveRate * controlMultiplier * Time.fixedDeltaTime * dragCompensation;

        Vector3 rigidbodyVelocity = rbody.linearVelocity;
        Vector3 planarVelocity = new Vector3(rigidbodyVelocity.x, 0f, rigidbodyVelocity.z);
        Vector3 targetPlanarVelocity = desiredPlanarVelocity * controlMultiplier;

        if (cinput.IsSprinting)
        {
            currentPlanarVelocity = Vector3.Lerp(planarVelocity, targetPlanarVelocity, 0.3f);
        }
        else
        {
            currentPlanarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, maxStep);
        }
        rbody.linearVelocity = new Vector3(currentPlanarVelocity.x, rigidbodyVelocity.y, currentPlanarVelocity.z);

        ApplyAirGravity();

        Vector3 facingDirection = hasMoveInput ? desiredPlanarVelocity : currentPlanarVelocity;
        facingDirection.y = 0f;

        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(facingDirection.normalized, Vector3.up);
            float maxRotationStep = maxTurnDegreesPerSecond * Time.fixedDeltaTime;
            Quaternion limitedRotation = Quaternion.RotateTowards(rbody.rotation, targetRotation, maxRotationStep);
            rbody.MoveRotation(limitedRotation);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (TryRegisterGroundCollision(collision))
        {
            EventManager.TriggerEvent<PlayerLandsEvent, Vector3, float>(
                collision.contacts[0].point,
                collision.impulse.magnitude);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        TryRegisterGroundCollision(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        groundContacts.Remove(collision.collider.GetInstanceID());
    }

    private void UpdateGroundSupport()
    {
        bool groundNear = CharacterCommon.CheckGroundNear(
            transform.position,
            jumpableGroundNormalMaxAngle,
            0.15f,
            groundCheckDistance,
            out bool isCloseToGround);

        closeToJumpableGround = isCloseToGround;
        hasGroundSupport = IsGrounded || groundNear;

        if (driveJumpAnimation && hasGroundedBool && anim != null)
        {
            anim.SetBool(groundedBoolHash, hasGroundSupport);
        }
    }

    private bool TryRegisterGroundCollision(Collision collision)
    {
        if (collision.collider == null || collision.collider.isTrigger)
        {
            return false;
        }

        for (int i = 0; i < collision.contactCount; ++i)
        {
            ContactPoint contact = collision.GetContact(i);
            float surfaceAngle = Vector3.Angle(contact.normal, Vector3.up);
            if (surfaceAngle <= jumpableGroundNormalMaxAngle)
            {
                return groundContacts.Add(collision.collider.GetInstanceID());
            }
        }

        return false;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void CacheAnimationParameters()
    {
        if (!driveJumpAnimation || anim == null || anim.runtimeAnimatorController == null)
        {
            return;
        }

        jumpTriggerHash = Animator.StringToHash(jumpTriggerParameter);
        groundedBoolHash = Animator.StringToHash(groundedBoolParameter);

        for (int i = 0; i < anim.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = anim.parameters[i];
            if (parameter.nameHash == jumpTriggerHash && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                hasJumpTrigger = true;
            }

            if (parameter.nameHash == groundedBoolHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasGroundedBool = true;
            }
        }

        if (!hasJumpTrigger)
        {
            Debug.LogWarning($"BasicControlScript: Missing Trigger parameter '{jumpTriggerParameter}' on Animator.", this);
        }

        if (!hasGroundedBool)
        {
            Debug.LogWarning($"BasicControlScript: Missing Bool parameter '{groundedBoolParameter}' on Animator.", this);
        }
    }

    private void TriggerJumpAnimation()
    {
        if (!driveJumpAnimation || anim == null || !hasJumpTrigger)
        {
            return;
        }

        anim.SetTrigger(jumpTriggerHash);
    }

    private void ApplyAirGravity()
    {
        if (!useCustomAirGravity || hasGroundSupport || !rbody.useGravity)
        {
            return;
        }

        float verticalSpeed = rbody.linearVelocity.y;
        float multiplier = verticalSpeed >= 0f ? ascendingGravityMultiplier : descendingGravityMultiplier;

        if (multiplier <= 1f)
        {
            return;
        }

        Vector3 extraGravity = Physics.gravity * (multiplier - 1f);
        rbody.AddForce(extraGravity, ForceMode.Acceleration);
    }
}
