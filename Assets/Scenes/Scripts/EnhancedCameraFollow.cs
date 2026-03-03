using UnityEngine;

public class EnhancedCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(1f, 3f, -10f);
    public Vector3 lookOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Camera Smoothing")]
    public float followSpeed = 10f;
    public float rotationSpeed = 5f;
    public float yawFollowSpeed = 8f;
    public float movementLookAhead = 1f;
    public float sideLookAhead = 0.4f;
    public float lookAheadResponse = 6f;

    [Header("Peek Camera Settings")]
    public float normalFOV = 60f;
    public float peekFOV = 40f;
    public float peekZoomDistance = 3f;
    public float fovTransitionSpeed = 8f;
    public float peekCameraOffset = 3.5f;
    public float peekLookOffset = 0.35f;

    private Camera cam;
    private PeekSystem peekSystem;
    private BasicControlScript basicControl;
    private float currentFOV;
    private float smoothedYaw;
    private float yawVelocity;
    private Vector3 currentOffset;
    private Vector3 smoothedLookAhead;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            currentFOV = normalFOV;
            cam.fieldOfView = normalFOV;
        }

        if (target != null)
        {
            peekSystem = target.GetComponent<PeekSystem>();
            basicControl = target.GetComponent<BasicControlScript>();
            smoothedYaw = target.eulerAngles.y;
        }

        currentOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        float peekAmount = 0f;
        if (peekSystem != null)
        {
            peekAmount = peekSystem.PeekAmount;
        }

        float targetFOV = normalFOV;
        Vector3 targetOffset = offset;

        if (Mathf.Abs(peekAmount) > 0.01f)
        {
            float zoomFactor = Mathf.Abs(peekAmount);
            targetFOV = Mathf.Lerp(normalFOV, peekFOV, zoomFactor);
            targetOffset.z = offset.z + (peekZoomDistance * zoomFactor);
            targetOffset.x = offset.x + (peekAmount * peekCameraOffset);
        }

        if (cam != null)
        {
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
            cam.fieldOfView = currentFOV;
        }

        smoothedYaw = Mathf.SmoothDampAngle(
            smoothedYaw,
            target.eulerAngles.y,
            ref yawVelocity,
            1f / Mathf.Max(0.01f, yawFollowSpeed));

        Quaternion targetYaw = Quaternion.Euler(0f, smoothedYaw, 0f);
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * fovTransitionSpeed);

        Vector3 desiredPosition = target.position + (targetYaw * currentOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        Vector3 desiredLookAhead = Vector3.zero;
        if (basicControl != null)
        {
            Vector3 planarVelocity = basicControl.CurrentPlanarVelocity;
            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 moveDirection = planarVelocity.normalized;
                float speedRatio = Mathf.Clamp01(planarVelocity.magnitude / Mathf.Max(0.01f, basicControl.forwardMaxSpeed));
                Vector3 yawRight = targetYaw * Vector3.right;

                desiredLookAhead += moveDirection * (movementLookAhead * speedRatio);
                desiredLookAhead += yawRight * (Vector3.Dot(moveDirection, yawRight) * sideLookAhead * speedRatio);
            }
        }

        desiredLookAhead += (targetYaw * Vector3.right) * (peekAmount * peekLookOffset);
        smoothedLookAhead = Vector3.Lerp(smoothedLookAhead, desiredLookAhead, Time.deltaTime * lookAheadResponse);

        Vector3 lookTarget = target.position + lookOffset + smoothedLookAhead;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
