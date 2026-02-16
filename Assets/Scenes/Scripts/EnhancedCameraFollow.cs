using UnityEngine;

public class EnhancedCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(1f, 3f, -10f);
    
    [Header("Camera Smoothing")]
    public float followSpeed = 10f;
    public float rotationSpeed = 5f;
    
    [Header("Peek Camera Settings")]
    public float normalFOV = 60f;
    public float peekFOV = 40f;
    public float peekZoomDistance = 3f;
    public float fovTransitionSpeed = 8f;
    public float peekCameraOffset = 3.5f;
    
    private Camera cam;
    private PeekSystem peekSystem;
    private float currentFOV;
    private Vector3 currentOffset;

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
        }
        
        currentOffset = offset;
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        float peekAmount = 0f;
        if (peekSystem != null)
        {
            peekAmount = peekSystem.PeekAmount;
        }
        
        float targetFOV = normalFOV;
        Vector3 targetOffset = offset;
        
        if (Mathf.Abs(peekAmount) > 0.01f)
        {
            targetFOV = Mathf.Lerp(normalFOV, peekFOV, Mathf.Abs(peekAmount));
            float zoomFactor = Mathf.Abs(peekAmount);
            targetOffset.z = offset.z + (peekZoomDistance * zoomFactor);
            targetOffset.x = offset.x + (peekAmount * peekCameraOffset);
        }
        
        if (cam != null)
        {
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * fovTransitionSpeed);
            cam.fieldOfView = currentFOV;
        }
        
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.deltaTime * fovTransitionSpeed);
        Vector3 desiredPosition = target.position + target.TransformDirection(currentOffset);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        
        Vector3 lookTarget = target.position + Vector3.up * 1.5f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
