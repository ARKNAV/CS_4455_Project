using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum GuardState
{
    Patrol,
    Investigate,
    Chase
}

public class GuardAI : MonoBehaviour
{
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float walkSpeed = 1.5f;
    [SerializeField] float baseHearingRadius = 12f;
    [SerializeField] float investigateWaitTime = 2f;
    [SerializeField] Transform player;
    [SerializeField] float viewDistance = 10f;
    [SerializeField] float fovDegrees = 90f;
    [SerializeField] LayerMask visionBlockers = Physics.DefaultRaycastLayers;
    [SerializeField] float peripheralFovDegrees = 150f;
    [SerializeField] float fullSightSuspicionRate = 160f;
    [SerializeField] float glimpseSuspicionRate = 50f;
    [SerializeField] float faintNoiseSuspicion = 25f;
    [Range(0.3f, 0.9f)]
    [SerializeField] float strongHearingFraction = 0.6f;
    [SerializeField] float runSpeed = 4f;
    [SerializeField] float catchDistance = 1.5f;

    private NavMeshAgent _agent;
    private GuardState _state = GuardState.Patrol;
    private int _patrolIndex;
    private bool _patrolForward = true;
    private Vector3 _investigateTarget;
    private float _investigateWaitUntil;
    private DisguiseSystem _disguiseSystem;
    private Vector3 _lastKnownPlayerPosition;
    private bool _isObserving;
    private bool _isBeingTakenDown = false;
    private static bool _playerBeingCaught = false;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState() { _playerBeingCaught = false; }
    private bool _pinToGround = false;
    private float _pinnedY;
    private Animator _animator;

    private static readonly int TakedownHash       = Animator.StringToHash("Takedown");
    private static readonly int TakedownVictimHash = Animator.StringToHash("TakedownVictim");

    public bool IsBeingTakenDown => _isBeingTakenDown;

    /// <summary>
    /// Atomically claims this guard for a takedown. Returns true only if the caller
    /// is the first to claim it; false if another system already claimed it.
    /// This prevents the player-initiated takedown and the guard's CatchPlayerRoutine
    /// from both firing on the same frame.
    /// </summary>
    public bool ClaimTakedown()
    {
        if (_isBeingTakenDown) return false;
        _isBeingTakenDown = true;
        return true;
    }
    public bool IsChasing => _state == GuardState.Chase;
    public bool IsInvestigating => _state == GuardState.Investigate;
    public bool IsSuspicious => _state != GuardState.Patrol;

    void Awake()
    {
        _playerBeingCaught = false;
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
            _agent = gameObject.AddComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();

        if (GetComponent<GuardSuspicionMarker>() == null)
            gameObject.AddComponent<GuardSuspicionMarker>();
    }

    void OnEnable()
    {
        EventManager.AddListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
        EventManager.AddListener<GlobalChaseCascadeEvent>(OnGlobalChaseCascade);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
        EventManager.RemoveListener<GlobalChaseCascadeEvent>(OnGlobalChaseCascade);
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

    void LateUpdate()
    {
        if (_pinToGround)
        {
            Vector3 pos = transform.position;
            pos.y = _pinnedY;
            transform.position = pos;
        }
    }

    void Update()
    {
        if (_isBeingTakenDown) return;

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
    }

    private void OnNoiseHeard(Vector3 position, float noiseRadius)
    {
        if (_state == GuardState.Chase) return;

        bool isDisguised = _disguiseSystem != null && _disguiseSystem.IsDisguised;

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
            if (!isDisguised)
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
        {
            StartCoroutine(CatchPlayerRoutine());
        }
    }

    [Header("Catch / Tackle")]
    [Tooltip("Fallback clip duration if we cannot read it from the Animator.")]
    [SerializeField] private float catchTakedownDurationFallback = 9.9f;
    [Tooltip("How far behind the player the guard snaps (local Z offset from the shared anchor).")]
    [SerializeField] private float tackleSnapDistance = 0.7f;
    [Tooltip("Local-space offset applied to the guard's snap position to correct for animation lateral drift.")]
    [SerializeField] private Vector3 guardSnapOffset = Vector3.zero;
    [Tooltip("Local-space offset applied to the player's snap position to correct for animation lateral drift.")]
    [SerializeField] private Vector3 playerSnapOffset = Vector3.zero;

    private IEnumerator CatchPlayerRoutine()
    {
        // ClaimTakedown() atomically sets _isBeingTakenDown; if another system
        // already claimed this guard (e.g. player pressed F on the same frame),
        // we bail out immediately so only one takedown sequence runs.
        if (!ClaimTakedown()) yield break;

        // Only one guard can execute CatchPlayerRoutine at a time. A second guard
        // that reaches catchDistance on the same frame passes ClaimTakedown() on
        // itself but must yield here so the player isn't double-triggered.
        if (_playerBeingCaught) { _isBeingTakenDown = false; yield break; }
        _playerBeingCaught = true;

        // Stop all other guards chasing
        foreach (GuardAI g in FindObjectsByType<GuardAI>(FindObjectsSortMode.None))
            if (g != null && g != this) g.StopPursuit();

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
        if (_agent != null) _agent.enabled = false;
        SetObserving(false);

        if (player == null) { GameManager.TriggerLose(); yield break; }

        // ── Freeze player ──
        CharacterInputController inputCtrl = player.GetComponent<CharacterInputController>();
        BasicControlScript controlScript   = player.GetComponent<BasicControlScript>();
        Rigidbody playerRb                 = player.GetComponent<Rigidbody>();
        Animator  playerAnimator           = player.GetComponent<Animator>();

        if (inputCtrl    != null) inputCtrl.enabled    = false;
        if (controlScript != null) controlScript.enabled = false;
        if (playerRb != null)
        {
            if (!playerRb.isKinematic) { playerRb.linearVelocity = Vector3.zero; playerRb.angularVelocity = Vector3.zero; }
            playerRb.isKinematic = true;
        }

        // Zero blend params on both animators so we enter the catch animations from a clean idle pose
        ZeroAnimatorParams(playerAnimator);
        ZeroAnimatorParams(_animator);

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        Vector3 guardFwd = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
        Quaternion facingRot = Quaternion.LookRotation(guardFwd, Vector3.up);

        // Rotate player (victim) to face away from guard (same direction as guard faces)
        player.rotation = facingRot;

        // Guard (attacker) snaps tackleSnapDistance behind the player (victim).
        // guardSnapOffset is a local-space fine-tune (x=lateral, y=height, z=depth).
        // Empirical baseline: 0.4m behind places guard hips ~0.35m behind player hips.
        // Set guardSnapOffset.y negative if guard character is taller than player.
        Vector3 attackerPos = player.position - guardFwd * tackleSnapDistance;
        attackerPos.y      = player.position.y + guardSnapOffset.y;
        attackerPos        += facingRot * new Vector3(guardSnapOffset.x, 0f, guardSnapOffset.z);

        // Hide guard renderers during the snap to prevent phasing-through visual,
        // then teleport instantly and re-enable once animation has started.
        Renderer[] guardRenderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in guardRenderers) r.enabled = false;

        transform.position = attackerPos;
        transform.rotation = facingRot;

        // Root motion OFF on both — NavMeshAgent and Rigidbody block root motion anyway.
        if (_animator      != null) _animator.applyRootMotion      = false;
        if (playerAnimator != null) playerAnimator.applyRootMotion = false;

        yield return null;

        // Guard is attacker, player is victim — fire triggers then restore renderers
        if (_animator      != null) _animator.SetTrigger(TakedownHash);
        if (playerAnimator != null) playerAnimator.SetTrigger(TakedownVictimHash);

        // Re-enable guard renderers now that both are in position and animation is starting
        foreach (var r in guardRenderers) r.enabled = true;

        TakedownDiagnostic diag = player.GetComponent<TakedownDiagnostic>();
        if (diag != null) diag.StartLogging(this);

        yield return new WaitForSeconds(catchTakedownDurationFallback);

        GameManager.TriggerLose();
    }

    private static readonly System.Collections.Generic.HashSet<string> _zeroParamNames
        = new System.Collections.Generic.HashSet<string>
        { "speed", "Speed", "MoveX", "MoveY", "isCrouching", "isSprinting", "IsPeeking", "PeekDirection" };

    private static void ZeroAnimatorParams(Animator anim)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (var p in anim.parameters)
        {
            if (!_zeroParamNames.Contains(p.name)) continue;
            switch (p.type)
            {
                case AnimatorControllerParameterType.Float:   anim.SetFloat(p.nameHash, 0f);    break;
                case AnimatorControllerParameterType.Int:     anim.SetInteger(p.nameHash, 0);   break;
                case AnimatorControllerParameterType.Bool:    anim.SetBool(p.nameHash, false);  break;
            }
        }
    }

    public void StopPursuit()
    {
        if (_isBeingTakenDown) return;
        _state = GuardState.Patrol;
        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            _agent.ResetPath();
        _agent.speed = walkSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[_patrolIndex] != null)
            SetDestination(patrolPoints[_patrolIndex].position);
    }

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
                BeginChase(player != null ? player.position : _lastKnownPlayerPosition, fromCascade: false);
            return;
        }

        if (isDisguised)
        {
            if (_state == GuardState.Chase || _state == GuardState.Investigate)
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
            bool alreadyChasing = _state == GuardState.Chase;
            Vector3 target = (alreadyChasing || visionLevel >= 1) && player != null
                ? player.position
                : _lastKnownPlayerPosition;
            BeginChase(target, fromCascade: false);
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

    private void SetDestination(Vector3 worldPosition)
    {
        if (_agent != null && _agent.isOnNavMesh)
            _agent.SetDestination(worldPosition);
    }

    private void BeginChase(Vector3 target, bool fromCascade)
    {
        if (_isBeingTakenDown) return;
        if (!CanQueryAgentPath()) return;
        if (player == null) return;

        bool alreadyChasing = _state == GuardState.Chase;
        if (!alreadyChasing)
        {
            _state = GuardState.Chase;
            _agent.speed = runSpeed;
        }

        _lastKnownPlayerPosition = target;
        SetDestination(target);

        if (!alreadyChasing && !fromCascade)
            EventManager.TriggerEvent<GlobalChaseCascadeEvent>();
    }

    private void OnGlobalChaseCascade()
    {
        if (_isBeingTakenDown) return;
        if (player == null) return;
        if (_state == GuardState.Chase) return;
        BeginChase(player.position, fromCascade: true);
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

    public void OnTakedown(float duration)
    {
        // Caller must have already called ClaimTakedown() successfully.
        // _isBeingTakenDown is already true; just start the routine.
        StartCoroutine(TakedownRoutine(duration));
    }

    private IEnumerator TakedownRoutine(float duration)
    {
        _isBeingTakenDown = true;

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            _agent.ResetPath();
        if (_agent != null) _agent.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        SetObserving(false);

        ZeroAnimatorParams(_animator);

        if (_animator != null) _animator.applyRootMotion = false;

        _pinnedY = transform.position.y;
        _pinToGround = true;

        yield return null;

        if (_animator != null)
            _animator.SetTrigger(TakedownVictimHash);

        yield return new WaitForSeconds(Mathf.Max(0f, duration - Time.deltaTime));

        _pinToGround = false;

        gameObject.SetActive(false);
    }
}
