using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectTakedownAnims
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Diagnostic from this run:
        // Player GO: (156.140, 0.540, 76.259), Player Hips: (156.140, 1.488, 76.258)
        // Guard  GO: (154.925, 0.540, 78.478), Guard  Hips: (154.950, 1.676, 78.447)
        // Guard fwd: (0.480, 0, -0.877)
        Vector3 guardFwd = new Vector3(0.480f, 0, -0.877f).normalized;
        Quaternion guardRot = Quaternion.LookRotation(guardFwd, Vector3.up);

        Vector3 playerGO = new Vector3(156.140f, 0.540f, 76.259f);
        Vector3 guardGO  = new Vector3(154.925f, 0.540f, 78.478f);
        Vector3 playerHips = new Vector3(156.140f, 1.488f, 76.258f);
        Vector3 guardHips  = new Vector3(154.950f, 1.676f, 78.447f);

        Vector3 hipsDeltaWorld = guardHips - playerHips;
        Vector3 hipsDeltaLocal = Quaternion.Inverse(guardRot) * hipsDeltaWorld;
        sb.AppendLine($"Hips delta world = {hipsDeltaWorld:F4}");
        sb.AppendLine($"Hips delta local (guard frame) = {hipsDeltaLocal:F4}");
        sb.AppendLine("  X>0 means guard hips are to the right of player hips in guard frame");
        sb.AppendLine("  Z>0 means guard hips are ahead of player hips in guard frame");

        Vector3 goDeltaWorld = guardGO - playerGO;
        Vector3 goDeltaLocal = Quaternion.Inverse(guardRot) * goDeltaWorld;
        sb.AppendLine($"\nGO delta world = {goDeltaWorld:F4}");
        sb.AppendLine($"GO delta local = {goDeltaLocal:F4}");

        // For the strangle to look correct:
        // Guard hips should be directly BEHIND player hips (negative Z in guard frame)
        // but close enough that the guard's arms reach the player's neck.
        // Typical spacing: ~0.3m to 0.5m behind.
        //
        // Currently: hips local Z = hipsDeltaLocal.z
        // We want: hips local Z = -0.35 (guard 0.35m behind player)
        // So we need to shift guard GO forward by (hipsDeltaLocal.z - (-0.35)) = hipsDeltaLocal.z + 0.35
        //
        // Also want: hips local X = 0 (directly behind, not offset sideways)
        // So shift guard GO sideways by hipsDeltaLocal.x

        float desiredHipsZ = -0.35f; // guard hips 0.35m behind player hips
        float zCorrection  = hipsDeltaLocal.z - desiredHipsZ; // shift guard FORWARD by this
        float xCorrection  = hipsDeltaLocal.x; // shift guard LEFT by this (negative rightDir)

        sb.AppendLine($"\nCurrent skeletonOffset = 2.53 (GO distance)");
        sb.AppendLine($"Current hips local Z = {hipsDeltaLocal.z:F3} (should be {desiredHipsZ})");
        sb.AppendLine($"Current hips local X = {hipsDeltaLocal.x:F3} (should be 0)");
        sb.AppendLine($"\nShift guard FORWARD by {zCorrection:F3}m in local Z");
        sb.AppendLine($"Shift guard LEFT    by {xCorrection:F3}m in local X");
        sb.AppendLine($"\nNew skeletonOffset (guard GO behind player in local Z):");
        sb.AppendLine($"  = 2.53 - zCorrection = {2.53f - zCorrection:F3}");
        sb.AppendLine($"New rightCorrection = {-xCorrection:F3}");

        return sb.ToString();
    }
}
