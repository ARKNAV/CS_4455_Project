using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator), typeof(Rigidbody), typeof(CapsuleCollider))]
[RequireComponent(typeof(CharacterInputController))]
public class BasicControlScript : MonoBehaviour
{
    private Animator anim;	
    private Rigidbody rbody;
    private CharacterInputController cinput;

    public Transform cameraTransform;

    private Transform leftFoot;
    private Transform rightFoot;
    
    private FootstepSound footstepSound;
    private float lastFootstepTime = 0f;
    private float footstepCooldown = 0.3f;

    public float forwardMaxSpeed = 1f;
    public float turnMaxSpeed = 1f;
    public float maxTurnDegreesPerSecond = 360f;
    public float jumpableGroundNormalMaxAngle = 45f;
    public bool closeToJumpableGround;


    private int groundContactCount = 0;

    public bool IsGrounded
    {
        get
        {
            return groundContactCount > 0;
        }
    }


    void Awake()
    {

        anim = GetComponent<Animator>();

        if (anim == null)
            Debug.Log("Animator could not be found");

        rbody = GetComponent<Rigidbody>();

        if (rbody == null)
            Debug.Log("Rigid body could not be found");

        cinput = GetComponent<CharacterInputController>();

        if (cinput == null)
            Debug.Log("CharacterInputController could not be found");

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

    }


    void Start()
    {
        leftFoot = this.transform.Find("mixamorig:Hips/mixamorig:LeftUpLeg/mixamorig:LeftLeg/mixamorig:LeftFoot");
        rightFoot = this.transform.Find("mixamorig:Hips/mixamorig:RightUpLeg/mixamorig:RightLeg/mixamorig:RightFoot");

        if (leftFoot == null || rightFoot == null)
            Debug.Log("One of the feet could not be found");

        anim.applyRootMotion = false;

    }


    void Update() {

        float inputForward=0f;
        float inputTurn=0f;

        if (cinput.enabled)
        {
            inputForward = cinput.Forward;
            inputTurn = cinput.Turn;
        }

        bool isGrounded = IsGrounded || CharacterCommon.CheckGroundNear(this.transform.position, jumpableGroundNormalMaxAngle, 0.1f, 1f, out closeToJumpableGround);

        Vector3 inputVector = new Vector3(inputTurn, 0f, inputForward);
        inputVector = Vector3.ClampMagnitude(inputVector, 1f);

        Vector3 moveDirection;
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            moveDirection = (cameraForward * inputVector.z) + (cameraRight * inputVector.x);
        }
        else
        {
            moveDirection = inputVector;
        }

        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        Vector3 move = moveDirection * Time.deltaTime * forwardMaxSpeed;
        rbody.MovePosition(rbody.position + move);

        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            float maxStep = maxTurnDegreesPerSecond * Time.deltaTime;
            Quaternion limitedRotation = Quaternion.RotateTowards(rbody.rotation, targetRotation, maxStep);
            rbody.MoveRotation(limitedRotation);
        }

    }




    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.gameObject.tag == "ground")
        {
            ++groundContactCount;
            EventManager.TriggerEvent<PlayerLandsEvent, Vector3, float>(collision.contacts[0].point, collision.impulse.magnitude);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.transform.gameObject.tag == "ground")
        {
            --groundContactCount;

        }
    }

}
