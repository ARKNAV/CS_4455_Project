using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class FallGroundAnchor : MonoBehaviour
{
    [Tooltip("Seconds to lerp from standing to lying flat")]
    public float tiltDuration = 0.32f;

    [Tooltip("Ground detection layers")]
    public LayerMask groundMask = ~0;

    private Rigidbody         _rb;
    private BasicControlScript _movement;
    private NavMeshAgent      _agent;

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

    public void StartFall()
    {
        if (_fallen) return;
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(DoFall());
    }

    public void RestoreUpright(Action onComplete = null)
    {
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(DoRestore(onComplete));
    }

    private IEnumerator DoFall()
    {
        _fallen = true;
        _savedUprightRotation = transform.rotation;

        float floorY = FindFloorY();

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

        if (_rb != null)
        {
            _rb.isKinematic    = _wasKinematic;
            _rb.linearVelocity = Vector3.zero;
        }
        if (_movement != null)
            _movement.enabled = true;

        onComplete?.Invoke();
    }

    private float FindFloorY()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 5f,
                            groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }
        return transform.position.y;
    }
}
