using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.5f, -4f);
    public Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);
    public bool lockPositionToTarget = true;
    public float positionSmoothTime = 0.08f;
    public float rotationSmoothSpeed = 12f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target != null)
        {
            Quaternion targetYaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            Vector3 desiredPosition = target.position + targetYaw * offset;
            if (lockPositionToTarget)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, positionSmoothTime);
            }

            Vector3 lookTarget = target.position + lookOffset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationSmoothSpeed);

        }
    }
}
