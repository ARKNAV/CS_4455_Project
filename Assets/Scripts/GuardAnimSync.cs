using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Keeps guard locomotion animation in sync with NavMeshAgent velocity.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class GuardAnimSync : MonoBehaviour
{
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float damping = 8f;

    private Animator _animator;
    private NavMeshAgent _agent;
    private float _smoothedSpeed;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (_animator == null || _agent == null)
            return;

        float targetSpeed = _agent.velocity.magnitude;
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, targetSpeed, Time.deltaTime * damping);
        _animator.SetFloat(speedParam, _smoothedSpeed);
    }
}
