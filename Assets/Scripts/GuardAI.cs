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
    [Header("Disguise Access Check")]
    [SerializeField] SecurityClearance minimumTrustedClearance = SecurityClearance.Guard02;
    [SerializeField] bool enforceCurrentZoneClearance = true;

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
    private bool _pinToGround = false;
    private float _pinnedY;
    private Animator _animator;

    private static readonly int TakedownHash = Animator.StringToHash("Takedown");

    public bool IsBeingTakenDown => _isBeingTakenDown;
    public bool IsChasing => _state == GuardState.Chase;
    public bool IsInvestigating => _state == GuardState.Investigate;
    public bool IsSuspicious => _state != GuardState.Patrol;

    void Awake()
    {
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
        if (player == null)
        {
            DisguiseSystem playerDisguise = FindFirstObjectByType<DisguiseSystem>();
            if (playerDisguise != null)
                player = playerDisguise.transform;
            else
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }
        }

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

    private IEnumerator CatchPlayerRoutine()
    {
        if (_isBeingTakenDown) yield break;
        _isBeingTakenDown = true;

        GuardAI[] allGuards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        foreach (GuardAI g in allGuards)
        {
            if (g == this || g == null) continue;
            g.StopPursuit();
        }

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            _agent.ResetPath();
        if (_agent != null) _agent.enabled = false;

        SetObserving(false);

        if (_animator != null)
        {
            _animator.SetFloat("speed", 0f);
            _animator.SetFloat("Speed", 0f);
        }

        if (player != null)
        {
            Animator playerAnimator = player.GetComponent<Animator>();
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            CharacterInputController inputCtrl = player.GetComponent<CharacterInputController>();
            BasicControlScript controlScript = player.GetComponent<BasicControlScript>();

            if (inputCtrl != null) inputCtrl.enabled = false;
            if (controlScript != null) controlScript.enabled = false;

            if (playerRb != null)
            {
                playerRb.isKinematic = true;
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("speed", 0f);
                playerAnimator.SetFloat("MoveX", 0f);
                playerAnimator.SetFloat("MoveY", 0f);
                playerAnimator.SetBool("isCrouching", false);
                playerAnimator.SetBool("isSprinting", false);
                playerAnimator.SetInteger("PeekDirection", 0);
                playerAnimator.SetBool("IsPeeking", false);
            }

            yield return null;

            if (playerAnimator != null)
                playerAnimator.SetTrigger(TakedownHash);
        }

        GameManager.TriggerLose();
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
        bool hasTrustedClearance = _disguiseSystem != null &&
                                   _disguiseSystem.CurrentClearance >= minimumTrustedClearance;
        bool hasZoneClearance = _disguiseSystem != null &&
                                (!enforceCurrentZoneClearance ||
                                 _disguiseSystem.CurrentZone == null ||
                                 _disguiseSystem.CurrentClearance >= _disguiseSystem.CurrentZone.RequiredClearance);
        bool effectivelyDisguised = isDisguised && hasTrustedClearance && hasZoneClearance;

        if (player != null && visionLevel >= 1)
            _lastKnownPlayerPosition = player.position;

        SetObserving(visionLevel > 0 && !effectivelyDisguised);

        if (_disguiseSystem == null)
        {
            if (visionLevel >= 2)
                BeginChase(player != null ? player.position : _lastKnownPlayerPosition, fromCascade: false);
            return;
        }

        // Disguises only protect when they satisfy the current zone's clearance requirement.
        if (effectivelyDisguised)
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
        if (_isBeingTakenDown) return;
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
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetObserving(false);

        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(toPlayer.normalized);
        }

        if (_animator != null)
        {
            _animator.SetFloat("speed", 0f);
            _animator.SetFloat("Speed", 0f);
            _animator.SetBool("isCrouching", false);
            _animator.SetBool("isSprinting", false);
            _animator.SetInteger("PeekDirection", 0);
            _animator.SetBool("IsPeeking", false);
        }

        _pinnedY = transform.position.y;
        _pinToGround = true;

        yield return null;

        if (_animator != null)
            _animator.SetTrigger(TakedownHash);

        yield return new WaitForSeconds(duration);

        _pinToGround = false;

        gameObject.SetActive(false);
    }
}
