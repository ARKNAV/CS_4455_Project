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
        float speed = agent.velocity.magnitude;
        Debug.Log("Speed: " + speed);
        animator.SetFloat(speedParam, speed);
    }
}