using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GuardDisguisePickup : MonoBehaviour
{
    [Tooltip("How close the player must be to loot a downed guard")]
    public float pickupRange = 2.0f;

    [Tooltip("Prompt text shown when a lootable guard is nearby")]
    public string promptText = "[F] Take disguise";

    private DisguiseSystem   _disguise;
    private DisguiseUIPrompt _uiPrompt;
    private GuardAI          _nearestLootable;

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

        GuardAI found = null;
        float   best  = pickupRange;

        foreach (GuardAI g in FindObjectsByType<GuardAI>(FindObjectsSortMode.None))
        {
            if (!g.IsIncapacitated || !g.DisguiseAvailable) continue;
            float d = Vector3.Distance(transform.position, g.transform.position);
            if (d < best) { best = d; found = g; }
        }

        if (found != _nearestLootable)
        {
            if (_uiPrompt != null && _nearestLootable != null)
                _uiPrompt.HidePrompt(null);

            _nearestLootable = found;

            if (_uiPrompt != null && found != null)
                _uiPrompt.ShowPrompt(promptText, null);
        }

        if (_nearestLootable == null) return;

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

        guard.StripDisguise();

        if (_disguise != null && outfit != null)
        {
            _disguise.ApplyDisguise(level, outfit);
            if (DemoObjectiveManager.Instance != null)
                DemoObjectiveManager.Instance.CompleteObjective("disguise");
        }

        if (_uiPrompt != null)
            _uiPrompt.HidePrompt(null);
        _nearestLootable = null;
    }
}
