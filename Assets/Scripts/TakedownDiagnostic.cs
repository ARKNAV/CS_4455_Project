using System.Collections;
using UnityEngine;

public class TakedownDiagnostic : MonoBehaviour
{
    private GuardAI _activeGuard;

    void Awake() { }

    public void StartLogging(GuardAI guard)
    {
        _activeGuard = guard;
        StartCoroutine(LogRoutine());
    }

    private IEnumerator LogRoutine()
    {
        yield return null; // wait one frame after snap

        if (_activeGuard == null) yield break;

        Vector3 pp = transform.position;
        Vector3 gp = _activeGuard.transform.position;
        Vector3 pf = transform.forward;
        Vector3 gf = _activeGuard.transform.forward;
        Vector3 delta = gp - pp;
        Vector3 localDelta = Quaternion.Inverse(_activeGuard.transform.rotation) * delta;

        // Find Hips bones
        Transform playerHips = FindBone(transform, "Hips");
        Transform guardHips  = FindBone(_activeGuard.transform, "Hips");

        Vector3 playerHipsWorld = playerHips != null ? playerHips.position : Vector3.zero;
        Vector3 guardHipsWorld  = guardHips  != null ? guardHips.position  : Vector3.zero;
        Vector3 hipsDelta = guardHipsWorld - playerHipsWorld;
        Vector3 hipsLocalDelta = Quaternion.Inverse(_activeGuard.transform.rotation) * hipsDelta;

        Debug.Log($"[TakedownDiag] GO positions: player={pp:F3} fwd={pf:F3} | guard={gp:F3} fwd={gf:F3}");
        Debug.Log($"[TakedownDiag] GO delta: world={delta:F3} | local(guard-space)={localDelta:F3}");
        Debug.Log($"[TakedownDiag] Hips: player={playerHipsWorld:F3} | guard={guardHipsWorld:F3}");
        Debug.Log($"[TakedownDiag] Hips delta: world={hipsDelta:F3} | local(guard-space)={hipsLocalDelta:F3}");
    }

    private Transform FindBone(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindBone(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
