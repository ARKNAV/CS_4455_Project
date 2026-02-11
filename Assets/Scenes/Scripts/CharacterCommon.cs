using UnityEngine;

public static class CharacterCommon
{
    public static bool CheckGroundNear(Vector3 position, float maxAngle, float checkDistance, float castDistance, out bool closeToGround)
    {
        closeToGround = false;

        var origin = position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance))
        {
            var angle = Vector3.Angle(hit.normal, Vector3.up);
            if (angle <= maxAngle)
            {
                closeToGround = hit.distance <= checkDistance;
                return true;
            }
        }

        return false;
    }
}
