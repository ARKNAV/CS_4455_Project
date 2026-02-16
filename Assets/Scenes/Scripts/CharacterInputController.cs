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

    private float filteredForwardInput = 0f;
    private float filteredTurnInput = 0f;

    public bool InputMapToCircular = true;

    public float forwardInputFilter = 5f;
    public float turnInputFilter = 5f;
    public float crouchSpeedMultiplier = 0.5f;

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

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update () {

    float h = 0f;
    float v = 0f;

#if ENABLE_INPUT_SYSTEM
    if (Keyboard.current != null)
    {
        h = (Keyboard.current.dKey.isPressed ? 1f : 0f) + (Keyboard.current.aKey.isPressed ? -1f : 0f);
        v = (Keyboard.current.wKey.isPressed ? 1f : 0f) + (Keyboard.current.sKey.isPressed ? -1f : 0f);
        Jump = Keyboard.current.spaceKey.wasPressedThisFrame;
        
        isCrouching = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed;
    }
#else
    
    h = Input.GetAxisRaw("Horizontal");
    v = Input.GetAxisRaw("Vertical");
    Jump = Input.GetButtonDown("Jump");
    
    isCrouching = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
#endif


        if (InputMapToCircular)
        {
  
            h = h * Mathf.Sqrt(1f - 0.5f * v * v);
            v = v * Mathf.Sqrt(1f - 0.5f * h * h);        


        filteredForwardInput = Mathf.Clamp(Mathf.Lerp(filteredForwardInput, v, 
            Time.deltaTime * forwardInputFilter), -forwardSpeedLimit, forwardSpeedLimit);

        filteredTurnInput = Mathf.Lerp(filteredTurnInput, h, 
            Time.deltaTime * turnInputFilter);

        if (isCrouching)
        {
            filteredForwardInput *= crouchSpeedMultiplier;
            filteredTurnInput *= crouchSpeedMultiplier;
        }

        Forward = filteredForwardInput;
        Turn = filteredTurnInput;

        if (animator != null)
        {
            float speed = new Vector2(h, v).magnitude;
            if (isCrouching)
                speed *= crouchSpeedMultiplier;
            animator.SetFloat("speed", speed);
            animator.SetBool("isCrouching", isCrouching);
            
            Vector2 moveVector = new Vector2(h, v);
            moveVector = Vector2.ClampMagnitude(moveVector, 1f);
            if (isCrouching)
                moveVector *= crouchSpeedMultiplier;
            animator.SetFloat("MoveX", moveVector.x, 0.1f, Time.deltaTime);
            animator.SetFloat("MoveY", moveVector.y, 0.1f, Time.deltaTime);
        }

        }

	}
}
