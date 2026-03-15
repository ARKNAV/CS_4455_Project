using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player-initiated guard takedowns and the guard-tackle reaction.
/// Attach to the player root.
///
/// Takedown (F key):
///   Player snaps onto the guard's position and BOTH fall flat simultaneously —
///   the player lands face-down ON TOP of the guard.
///
/// Tackled (guard catches player):
///   Guard is already at the player's position. Player falls face-down away from
///   the guard direction.
/// </summary>
public class GuardTakedownController : MonoBehaviour
{
    [Header("Detection")]
    public float takedownRange = 2.2f;

    [Header("Animation Triggers — Player Animator")]
    public string playerTakedownTrigger = "Takedown";
    public string playerTackledTrigger  = "Tackled";

    [Header("Timing")]
    public float takedownDuration = 1.8f;
    public float tackleDuration   = 1.4f;

    // ── Internal ──────────────────────────────────────────────────────────
    private Animator               _animator;
    private CharacterInputController _input;
    private bool                   _busy;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        _input = GetComponent<CharacterInputController>();
    }

    void Update()
    {
        if (_busy) return;
        if (_input == null || !_input.Interact) return;
        TryTakedown();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Player-initiated takedown
    // ─────────────────────────────────────────────────────────────────────

    private void TryTakedown()
    {
        GuardAI target = FindNearestNonIncapacitatedGuard();
        if (target == null) return;
        if (target.CurrentState == GuardState.Chase) return;

        _busy = true;

        // ── 1. Snap player ONTO the guard (player lands on top) ───────────
        Vector3 guardPos   = target.transform.position;
        Vector3 playerDest = guardPos + target.transform.forward * 0.05f;
        playerDest.y       = transform.position.y;
        transform.position = playerDest;

        // Face same direction as the guard (player bears down from behind)
        transform.rotation = Quaternion.LookRotation(target.transform.forward, Vector3.up);
        Debug.Log($"[TakedownCtrl] Snapped player onto guard '{target.name}'");

        // ── 2. Play animation + fall flat (player lands ON guard) ─────────
        TriggerAnim(playerTakedownTrigger);
        var playerAnchor = GetOrAddAnchor(gameObject);
        playerAnchor.StartFall();

        // ── 3. Guard also falls flat ───────────────────────────────────────
        target.TakeDown();

        // ── 4. Objective ──────────────────────────────────────────────────
        if (DemoObjectiveManager.Instance != null)
            DemoObjectiveManager.Instance.CompleteObjective("takedown");

        StartCoroutine(FinishAfter(takedownDuration));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Guard-initiated tackle
    // ─────────────────────────────────────────────────────────────────────

    public void OnTackledByGuard(GuardAI guard)
    {
        if (_busy) return;
        _busy = true;

        // Face player AWAY from the guard so they fall forward (away from guard)
        if (guard != null)
        {
            Vector3 awayFromGuard = transform.position - guard.transform.position;
            awayFromGuard.y = 0f;
            if (awayFromGuard.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(awayFromGuard.normalized, Vector3.up);
        }

        Debug.Log("[TakedownCtrl] Player tackled — falling flat.");
        TriggerAnim(playerTackledTrigger);

        var playerAnchor = GetOrAddAnchor(gameObject);
        playerAnchor.StartFall();

        StartCoroutine(FinishTackle());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    private GuardAI FindNearestNonIncapacitatedGuard()
    {
        Collider[] hits    = Physics.OverlapSphere(transform.position, takedownRange);
        GuardAI    best    = null;
        float      bestDist = float.MaxValue;

        foreach (var col in hits)
        {
            GuardAI g = col.GetComponent<GuardAI>() ?? col.GetComponentInParent<GuardAI>();
            if (g == null || g.IsIncapacitated) continue;
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d < bestDist) { bestDist = d; best = g; }
        }
        return best;
    }

    private void TriggerAnim(string trigger)
    {
        if (_animator != null && _animator.runtimeAnimatorController != null
            && !string.IsNullOrEmpty(trigger))
            _animator.SetTrigger(trigger);
    }

    private static FallGroundAnchor GetOrAddAnchor(GameObject go)
    {
        var a = go.GetComponent<FallGroundAnchor>();
        return a != null ? a : go.AddComponent<FallGroundAnchor>();
    }

    private IEnumerator FinishAfter(float dur)
    {
        yield return new WaitForSeconds(dur);
        // Restore player upright after takedown animation finishes
        var anchor = GetComponent<FallGroundAnchor>();
        if (anchor != null && anchor.IsFallen)
            anchor.RestoreUpright();
        _busy = false;
    }

    private IEnumerator FinishTackle()
    {
        yield return new WaitForSeconds(tackleDuration);
        _busy = false;
        GameManager.TriggerLose();
    }
}
