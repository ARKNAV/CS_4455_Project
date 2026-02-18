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
    
    [Header("IK Settings")]
    public bool useIK = true;
    public float headTurnAngle = 25f;
    public float handRaiseHeight = 0.3f;
    
    private Animator animator;
    private Rigidbody rb;
    private BasicControlScript basicControl;
    
    private float currentPeekAmount = 0f;
    private float targetPeekAmount = 0f;
    private int peekDirection = 0; // -1 = left, 0 = none, 1 = right
    
    private Transform headBone;
    private Transform leftHandBone;
    private Transform rightHandBone;
    private Quaternion originalHeadRotation;
    private Vector3 originalLeftHandPos;
    private Vector3 originalRightHandPos;
    
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
        
        // Get bone references if using humanoid
        if (animator.isHuman && useIK)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            
            if (headBone != null)
            {
                originalHeadRotation = headBone.localRotation;
                Debug.Log("PeekSystem: Found head bone for IK");
            }
            if (leftHandBone != null)
            {
                originalLeftHandPos = leftHandBone.localPosition;
                Debug.Log("PeekSystem: Found left hand bone for IK");
            }
            if (rightHandBone != null)
            {
                originalRightHandPos = rightHandBone.localPosition;
                Debug.Log("PeekSystem: Found right hand bone for IK");
            }
        }
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
    
    void LateUpdate()
    {
        if (!useIK || !animator.isHuman) return;
        
        // Apply IK to head and hands after animation
        float ikWeight = Mathf.Abs(currentPeekAmount);
        
        // Head turning
        if (headBone != null && ikWeight > 0.01f)
        {
            Quaternion targetHeadRot = headBone.localRotation * Quaternion.Euler(0, headTurnAngle * currentPeekAmount * ikWeight, 0);
            headBone.localRotation = targetHeadRot;
        }
        
        // Hand raising based on peek direction
        if (peekDirection == -1 && ikWeight > 0.01f) // Left peek - raise left hand
        {
            Transform leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            
            if (leftUpperArm != null)
            {
                Quaternion addRot = Quaternion.Euler(-15 * ikWeight, 0, -65 * ikWeight);
                leftUpperArm.localRotation = leftUpperArm.localRotation * addRot;
            }
            if (leftLowerArm != null)
            {
                Quaternion addRot = Quaternion.Euler(0, 0, -50 * ikWeight);
                leftLowerArm.localRotation = leftLowerArm.localRotation * addRot;
            }
        }
        else if (peekDirection == 1 && ikWeight > 0.01f) // Right peek - raise right hand
        {
            Transform rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Transform rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            
            if (rightUpperArm != null)
            {
                Quaternion addRot = Quaternion.Euler(-15 * ikWeight, 0, 65 * ikWeight);
                rightUpperArm.localRotation = rightUpperArm.localRotation * addRot;
            }
            if (rightLowerArm != null)
            {
                Quaternion addRot = Quaternion.Euler(0, 0, 50 * ikWeight);
                rightLowerArm.localRotation = rightLowerArm.localRotation * addRot;
            }
        }
    }
}
