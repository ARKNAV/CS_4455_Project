using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PeekSystem : MonoBehaviour
{
    [Header("Animation Settings")]
    public bool useAnimations = true;
    
    [Header("Peek Settings")]
    public float peekDistance = 0.5f;
    public float peekRotation = 15f;
    public float peekSpeed = 8f;
    
    [Header("Body Lean Settings")]
    public Transform spineBone;
    public float spineLeanAngle = 10f;
    
    private Animator animator;
    private Rigidbody rb;
    private BasicControlScript basicControl;
    
    private float currentPeekAmount = 0f;
    private float targetPeekAmount = 0f;
    private int peekDirection = 0; // -1 = left, 0 = none, 1 = right
    
    private bool qKeyPressed = false;
    private bool qKeyWasPressed = false;
    private bool eKeyPressed = false;
    private bool eKeyWasPressed = false;
    
    public bool IsPeeking
    {
        get { return Mathf.Abs(currentPeekAmount) > 0.01f; }
    }
    
    public float PeekAmount
    {
        get { return currentPeekAmount; }
    }
    
    public int PeekDirection
    {
        get { return peekDirection; }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("PeekSystem requires an Animator component!");
            enabled = false;
            return;
        }
        
        rb = GetComponent<Rigidbody>();
        basicControl = GetComponent<BasicControlScript>();
    }

    void Update()
    {
        HandlePeekInput();
        UpdatePeek();
    }

    void HandlePeekInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            qKeyPressed = Keyboard.current.qKey.isPressed;
            eKeyPressed = Keyboard.current.eKey.isPressed;
        }
#else
        qKeyPressed = Input.GetKey(KeyCode.Q);
        eKeyPressed = Input.GetKey(KeyCode.E);
#endif

        if (qKeyPressed && !qKeyWasPressed)
        {
            if (peekDirection == -1)
            {
                peekDirection = 0;
            }
            else
            {
                peekDirection = -1;
            }
        }
        
        if (eKeyPressed && !eKeyWasPressed)
        {
            if (peekDirection == 1)
            {
                peekDirection = 0;
            }
            else
            {
                peekDirection = 1;
            }
        }
        
        qKeyWasPressed = qKeyPressed;
        eKeyWasPressed = eKeyPressed;

        if (peekDirection == -1)
        {
            targetPeekAmount = -1f;
        }
        else if (peekDirection == 1)
        {
            targetPeekAmount = 1f;
        }
        else
        {
            targetPeekAmount = 0f;
        }
    }

    void UpdatePeek()
    {
        currentPeekAmount = Mathf.Lerp(currentPeekAmount, targetPeekAmount, Time.deltaTime * peekSpeed);
        
        if (useAnimations && animator != null)
        {
            animator.SetInteger("PeekDirection", peekDirection);
            animator.SetBool("IsPeeking", peekDirection != 0);
            
            if (rb != null && Mathf.Abs(currentPeekAmount) > 0.1f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.95f, rb.linearVelocity.y, rb.linearVelocity.z * 0.95f);
            }
        }
    }

    void OnDisable()
    {
        if (animator != null)
        {
            animator.SetInteger("PeekDirection", 0);
            animator.SetBool("IsPeeking", false);
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null && Mathf.Abs(currentPeekAmount) > 0.1f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x *= 0.9f;
            velocity.z *= 0.9f;
            rb.linearVelocity = velocity;
        }
    }
}
