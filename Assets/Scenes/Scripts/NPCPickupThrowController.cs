using UnityEngine;

public class NPCPickupThrowController : MonoBehaviour
{
    [Header("Input")]
    public CharacterInputController actionInput;

    [Header("Detection")]
    public Transform holdPoint;
    public float pickupRange = 2.8f;
    public float pickupSphereRadius = 0.35f;
    public LayerMask interactableLayers = ~0;

    [Header("Hold")]
    public Vector3 holdOffset = new Vector3(0.35f, -0.15f, 0.8f);
    public float holdFollowSpeed = 18f;
    public bool keepKinematicWhileHeld = true;

    [Header("Throw")]
    public float throwForce = 12f;
    public float upwardThrowBias = 0.25f;

    private PickableObject heldPickable;
    private Rigidbody heldBody;
    private Transform holdAnchor;
    private bool actionPressedLastFrame;

    void Awake()
    {
        if (actionInput == null)
        {
            actionInput = GetComponent<CharacterInputController>();
        }

        holdPoint = ResolveHoldPoint(holdPoint);
    }

    void Update()
    {
        if (actionInput == null)
        {
            return;
        }

        if (actionInput.Action && !actionPressedLastFrame)
        {
            if (heldPickable == null)
            {
                TryPick();
            }
            else
            {
                ThrowHeld();
            }
        }

        actionPressedLastFrame = actionInput.Action;

        if (heldBody != null)
        {
            UpdateHeldPosition();
        }
    }

    private void TryPick()
    {
        if (holdAnchor == null)
        {
            holdAnchor = ResolveHoldPoint(holdPoint);
        }

        PickableObject pickable = TryGetPickableInRange();

        if (pickable == null || pickable.IsHeld)
        {
            return;
        }

        Rigidbody rb = pickable.Rigidbody;
        if (rb == null)
        {
            return;
        }

        heldPickable = pickable;
        heldBody = rb;

        heldPickable.OnPickedUp();
        heldBody.detectCollisions = false;
        heldBody.useGravity = false;

        if (keepKinematicWhileHeld)
        {
            heldBody.isKinematic = true;
        }

        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.position = holdAnchor.TransformPoint(holdOffset);
        heldBody.rotation = holdAnchor.rotation;
    }

    private PickableObject TryGetPickableInRange()
    {
        Transform anchor = holdAnchor != null ? holdAnchor : ResolveHoldPoint(holdPoint);
        Vector3 origin = anchor.position;
        Vector3 forward = anchor.forward;
        Vector3 sphereCastStart = origin + forward * 0.2f;

        if (Physics.SphereCast(sphereCastStart, pickupSphereRadius, forward, out RaycastHit hit, pickupRange, interactableLayers, QueryTriggerInteraction.Ignore))
        {
            PickableObject pickable = hit.collider.GetComponentInParent<PickableObject>();
            if (pickable != null && !pickable.IsHeld)
            {
                return pickable;
            }
        }

        Collider[] nearby = Physics.OverlapSphere(origin, pickupRange, interactableLayers, QueryTriggerInteraction.Ignore);
        float bestDistanceSqr = float.MaxValue;
        PickableObject bestPickable = null;

        for (int i = 0; i < nearby.Length; i++)
        {
            PickableObject candidate = nearby[i].GetComponentInParent<PickableObject>();

            if (candidate == null || candidate.IsHeld)
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestPickable = candidate;
            }
        }

        return bestPickable;
    }

    private void ThrowHeld()
    {
        if (heldBody == null)
        {
            return;
        }

        Transform anchor = holdAnchor != null ? holdAnchor : ResolveHoldPoint(holdPoint);
        Vector3 forward = anchor.forward;
        Vector3 throwDirection = (forward + (Vector3.up * upwardThrowBias)).normalized;

        RestoreHeldBodyPhysicsForThrow();
        heldBody.linearVelocity = Vector3.zero;
        heldBody.angularVelocity = Vector3.zero;
        heldBody.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        heldPickable?.OnDropped();

        heldPickable = null;
        heldBody = null;
    }

    private void RestoreHeldBodyPhysicsForThrow()
    {
        heldBody.isKinematic = false;
        heldBody.useGravity = true;
        heldBody.detectCollisions = true;
    }

    private void UpdateHeldPosition()
    {
        Transform anchor = holdAnchor != null ? holdAnchor : ResolveHoldPoint(holdPoint);
        Vector3 targetPosition = anchor.TransformPoint(holdOffset);
        Quaternion targetRotation = anchor.rotation;

        float followStep = 1f - Mathf.Exp(-holdFollowSpeed * Time.deltaTime);

        heldBody.MovePosition(Vector3.Lerp(heldBody.position, targetPosition, followStep));
        heldBody.MoveRotation(Quaternion.Slerp(heldBody.rotation, targetRotation, followStep));
    }

    void OnDisable()
    {
        if (heldPickable != null)
        {
            RestoreHeldBodyPhysicsForThrow();
            heldPickable.OnDropped();
        }

        heldPickable = null;
        heldBody = null;
    }

    private Transform ResolveHoldPoint(Transform candidate)
    {
        if (candidate != null && candidate != transform && !IsCameraTransform(candidate))
        {
            return candidate;
        }

        Transform childHoldPoint = transform.Find("HoldPoint");
        if (childHoldPoint != null)
        {
            return childHoldPoint;
        }

        return transform;
    }

    private bool IsCameraTransform(Transform candidate)
    {
        return candidate != null && Camera.main != null && candidate == Camera.main.transform;
    }
}
