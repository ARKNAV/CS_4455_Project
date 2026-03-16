using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the player. Detects nearby guards and performs a takedown
/// when the player presses the takedown key (F by default).
/// Only the guard plays the takedown animation and falls; the player remains standing.
/// </summary>
public class TakedownSystem : MonoBehaviour
{
    [Header("Takedown Settings")]
    [Tooltip("Max distance to initiate a takedown (measured from player to guard).")]
    [SerializeField] private float takedownRange = 3.0f;

    [Tooltip("Half-angle of the cone in front of the player where takedowns are allowed.")]
    [SerializeField] private float takedownHalfAngle = 100f;

    [Tooltip("Duration to lock controls (should match the Takedown clip length).")]
    [SerializeField] private float takedownDuration = 4.67f;

    [Header("References")]
    [SerializeField] private Animator playerAnimator;

    private bool isTakingDown = false;

    void Awake()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isTakingDown) return;

        bool takedownPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
            takedownPressed = UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
#else
        takedownPressed = Input.GetKeyDown(KeyCode.F);
#endif

        if (takedownPressed)
            TryTakedown();
    }

    private void TryTakedown()
    {
        GuardAI bestGuard = FindBestGuard();
        if (bestGuard == null)
        {
            Debug.Log("[TakedownSystem] No guard in range to take down.");
            return;
        }
        StartCoroutine(PerformTakedown(bestGuard));
    }

    private GuardAI FindBestGuard()
    {
        GuardAI[] guards = FindObjectsByType<GuardAI>(FindObjectsSortMode.None);
        GuardAI best = null;
        float bestDist = float.MaxValue;

        foreach (GuardAI guard in guards)
        {
            if (guard == null || guard.IsBeingTakenDown) continue;

            Vector3 toGuard = guard.transform.position - transform.position;
            float dist = toGuard.magnitude;

            if (dist > takedownRange) continue;

            toGuard.y = 0f;
            if (toGuard.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, toGuard.normalized);
                if (angle > takedownHalfAngle) continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                best = guard;
            }
        }

        return best;
    }

    private IEnumerator PerformTakedown(GuardAI guard)
    {
        isTakingDown = true;

        // --- Freeze player movement during the takedown ---
        CharacterInputController inputController = GetComponent<CharacterInputController>();
        BasicControlScript controlScript = GetComponent<BasicControlScript>();
        Rigidbody rb = GetComponent<Rigidbody>();

        if (inputController != null) inputController.enabled = false;
        if (controlScript != null) controlScript.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset player animator to idle (player does NOT play takedown animation)
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

        // --- Only the guard plays the takedown animation and falls ---
        guard.OnTakedown(takedownDuration);

        yield return new WaitForSeconds(takedownDuration);

        // --- Restore player ---
        if (rb != null)
            rb.isKinematic = false;

        if (inputController != null) inputController.enabled = true;
        if (controlScript != null) controlScript.enabled = true;

        isTakingDown = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, takedownRange);
    }
}
