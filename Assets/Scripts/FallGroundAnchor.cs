using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Physically rotates a character to lie flat on the floor when knocked out or tackled.
/// Works for both the player (Rigidbody + BasicControlScript) and guards (NavMeshAgent).
///
/// Usage:
///   var anchor = GetOrAdd<FallGroundAnchor>(gameObject);
///   anchor.StartFall();          // tilt to face-down over tiltDuration seconds
///   anchor.RestoreUpright(cb);   // tilt back to upright (for guard recovery)
/// </summary>
[RequireComponent(typeof(Animator))]
public class FallGroundAnchor : MonoBehaviour
{
    [Tooltip("Seconds to lerp from standing to lying flat")]
    public float tiltDuration = 0.32f;

    [Tooltip("Ground detection layers")]
    public LayerMask groundMask = ~0;

    // ── Cached components ─────────────────────────────────────────────────
    private Rigidbody         _rb;
    private BasicControlScript _movement;
    private NavMeshAgent      _agent;

    // ── State ─────────────────────────────────────────────────────────────
    private Quaternion _savedUprightRotation;
    private bool       _fallen;
    private bool       _wasKinematic;
    private Coroutine  _active;

    public bool IsFallen => _fallen;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _rb       = GetComponent<Rigidbody>();
        _movement  = GetComponent<BasicControlScript>();
        _agent    = GetComponent<NavMeshAgent>();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Smoothly rotate this object so the character lies face-down on the floor.
    /// Freezes Rigidbody/BasicControlScript to prevent movement fighting.
    /// </summary>
    public void StartFall()
    {
        if (_fallen) return;
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(DoFall());
    }

    /// <summary>
    /// Smoothly rotate back to upright. Optional callback fires when done.
    /// </summary>
    public void RestoreUpright(Action onComplete = null)
    {
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(DoRestore(onComplete));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Coroutines
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator DoFall()
    {
        _fallen = true;
        _savedUprightRotation = transform.rotation;

        float floorY = FindFloorY();

        // Freeze conflicting movement systems
        if (_rb != null)
        {
            _wasKinematic        = _rb.isKinematic;
            _rb.linearVelocity   = Vector3.zero;
            _rb.angularVelocity  = Vector3.zero;
            _rb.isKinematic      = true;
        }
        if (_movement != null)
            _movement.enabled = false;
        if (_agent != null && _agent.enabled)
        {
            _agent.isStopped = true;
            _agent.enabled   = false;
        }

        // Face-down: Euler(90, currentY, 0)  — character tilts forward onto stomach
        Quaternion startRot = transform.rotation;
        Quaternion endRot   = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        Vector3    startPos = transform.position;
        Vector3    endPos   = new Vector3(startPos.x, floorY, startPos.z);

        float elapsed = 0f;
        while (elapsed < tiltDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / tiltDuration);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
        transform.position = endPos;
        Debug.Log($"[FallAnchor] '{name}': lying flat at Y={endPos.y:F2}");
    }

    private IEnumerator DoRestore(Action onComplete)
    {
        Quaternion fromRot = transform.rotation;
        Vector3    fromPos = transform.position;
        float      floorY  = FindFloorY();
        Vector3    toPos   = new Vector3(fromPos.x, floorY, fromPos.z);

        float elapsed = 0f;
        while (elapsed < tiltDuration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / tiltDuration);
            transform.rotation = Quaternion.Slerp(fromRot, _savedUprightRotation, t);
            transform.position = Vector3.Lerp(fromPos, toPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = _savedUprightRotation;
        transform.position = toPos;
        _fallen = false;

        // Restore physics for player
        if (_rb != null)
        {
            _rb.isKinematic    = _wasKinematic;
            _rb.linearVelocity = Vector3.zero;
        }
        if (_movement != null)
            _movement.enabled = true;

        Debug.Log($"[FallAnchor] '{name}': restored upright.");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private float FindFloorY()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f,
                            groundMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[FallAnchor] '{name}': floor='{hit.collider.name}' Y={hit.point.y:F2}");
            return hit.point.y;
        }
        Debug.LogWarning($"[FallAnchor] '{name}': no floor found, using current Y.");
        return transform.position.y;
    }
}
