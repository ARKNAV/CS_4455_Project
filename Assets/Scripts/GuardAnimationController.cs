using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(Animator))]
public class GuardAnimatorController : MonoBehaviour
{
    [SerializeField] string speedParam = "speed";

    NavMeshAgent agent;
    Animator animator;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Don't overwrite animator params during a takedown
        GuardAI guardAI = GetComponent<GuardAI>();
        if (guardAI != null && guardAI.IsBeingTakenDown) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(speedParam, speed);
    }
}