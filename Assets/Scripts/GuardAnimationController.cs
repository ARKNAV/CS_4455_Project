using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(Animator))]
public class GuardAnimatorController : MonoBehaviour
{
    [SerializeField] private string speedParam = "Speed";

    private NavMeshAgent _agent;
    private Animator     _animator;
    private GuardAI      _guardAI;
    private int          _speedHash;

    void Awake()
    {
        _agent    = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _guardAI  = GetComponent<GuardAI>();
        _speedHash = Animator.StringToHash(speedParam);
    }

    void Update()
    {
        // Don't overwrite animator params during a takedown
        if (_guardAI != null && _guardAI.IsBeingTakenDown) return;

        if (_agent == null || _animator == null) return;

        float speed = _agent.velocity.magnitude;
        _animator.SetFloat(_speedHash, speed);
    }
}
