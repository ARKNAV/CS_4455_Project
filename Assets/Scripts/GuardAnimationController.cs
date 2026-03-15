using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(Animator))]
public class GuardAnimatorController : MonoBehaviour
{
    [SerializeField] string speedParam = "Speed";

    NavMeshAgent agent;
    Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParam, speed);
    }

    /// <summary>
    /// Receives the EmitFootstep animation event from shared walk/run clips.
    /// Guards don't play footstep audio, so this is intentionally a no-op.
    /// </summary>
    public void EmitFootstep() { }
}