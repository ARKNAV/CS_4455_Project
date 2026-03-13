using UnityEngine;

public class WallPeek : MonoBehaviour
{
    [Header("Wall Detection")]
    public float wallCheckHeight = 1.35f;
    public float wallCheckDistance = 0.9f;
    public float sideClearanceDistance = 0.8f;
    public float sideProbeForwardOffset = 0.45f;
    public float wallMinNormalY = 0.35f;
    public LayerMask blockingLayers = Physics.DefaultRaycastLayers;

    private bool hasWallInFront;
    private bool canPeekLeft;
    private bool canPeekRight;
    private Vector3 wallNormal = Vector3.forward;

    public bool HasWallInFront
    {
        get { return hasWallInFront; }
    }

    public bool CanPeekLeft
    {
        get { return hasWallInFront && canPeekLeft; }
    }

    public bool CanPeekRight
    {
        get { return hasWallInFront && canPeekRight; }
    }

    public Vector3 WallNormal
    {
        get { return wallNormal; }
    }

    void Update()
    {
        EvaluateCover();
    }

    private void EvaluateCover()
    {
        Vector3 origin = transform.position + (Vector3.up * wallCheckHeight);
        Vector3 forward = transform.forward;

        hasWallInFront = Physics.Raycast(origin, forward, out RaycastHit wallHit, wallCheckDistance, blockingLayers, QueryTriggerInteraction.Ignore);
        if (!hasWallInFront)
        {
            canPeekLeft = false;
            canPeekRight = false;
            return;
        }

        if (Mathf.Abs(wallHit.normal.y) > wallMinNormalY)
        {
            hasWallInFront = false;
            canPeekLeft = false;
            canPeekRight = false;
            return;
        }

        wallNormal = wallHit.normal;

        Vector3 sideOrigin = wallHit.point - (wallHit.normal * 0.08f) + (Vector3.up * 0.05f);
        Vector3 left = -transform.right;
        Vector3 right = transform.right;

        canPeekLeft = CanPeekToSide(sideOrigin, left);
        canPeekRight = CanPeekToSide(sideOrigin, right);
    }

    private bool CanPeekToSide(Vector3 wallPoint, Vector3 sideDirection)
    {
        Vector3 sideOrigin = wallPoint + (sideDirection * 0.1f);
        bool sideBlocked = Physics.Raycast(
            sideOrigin,
            sideDirection,
            sideClearanceDistance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        if (sideBlocked)
        {
            return false;
        }

        Vector3 forwardProbeOrigin = sideOrigin + (sideDirection * sideProbeForwardOffset);
        bool forwardBlocked = Physics.Raycast(
            forwardProbeOrigin,
            transform.forward,
            wallCheckDistance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);

        return !forwardBlocked;
    }
}
