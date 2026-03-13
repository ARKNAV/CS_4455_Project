using UnityEngine;

/// <summary>
/// A disguise box that the player can interact with to change into a specific outfit.
/// Each box grants a specific security clearance tier.
/// When the player enters the trigger zone, a UI prompt appears ONLY if the player
/// is not already disguised at the same or higher clearance level.
/// Pressing F triggers the disguise change with a Sims-style animation.
/// (E is reserved for the peek system.)
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DisguiseBox : MonoBehaviour
{
    [Header("Disguise Settings")]
    [Tooltip("Name of this disguise")]
    public string disguiseName = "Bodyguard Uniform";

    [Tooltip("Security clearance this disguise grants")]
    public SecurityClearance grantedClearance = SecurityClearance.Guard02;

    [Tooltip("Outfit data with per-slot materials for this disguise")]
    public DisguiseOutfit disguiseOutfit;

    [Tooltip("Whether this box has been used (one-time use)")]
    public bool isUsed = false;

    [Header("Visual")]
    [Tooltip("Color of the interaction glow")]
    public Color glowColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    private bool _playerInRange = false;
    private DisguiseSystem _playerDisguiseSystem;
    private DisguiseUIPrompt _uiPrompt;
    private Renderer _boxRenderer;

    void Start()
    {
        // Ensure we have a trigger collider for detection
        BoxCollider triggerCollider = null;
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }

        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
            triggerCollider.size = new Vector3(3f, 3f, 3f);
            triggerCollider.center = Vector3.up * 0.5f;
        }

        _boxRenderer = GetComponentInChildren<Renderer>();

        // Find the UI prompt in the scene
        _uiPrompt = FindAnyObjectByType<DisguiseUIPrompt>();
    }

    void Update()
    {
        if (!_playerInRange || isUsed || _playerDisguiseSystem == null) return;
        if (_playerDisguiseSystem.IsChanging || !CanPlayerUseThisBox()) return;

        bool interactPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
            interactPressed = UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame;
#else
        interactPressed = Input.GetKeyDown(KeyCode.F);
#endif

        if (interactPressed)
        {
            UseDisguise();
        }
    }

    /// <summary>
    /// Check if the player can benefit from this disguise box.
    /// Returns true if the player is not disguised, or if this box grants
    /// a higher clearance than the player's current disguise, or if the
    /// player's same-tier disguise integrity is low.
    /// </summary>
    private bool CanPlayerUseThisBox()
    {
        if (_playerDisguiseSystem == null) return false;

        // Player is not disguised — can always use
        if (!_playerDisguiseSystem.IsDisguised) return true;

        // Player is disguised but at a lower clearance — can upgrade
        if (_playerDisguiseSystem.CurrentClearance < grantedClearance) return true;

        // Player's disguise integrity is low — can refresh with same-tier box
        if (_playerDisguiseSystem.CurrentClearance == grantedClearance &&
            _playerDisguiseSystem.IntegrityNormalized < 0.5f) return true;

        return false;
    }

    private void UseDisguise()
    {
        if (isUsed || _playerDisguiseSystem == null) return;

        isUsed = true;
        _playerInRange = false;

        // Hide the prompt
        if (_uiPrompt != null)
            _uiPrompt.HidePrompt(this);

        // Apply the disguise using outfit materials on player torso elements only.
        if (disguiseOutfit == null)
        {
            Debug.LogWarning($"DisguiseBox '{name}' has no DisguiseOutfit assigned.");
            isUsed = false;
            _playerInRange = true;
            return;
        }

        _playerDisguiseSystem.ApplyDisguise(grantedClearance, disguiseOutfit);

        // Visually mark the box as used (open/empty look)
        if (_boxRenderer != null)
        {
            Material usedMat = new Material(_boxRenderer.sharedMaterial);
            usedMat.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            _boxRenderer.material = usedMat;
        }

        // Disable the lid child if it exists
        Transform lid = transform.Find("Lid");
        if (lid != null)
        {
            lid.gameObject.SetActive(false);
        }
    }

    private string BuildPromptText()
    {
        string clearanceLabel = GetGuardLabel(grantedClearance);
        return $"Press [F] to put on {disguiseName}\n<size=11>Grants {clearanceLabel} clearance</size>";
    }

    private static string GetGuardLabel(SecurityClearance clearance)
    {
        switch (clearance)
        {
            case SecurityClearance.Guard01: return "Guard 01";
            case SecurityClearance.Guard02: return "Guard 02";
            case SecurityClearance.Guard03: return "Guard 03";
            case SecurityClearance.Guard04: return "Guard 04";
            default: return "None";
        }
    }

    private static DisguiseSystem TryGetPlayerDisguiseSystem(Collider other)
    {
        DisguiseSystem ds = other.GetComponent<DisguiseSystem>();
        if (ds == null) ds = other.GetComponentInParent<DisguiseSystem>();
        return ds;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isUsed) return;

        DisguiseSystem disguiseSystem = TryGetPlayerDisguiseSystem(other);

        if (disguiseSystem != null)
        {
            _playerInRange = true;
            _playerDisguiseSystem = disguiseSystem;

            if (!disguiseSystem.IsChanging && CanPlayerUseThisBox() && _uiPrompt != null)
            {
                _uiPrompt.ShowPrompt(BuildPromptText(), this);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (isUsed) return;

        DisguiseSystem disguiseSystem = TryGetPlayerDisguiseSystem(other);

        if (disguiseSystem != null)
        {
            if (disguiseSystem.IsChanging || !CanPlayerUseThisBox())
            {
                if (_uiPrompt != null)
                    _uiPrompt.HidePrompt(this);
            }
            else if (_playerInRange && !isUsed)
            {
                if (_uiPrompt != null)
                    _uiPrompt.ShowPrompt(BuildPromptText(), this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        DisguiseSystem disguiseSystem = TryGetPlayerDisguiseSystem(other);

        if (disguiseSystem != null)
        {
            _playerInRange = false;
            _playerDisguiseSystem = null;

            if (_uiPrompt != null)
                _uiPrompt.HidePrompt(this);
        }
    }
}
