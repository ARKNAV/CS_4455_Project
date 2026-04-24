using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the player. Detects nearby guards and performs a takedown
/// when the player presses the takedown key (F by default).
/// Both the player and guard play synchronized takedown animations.
/// </summary>
public class TakedownSystem : MonoBehaviour
{
    [Header("Takedown Settings")]
    [Tooltip("Max distance to initiate a takedown (measured from player to guard).")]
    [SerializeField] private float takedownRange = 3.0f;

    [Tooltip("Half-angle of the cone in front of the player where takedowns are allowed.")]
    [SerializeField] private float takedownHalfAngle = 100f;

    [Tooltip("Fallback duration if the clip length cannot be read from the Animator.")]
    [SerializeField] private float takedownDurationFallback = 9.9f;

    [Tooltip("How far behind the guard the player snaps to during takedown.")]
    [SerializeField] private float snapBehindDistance = 0.6f;

    [Tooltip("Seconds to lerp the player into the snap position before animation fires.")]
    [SerializeField] private float snapDuration = 0.22f;

    [Tooltip("Local-space offset applied to the player's snap position to correct for animation lateral drift.")]
    [SerializeField] private Vector3 playerSnapOffset = Vector3.zero;

    [Tooltip("Local-space offset applied to the guard's snap position to correct for animation lateral drift.")]
    [SerializeField] private Vector3 guardSnapOffset = Vector3.zero;

    [Header("References")]
    [SerializeField] private Animator playerAnimator;

    // ── Animator parameter hashes ──────────────────────────────────────────
    private static readonly int TakedownHash = Animator.StringToHash("Takedown");
    private static readonly int SpeedHash    = Animator.StringToHash("speed");
    private static readonly int MoveXHash    = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash    = Animator.StringToHash("MoveY");
    private static readonly int CrouchHash   = Animator.StringToHash("isCrouching");
    private static readonly int SprintHash   = Animator.StringToHash("isSprinting");
    private static readonly int PeekDirHash  = Animator.StringToHash("PeekDirection");
    private static readonly int IsPeekHash   = Animator.StringToHash("IsPeeking");

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
        // ClaimTakedown() atomically marks the guard as taken down.
        // If the guard's CatchPlayerRoutine already claimed it on the same frame,
        // this returns false and we skip — the guard's sequence takes priority.
        if (!bestGuard.ClaimTakedown()) return;
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

        CharacterInputController inputController = GetComponent<CharacterInputController>();
        BasicControlScript controlScript = GetComponent<BasicControlScript>();
        Rigidbody rb = GetComponent<Rigidbody>();

        // ── Freeze player physics and input ──────────────────────────────
        if (inputController != null) inputController.enabled = false;
        if (controlScript   != null) controlScript.enabled   = false;

        if (rb != null)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }

        // faceDir = direction from player (attacker) toward guard (victim)
        Vector3 toGuard = (guard.transform.position - transform.position);
        toGuard.y = 0f;
        Vector3 faceDir  = toGuard.sqrMagnitude > 0.001f ? toGuard.normalized : transform.forward;
        Quaternion facingRot = Quaternion.LookRotation(faceDir, Vector3.up);

        // Rotate guard (victim) to face the same direction as the player (away from player)
        guard.transform.rotation = facingRot;

        // Player (attacker) snaps snapBehindDistance behind the guard (victim).
        // playerSnapOffset is a local-space fine-tune (x=lateral, y=height, z=depth).
        Vector3 snapPosition = guard.transform.position - faceDir * snapBehindDistance;
        snapPosition.y += playerSnapOffset.y;
        snapPosition   += facingRot * new Vector3(playerSnapOffset.x, 0f, playerSnapOffset.z);
        Quaternion targetRot = facingRot;

        // ── Smooth position/rotation lerp (SmoothStep) ───────────────────
        float elapsed     = 0f;
        Vector3    startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / snapDuration);
            // SmoothStep gives an ease-in/ease-out feel
            float ts = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos,  snapPosition, ts);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, ts);
            yield return null;
        }

        transform.position = snapPosition;
        transform.rotation = targetRot;

        // ── Zero blend params for a clean idle entry into Takedown ────────
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat(SpeedHash,   0f);
            playerAnimator.SetFloat(MoveXHash,   0f);
            playerAnimator.SetFloat(MoveYHash,   0f);
            playerAnimator.SetBool(CrouchHash,   false);
            playerAnimator.SetBool(SprintHash,   false);
            playerAnimator.SetInteger(PeekDirHash, 0);
            playerAnimator.SetBool(IsPeekHash,   false);
            // Root motion OFF on both — blocked by physics components anyway.
            playerAnimator.applyRootMotion = false;
        }

        // ── Tell the guard to start its animation and get the duration ────
        // We derive duration from the actual clip length so swapping
        // the animation never causes a desync.
        float actualDuration = takedownDurationFallback;

        // Fire the trigger on the player and read back the clip length
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(TakedownHash);

            // Wait one frame so the transition has started
            yield return null;

            // The AnyState→Takedown transition takes 0.1 s, so after one frame we are
            // usually still in the transition. Check both current and next state.
            bool inTransition = playerAnimator.IsInTransition(0);
            AnimatorStateInfo stateInfo = inTransition
                ? playerAnimator.GetNextAnimatorStateInfo(0)
                : playerAnimator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfos = inTransition
                ? playerAnimator.GetNextAnimatorClipInfo(0)
                : playerAnimator.GetCurrentAnimatorClipInfo(0);

            if (clipInfos.Length > 0 && clipInfos[0].clip != null)
            {
                actualDuration = clipInfos[0].clip.length;
                Debug.Log($"[TakedownSystem] Clip '{clipInfos[0].clip.name}' is {actualDuration:F2}s");
            }
        }

        // Tell guard to animate for the same duration
        guard.OnTakedown(actualDuration);

        TakedownDiagnostic diag = GetComponent<TakedownDiagnostic>();
        if (diag != null) diag.StartLogging(guard);

        if (DemoObjectiveManager.Instance != null)
            DemoObjectiveManager.Instance.CompleteObjective("takedown");

        // Wait for the full clip to finish
        yield return new WaitForSeconds(actualDuration);

        // ── Restore player ────────────────────────────────────────────────
        if (playerAnimator != null) playerAnimator.applyRootMotion = true;
        if (rb != null) rb.isKinematic = false;
        if (inputController != null) inputController.enabled = true;
        if (controlScript   != null) controlScript.enabled   = true;

        isTakingDown = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, takedownRange);

        // Draw the takedown cone
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Vector3 fwd = transform.forward;
        float rad = takedownHalfAngle * Mathf.Deg2Rad;
        Vector3 leftEdge  = Quaternion.AngleAxis(-takedownHalfAngle, Vector3.up) * fwd * takedownRange;
        Vector3 rightEdge = Quaternion.AngleAxis( takedownHalfAngle, Vector3.up) * fwd * takedownRange;
        Gizmos.DrawRay(transform.position, leftEdge);
        Gizmos.DrawRay(transform.position, rightEdge);
    }
}
