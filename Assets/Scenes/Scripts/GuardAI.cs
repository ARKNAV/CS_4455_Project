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
    [Header("Patrol")]
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float walkSpeed = 2f;

    [Header("Hearing")]
    [SerializeField] float baseHearingRadius = 12f;

    [Header("Investigate")]
    [SerializeField] float investigateWaitTime = 2f;

    [Header("Vision")]
    [SerializeField] Transform player;
    [SerializeField] float viewDistance = 10f;
    [SerializeField] float fovDegrees = 90f;
    [SerializeField] LayerMask visionBlockers = Physics.DefaultRaycastLayers;

    [Header("Chase")]
    [SerializeField] float runSpeed = 5f;
    [SerializeField] float catchDistance = 1.5f;

    private NavMeshAgent _agent;
    private GuardState _state = GuardState.Patrol;
    private int _patrolIndex;
    private bool _patrolForward = true;
    private Vector3 _investigateTarget;
    private float _investigateWaitUntil;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
            _agent = gameObject.AddComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        EventManager.AddListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<NoiseEmittedEvent, Vector3, float>(OnNoiseHeard);
    }

    void Start()
    {
        _agent.speed = walkSpeed;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            _patrolIndex = 0;
            SetDestination(patrolPoints[0].position);
        }
    }

    void Update()
    {
        if (player == null) return;

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

        if (CanSeePlayer())
        {
            EvaluateDisguiseOrSuspicion();
            _state = GuardState.Chase;
            _agent.speed = runSpeed;
            SetDestination(player.position);
        }
    }

    private void OnNoiseHeard(Vector3 position, float noiseRadius)
    {
        if (_state == GuardState.Chase) return;
        float dist = Vector3.Distance(transform.position, position);
        if (dist > baseHearingRadius || dist > noiseRadius) return;
        _investigateTarget = position;
        _state = GuardState.Investigate;
        _agent.speed = walkSpeed;
        SetDestination(position);
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
            GameManager.TriggerLose();
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        if (dist > viewDistance) return false;
        toPlayer.Normalize();
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fovDegrees * 0.5f) return false;
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 dir = (player.position + Vector3.up * 1f - origin).normalized;
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, viewDistance, visionBlockers, QueryTriggerInteraction.Ignore))
            return false;
        Transform t = hit.collider.transform;
        while (t != null)
        {
            if (t == player) return true;
            t = t.parent;
        }
        return false;
    }

    private void EvaluateDisguiseOrSuspicion()
    {
    }

    private void SetDestination(Vector3 worldPosition)
    {
        if (_agent != null && _agent.isOnNavMesh)
            _agent.SetDestination(worldPosition);
    }

    private bool CanQueryAgentPath()
    {
        return _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }
}
