using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum GuardState
{
    Patrol,
    Investigate,
    Chase,
    TakenDown,
    Tackled
}

public class GuardAI : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float walkSpeed = 1.5f;

    [Header("Hearing")]
    [SerializeField] float baseHearingRadius = 12f;
    [SerializeField] float faintNoiseSuspicion = 25f;
    [Range(0.3f, 0.9f)]
    [SerializeField] float strongHearingFraction = 0.6f;

    [Header("Investigate")]
    [SerializeField] float investigateWaitTime = 2f;

    [Header("Vision")]
    [SerializeField] Transform player;
    [SerializeField] float viewDistance = 10f;
    [SerializeField] float fovDegrees = 90f;
    [SerializeField] LayerMask visionBlockers = Physics.DefaultRaycastLayers;
    [SerializeField] float peripheralFovDegrees = 150f;
    [SerializeField] float fullSightSuspicionRate = 160f;
    [SerializeField] float glimpseSuspicionRate = 50f;

    [Header("Chase")]
    [SerializeField] float runSpeed = 4f;
    [SerializeField] float catchDistance = 1.5f;

    [Header("Recovery")]
    [Tooltip("Seconds before a knocked-out guard wakes up and resumes patrol")]
    [SerializeField] float recoveryTime = 15f;

    [Header("Disguise Loot")]
    [Tooltip("Disguise outfit this guard carries — player can loot it when guard is down")]
    public DisguiseOutfit guardOutfit;
    [Tooltip("Security clearance this guard's disguise grants")]
    public SecurityClearance guardClearance = SecurityClearance.Guard01;
    [Tooltip("Material to swap onto the torso when the guard's disguise is stripped")]
    public Material strippedTorsoMaterial;

    // ── Private state ──────────────────────────────────────────────────
    private NavMeshAgent _agent;
    private Animator     _animator;
    private GuardState   _state = GuardState.Patrol;
    private int          _patrolIndex;
    private bool         _patrolForward = true;
    private Vector3      _investigateTarget;
    private float        _investigateWaitUntil;
    private DisguiseSystem _disguiseSystem;
    private Vector3      _lastKnownPlayerPosition;
    private bool         _isObserving;
    private bool         _disguiseAvailable = true;

    // ── Public accessors ────────────────────────────────────────────────
    public GuardState      CurrentState      => _state;
    public bool            IsIncapacitated   => _state == GuardState.TakenDown || _state == GuardState.Tackled;
    public bool            DisguiseAvailable => _disguiseAvailable && guardOutfit != null;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
            _agent = gameObject.AddComponent<NavMeshAgent>();

        _animator = GetComponentInChildren<Animator>();

        // NavMeshAgent owns position — root motion would fight it.
        if (_animator != null) _animator.applyRootMotion = false;
    }

    void OnEnable()
    {
        EventManager.AddListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
        SetObserving(false);
    }

    void Start()
    {
        if (!_agent.isOnNavMesh)
            Debug.LogWarning("GuardAI: NavMeshAgent is not on a NavMesh.", this);

        if (patrolPoints == null || patrolPoints.Length == 0)
            Debug.LogWarning("GuardAI: No patrol points assigned.", this);

        if (peripheralFovDegrees <= 0f) peripheralFovDegrees = 150f;
        if (fullSightSuspicionRate <= 0f) fullSightSuspicionRate = 160f;
        if (glimpseSuspicionRate <= 0f) glimpseSuspicionRate = 50f;
        if (faintNoiseSuspicion <= 0f) faintNoiseSuspicion = 25f;
        if (strongHearingFraction <= 0f) strongHearingFraction = 0.6f;

        _disguiseSystem = FindFirstObjectByType<DisguiseSystem>();
        _lastKnownPlayerPosition = transform.position;

        _agent.speed = walkSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            _patrolIndex = 0;
            SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (IsIncapacitated) return;

        switch (_state)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;
            case GuardState.Investigate:
                UpdateInvestigate();
                break;
            case GuardState.Chase:
                UpdateChase();
                break;
        }

        EvaluateDisguiseOrSuspicion();

        // Drive Speed parameter directly — works even when GuardAnimatorController
        // is on a different GameObject.
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            float spd = _agent.enabled ? _agent.velocity.magnitude : 0f;
            _animator.SetFloat("Speed", spd);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Takedown / Tackle
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GuardTakedownController when the player stealth-takedowns this guard.
    /// Guard faces away from the player so it falls in the same forward direction.
    /// </summary>
    public void TakeDown()
    {
        if (IsIncapacitated) return;
        _state = GuardState.TakenDown;

        // Stop NavMeshAgent
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled   = false;
        }

        // Face guard AWAY from player
        if (player != null)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        // Play KnockedOut animation and fall flat
        Animator anim = _animator ?? GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            if (anim.runtimeAnimatorController != null)
                anim.SetTrigger("KnockedOut");

            FallGroundAnchor anchor = anim.gameObject.GetComponent<FallGroundAnchor>();
            if (anchor == null) anchor = anim.gameObject.AddComponent<FallGroundAnchor>();
            anchor.StartFall();
        }

        Debug.Log($"[GuardAI] '{name}': KnockedOut — recovering in {recoveryTime}s.");
        StartCoroutine(RecoverAfterDelay(recoveryTime));
    }

    /// <summary>
    /// Called when the guard catches the player.
    /// </summary>
    private void TriggerTackle()
    {
        if (IsIncapacitated) return;
        _state = GuardState.Tackled;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = true;

        // Snap guard onto the player for visual contact
        if (player != null)
        {
            Vector3 dir = transform.position - player.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) dir.Normalize(); else dir = transform.forward;
            transform.position = player.position + dir * 0.25f;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }

        if (_animator != null && _animator.runtimeAnimatorController != null)
            _animator.SetTrigger("Tackle");

        // Notify player
        if (player != null)
        {
            GuardTakedownController tc = player.GetComponent<GuardTakedownController>()
                                      ?? player.GetComponentInParent<GuardTakedownController>();
            if (tc != null) tc.OnTackledByGuard(this);
            else GameManager.TriggerLose();
        }
        else GameManager.TriggerLose();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Disguise loot
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GuardDisguisePickup when the player takes this guard's disguise.
    /// Makes the guard's torso visually stripped and marks the outfit as taken.
    /// </summary>
    public void StripDisguise()
    {
        if (!_disguiseAvailable) return;
        _disguiseAvailable = false;

        // Grey out / hide the guard's torso renderers
        SkinnedMeshRenderer[] smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in smrs)
        {
            string n = smr.name.ToLower();
            if (n.Contains("torso") || n.Contains("shirt") || n.Contains("upper") || n.Contains("body"))
            {
                if (strippedTorsoMaterial != null)
                {
                    Material[] mats = smr.materials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = strippedTorsoMaterial;
                    smr.materials = mats;
                    Debug.Log($"[GuardAI] '{name}': torso '{smr.name}' stripped.");
                }
                else
                {
                    // No stripped material — dim the existing material instead
                    Material[] mats = smr.materials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = new Material(mats[i]) { color = new Color(0.3f, 0.3f, 0.3f, 1f) };
                    }
                    smr.materials = mats;
                    Debug.Log($"[GuardAI] '{name}': torso '{smr.name}' dimmed (no stripped material assigned).");
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Recovery
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator RecoverAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Debug.Log($"[GuardAI] '{name}': recovering from knockout.");

        // Restore upright rotation via FallGroundAnchor if it exists
        Animator anim = _animator ?? GetComponentInChildren<Animator>(true);
        FallGroundAnchor anchor = anim != null ? anim.gameObject.GetComponent<FallGroundAnchor>() : null;

        if (anchor != null && anchor.IsFallen)
        {
            anchor.RestoreUpright(OnRecoveryComplete);
        }
        else
        {
            OnRecoveryComplete();
        }
    }

    private void OnRecoveryComplete()
    {
        // Re-enable NavMeshAgent
        if (_agent != null)
        {
            _agent.enabled   = true;
            _agent.isStopped = false;
            _agent.speed     = walkSpeed;
        }

        // Reset state
        _state = GuardState.Patrol;
        _patrolForward = true;

        // Play idle animation
        Animator anim = _animator ?? GetComponentInChildren<Animator>(true);
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetFloat("Speed", 0f);

        // Resume patrol from nearest point
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            float best = float.MaxValue;
            int   idx  = 0;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float d = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (d < best) { best = d; idx = i; }
            }
            _patrolIndex = idx;
            SetDestination(patrolPoints[idx].position);
        }

        Debug.Log($"[GuardAI] '{name}': resumed patrol.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Patrol / Investigate / Chase
    // ─────────────────────────────────────────────────────────────────────

    private void OnNoiseHeard(Vector3 position, float noiseRadius)
    {
        if (_state == GuardState.Chase) return;

        float dist = Vector3.Distance(transform.position, position);
        if (dist > baseHearingRadius || dist > noiseRadius) return;

        _lastKnownPlayerPosition = position;
        float strongRadius = baseHearingRadius * strongHearingFraction;

        if (dist <= strongRadius)
        {
            _investigateTarget = position;
            _state = GuardState.Investigate;
            _agent.speed = walkSpeed;
            SetDestination(position);
        }
        else if (_disguiseSystem != null)
        {
            _disguiseSystem.AddSuspicion(faintNoiseSuspicion, "Faint noise");
        }
        else
        {
            _investigateTarget = position;
            _state = GuardState.Investigate;
            _agent.speed = walkSpeed;
            SetDestination(position);
        }
    }

    private void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        if (!CanQueryAgentPath()) return;

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (_patrolForward)
            {
                _patrolIndex++;
                if (_patrolIndex >= patrolPoints.Length)
                {
                    _patrolIndex = Mathf.Max(0, patrolPoints.Length - 2);
                    _patrolForward = false;
                }
            }
            else
            {
                _patrolIndex--;
                if (_patrolIndex < 0)
                {
                    _patrolIndex = Mathf.Min(1, patrolPoints.Length - 1);
                    _patrolForward = true;
                }
            }
            if (_patrolIndex >= 0 && _patrolIndex < patrolPoints.Length && patrolPoints[_patrolIndex] != null)
                SetDestination(patrolPoints[_patrolIndex].position);
        }
    }

    private void UpdateInvestigate()
    {
        if (!CanQueryAgentPath()) return;

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            if (_investigateWaitUntil <= 0f)
                _investigateWaitUntil = Time.time + investigateWaitTime;
            if (Time.time >= _investigateWaitUntil)
            {
                _investigateWaitUntil = 0f;
                _state = GuardState.Patrol;
                _agent.speed = walkSpeed;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    float best = float.MaxValue;
                    int idx = 0;
                    for (int i = 0; i < patrolPoints.Length; i++)
                    {
                        if (patrolPoints[i] == null) continue;
                        float d = Vector3.Distance(transform.position, patrolPoints[i].position);
                        if (d < best)
                        {
                            best = d;
                            idx = i;
                        }
                    }
                    _patrolIndex = idx;
                    _patrolForward = true;
                    SetDestination(patrolPoints[idx].position);
                }
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null) return;
        SetDestination(player.position);
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= catchDistance)
            TriggerTackle();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Vision / Suspicion (restored from main)
    // ─────────────────────────────────────────────────────────────────────

    private int GetPlayerVisionLevel()
    {
        if (player == null) return 0;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return 0;

        toPlayer.Normalize();
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > peripheralFovDegrees * 0.5f) return 0;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 dir = (player.position + Vector3.up * 1f - origin).normalized;
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, viewDistance, visionBlockers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsChildOf(hit.collider.transform, transform)) continue;

            if (IsChildOf(hit.collider.transform, player))
                return angle <= fovDegrees * 0.5f ? 2 : 1;

            return 0;
        }
        return 0;
    }

    private void EvaluateDisguiseOrSuspicion()
    {
        int visionLevel = GetPlayerVisionLevel();
        bool isDisguised = _disguiseSystem != null && _disguiseSystem.IsDisguised;

        if (player != null && visionLevel >= 1)
            _lastKnownPlayerPosition = player.position;

        SetObserving(visionLevel > 0 && !isDisguised);

        if (_disguiseSystem == null)
        {
            if (visionLevel >= 2)
            {
                if (_state != GuardState.Chase)
                {
                    _state = GuardState.Chase;
                    _agent.speed = runSpeed;
                }
                if (player != null) SetDestination(player.position);
            }
            return;
        }

        if (isDisguised)
        {
            if (_state == GuardState.Chase && visionLevel >= 1)
                ReturnToPatrol();
            return;
        }

        if (visionLevel == 2)
            _disguiseSystem.AddSuspicion(fullSightSuspicionRate * Time.deltaTime, "Direct sight");
        else if (visionLevel == 1)
            _disguiseSystem.AddSuspicion(glimpseSuspicionRate * Time.deltaTime, "Peripheral glimpse");

        float suspicion = _disguiseSystem.SuspicionNormalized;

        if (suspicion >= 0.8f)
        {
            if (_state != GuardState.Chase)
            {
                _state = GuardState.Chase;
                _agent.speed = runSpeed;
            }
            SetDestination(visionLevel >= 1 && player != null ? player.position : _lastKnownPlayerPosition);
        }
        else if (suspicion >= 0.2f)
        {
            if (_state == GuardState.Patrol)
            {
                _state = GuardState.Investigate;
                _investigateTarget = _lastKnownPlayerPosition;
                _agent.speed = walkSpeed;
                SetDestination(_investigateTarget);
            }
            else if (_state == GuardState.Chase)
            {
                _state = GuardState.Investigate;
                _investigateTarget = _lastKnownPlayerPosition;
                _agent.speed = walkSpeed;
                SetDestination(_investigateTarget);
            }
        }
        else if (_state == GuardState.Chase)
        {
            _state = GuardState.Investigate;
            _investigateTarget = _lastKnownPlayerPosition;
            _agent.speed = walkSpeed;
            SetDestination(_investigateTarget);
        }
    }

    private void SetObserving(bool observing)
    {
        if (_disguiseSystem == null) return;
        if (observing && !_isObserving)
        {
            _disguiseSystem.AddObserver();
            _isObserving = true;
        }
        else if (!observing && _isObserving)
        {
            _disguiseSystem.RemoveObserver();
            _isObserving = false;
        }
    }

    private void ReturnToPatrol()
    {
        _state = GuardState.Patrol;
        _agent.speed = walkSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            float best = float.MaxValue;
            int idx = 0;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                float d = Vector3.Distance(transform.position, patrolPoints[i].position);
                if (d < best)
                {
                    best = d;
                    idx = i;
                }
            }
            _patrolIndex = idx;
            _patrolForward = true;
            SetDestination(patrolPoints[idx].position);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private void SetDestination(Vector3 worldPosition)
    {
        if (_agent != null && _agent.isOnNavMesh)
            _agent.SetDestination(worldPosition);
    }

    private bool CanQueryAgentPath()
    {
        return _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }

    private static bool IsChildOf(Transform child, Transform parent)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == parent) return true;
            t = t.parent;
        }
        return false;
    }
}
