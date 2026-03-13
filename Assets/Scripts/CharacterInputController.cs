using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CharacterInputController : MonoBehaviour {

    public string Name = "George P Burdell";

    private Animator animator;
    private bool isCrouching = false;
    private bool isSprinting = false;

    private float filteredForwardInput = 0f;
    private float filteredTurnInput = 0f;

    public bool InputMapToCircular = true;

    public float forwardInputFilter = 5f;
    public float turnInputFilter = 5f;
    public float crouchSpeedMultiplier = 0.5f;

    public float sprintSpeedMultiplier = 1.8f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 25f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;
    public float minStaminaToSprint = 10f;

    private float currentStamina;
    private float staminaRegenTimer;

    private float forwardSpeedLimit = 1f;

    public float Forward
    {
        get;
        private set;
    }

    public float Turn
    {
        get;
        private set;
    }

    public bool Action
    {
        get;
        private set;
    }

    public bool Jump
    {
        get;
        private set;
    }

    public bool IsCrouching
    {
        get { return isCrouching; }
    }

    public bool IsSprinting
    {
        get { return isSprinting; }
    }

    public float CurrentStamina
    {
        get { return currentStamina; }
    }

    public float MaxStamina
    {
        get { return maxStamina; }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        currentStamina = maxStamina;
    }

    void Update () {

    float h = 0f;
    float v = 0f;
    bool sprintHeld = false;
    bool spacePressedThisFrame = false;

	#if ENABLE_INPUT_SYSTEM
	    if (Keyboard.current != null)
	    {
	        h = (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f);
	        v = (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f);
	        Action = Keyboard.current != null && (Keyboard.current.digit1Key.wasPressedThisFrame);

	        isCrouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
	        sprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
	        spacePressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
	    }
	#else

	    h = Input.GetAxisRaw("Horizontal");
	    v = Input.GetAxisRaw("Vertical");
	    Action = Input.GetKeyDown(KeyCode.Alpha1);

	    isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
	    sprintHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	    spacePressedThisFrame = Input.GetKeyDown(KeyCode.Space);
	#endif

        Jump = false;
        if (spacePressedThisFrame)
        {
            Jump = true;
        }

        bool hasMovement = Mathf.Abs(v) > 0.001f || Mathf.Abs(h) > 0.001f;
        bool canStartSprint = sprintHeld && hasMovement && !isCrouching && currentStamina > minStaminaToSprint;
        bool canContinueSprint = sprintHeld && hasMovement && !isCrouching && isSprinting && currentStamina > 0f;

        if (canStartSprint || canContinueSprint)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            staminaRegenTimer = staminaRegenDelay;

            if (currentStamina <= 0f)
                isSprinting = false;
        }
        else
        {
            isSprinting = false;
            staminaRegenTimer -= Time.deltaTime;
            if (staminaRegenTimer <= 0f)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        if (InputMapToCircular)
        {

            h = h * Mathf.Sqrt(1f - 0.5f * v * v);
            v = v * Mathf.Sqrt(1f - 0.5f * h * h);


        bool hasMoveInput = Mathf.Abs(h) > 0.001f || Mathf.Abs(v) > 0.001f;

        if (hasMoveInput)
        {
            filteredForwardInput = Mathf.Clamp(Mathf.Lerp(filteredForwardInput, v,
                Time.deltaTime * forwardInputFilter), -forwardSpeedLimit, forwardSpeedLimit);

            filteredTurnInput = Mathf.Lerp(filteredTurnInput, h,
                Time.deltaTime * turnInputFilter);
        }
        else
        {
            filteredForwardInput = 0f;
            filteredTurnInput = 0f;
        }

        float outputForward = filteredForwardInput;
        float outputTurn = filteredTurnInput;

        if (isCrouching)
        {
            outputForward *= crouchSpeedMultiplier;
            outputTurn *= crouchSpeedMultiplier;
        }

        Forward = outputForward;
        Turn = outputTurn;

        if (animator != null)
        {
            Vector2 moveVector = new Vector2(h, v);
            moveVector = Vector2.ClampMagnitude(moveVector, 1f);
            
            float speed = moveVector.magnitude;
            if (isCrouching)
                speed *= crouchSpeedMultiplier;
            else if (isSprinting)
                speed *= sprintSpeedMultiplier;
            
            animator.SetFloat("speed", speed);
            animator.SetBool("isCrouching", isCrouching);
            animator.SetBool("isSprinting", isSprinting);
            animator.SetFloat("MoveX", moveVector.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", moveVector.y, 0.1f, Time.deltaTime);
        }

        }

	}
}
