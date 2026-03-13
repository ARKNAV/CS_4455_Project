using UnityEngine;

/// <summary>
/// Defines a security zone that requires a minimum clearance level to enter without
/// raising suspicion. Attach to a trigger collider that covers the zone area.
/// When the player enters with insufficient clearance, suspicion events are raised.
/// Also raises extra suspicion for behavioral violations (sprinting) in restricted zones.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SecurityZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Display name for this security zone")]
    public string zoneName = "Restricted Area";

    [Tooltip("Minimum clearance required to enter this zone without suspicion")]
    public SecurityClearance requiredClearance = SecurityClearance.Guard02;

    [Tooltip("How fast suspicion builds per second when player has wrong clearance")]
    public float suspicionRatePerSecond = 15f;

    [Tooltip("Extra suspicion per second when sprinting in this restricted zone")]
    public float sprintSuspicionRate = 20f;

    [Tooltip("If true, entering this zone with wrong clearance immediately triggers investigation")]
    public bool immediateInvestigation = false;

    private bool playerInZone = false;
    private DisguiseSystem playerDisguise;
    private CharacterInputController playerInput;

    /// <summary>Whether the player is currently inside this zone.</summary>
    public bool PlayerInZone => playerInZone;

    /// <summary>The clearance required for this zone.</summary>
    public SecurityClearance RequiredClearance => requiredClearance;

    /// <summary>The zone display name.</summary>
    public string ZoneName => zoneName;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInZone || playerDisguise == null) return;

        // Wrong clearance → continuous suspicion
        if (playerDisguise.CurrentClearance < requiredClearance)
        {
            float suspicionAmount = suspicionRatePerSecond * Time.deltaTime;
            EventManager.TriggerEvent<SuspicionChangedEvent, float, string>(
                suspicionAmount,
                $"Wrong clearance for {zoneName}");
        }

        // Sprinting in any non-civilian zone → extra suspicion (behavioral)
        if (requiredClearance > SecurityClearance.Civilian &&
            playerInput != null && playerInput.IsSprinting)
        {
            float sprintAmount = sprintSuspicionRate * Time.deltaTime;
            EventManager.TriggerEvent<SuspicionChangedEvent, float, string>(
                sprintAmount,
                $"Running in {zoneName}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        DisguiseSystem ds = other.GetComponent<DisguiseSystem>();
        if (ds == null) ds = other.GetComponentInParent<DisguiseSystem>();
        if (ds == null) return;

        playerInZone = true;
        playerDisguise = ds;
        playerInput = other.GetComponent<CharacterInputController>();
        if (playerInput == null)
            playerInput = other.GetComponentInParent<CharacterInputController>();

        ds.SetCurrentZone(this);

        // Check clearance on entry
        if (ds.CurrentClearance < requiredClearance)
        {
            EventManager.TriggerEvent<ZoneViolationEvent, SecurityClearance, string>(
                requiredClearance, zoneName);

            if (immediateInvestigation)
            {
                EventManager.TriggerEvent<SuspicionChangedEvent, float, string>(
                    30f, $"Unauthorized entry to {zoneName}");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        DisguiseSystem ds = other.GetComponent<DisguiseSystem>();
        if (ds == null) ds = other.GetComponentInParent<DisguiseSystem>();
        if (ds == null) return;

        playerInZone = false;
        playerDisguise = null;
        playerInput = null;
        ds.ClearCurrentZone(this);
    }
}
