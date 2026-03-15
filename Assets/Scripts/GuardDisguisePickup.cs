using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Allows the player to loot a downed guard's disguise by pressing [F] nearby.
///
/// Attach this to the player. It scans for incapacitated guards within
/// <pickupRange> each frame and shows a UI prompt when one is found.
/// On [F] press: calls DisguiseSystem.ApplyDisguise() and GuardAI.StripDisguise().
/// </summary>
public class GuardDisguisePickup : MonoBehaviour
{
    [Tooltip("How close the player must be to loot a downed guard")]
    public float pickupRange = 2.0f;

    [Tooltip("Prompt text shown when a lootable guard is nearby")]
    public string promptText = "[F] Take disguise";

    // ── Cached refs ────────────────────────────────────────────────────
    private DisguiseSystem   _disguise;
    private DisguiseUIPrompt _uiPrompt;
    private GuardAI          _nearestLootable;   // guard currently in range

    void Awake()
    {
        _disguise = GetComponent<DisguiseSystem>();
    }

    void Start()
    {
        _uiPrompt = FindAnyObjectByType<DisguiseUIPrompt>(FindObjectsInactive.Include);
    }

    void Update()
    {
        if (_disguise == null || _disguise.IsChanging) return;

        // Find the nearest downed guard with an available disguise
        GuardAI found = null;
        float   best  = pickupRange;

        foreach (GuardAI g in FindObjectsByType<GuardAI>(FindObjectsSortMode.None))
        {
            if (!g.IsIncapacitated || !g.DisguiseAvailable) continue;
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d < best) { best = d; found = g; }
        }

        // ── Prompt management ─────────────────────────────────────────
        if (found != _nearestLootable)
        {
            // Hide old prompt
            if (_uiPrompt != null && _nearestLootable != null)
                _uiPrompt.HidePrompt(null);

            _nearestLootable = found;

            if (_uiPrompt != null && found != null)
                _uiPrompt.ShowPrompt(promptText, null);
        }

        if (_nearestLootable == null) return;

        // ── Interact ──────────────────────────────────────────────────
        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            pressed = Keyboard.current.fKey.wasPressedThisFrame;
#else
        pressed = Input.GetKeyDown(KeyCode.F);
#endif

        if (pressed)
            LootDisguise(_nearestLootable);
    }

    private void LootDisguise(GuardAI guard)
    {
        if (guard == null || !guard.DisguiseAvailable) return;

        DisguiseOutfit outfit    = guard.guardOutfit;
        SecurityClearance level  = guard.guardClearance;

        // Strip the guard visually and mark outfit as taken
        guard.StripDisguise();

        // Apply to player
        if (_disguise != null && outfit != null)
        {
            _disguise.ApplyDisguise(level, outfit);
            Debug.Log($"[GuardDisguise] Looted disguise '{outfit.name}' (clearance={level}) from guard '{guard.name}'.");

            // Complete the disguise objective if it's pending
            if (DemoObjectiveManager.Instance != null)
                DemoObjectiveManager.Instance.CompleteObjective("disguise");
        }
        else
        {
            Debug.LogWarning($"[GuardDisguise] Guard '{guard.name}' has no guardOutfit assigned.");
        }

        // Hide prompt
        if (_uiPrompt != null)
            _uiPrompt.HidePrompt(null);
        _nearestLootable = null;
    }
}
