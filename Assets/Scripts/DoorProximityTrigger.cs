using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class DoorProximityTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    public string openParameter = "character_nearby";
    public string playerTag = "Player";
    [Header("Interaction (Optional)")]
    public bool requireInteractionToOpen = false;
    public KeyCode interactKey = KeyCode.F;
    [Header("Objective Gate (Optional)")]
    public bool requireObjective = false;
    public string requiredObjectiveId = "exit";
    public bool completeObjectiveOnOpen = true;
    [Header("Access Control (Optional)")]
    public bool requireReaderUnlock = false;
    public KeycardReaderController requiredReader;
    public bool requireConsoleUnlock = false;

    private bool consoleUnlocked = false;
    private int playerOverlapCount = 0;

    private int openParameterHash;

    private void Awake()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }

        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInParent<Animator>();
        }

        openParameterHash = Animator.StringToHash(openParameter);
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerOverlapCount++;

        if (!requireInteractionToOpen)
        {
            TryOpenDoor();
        }
    }

    private void Update()
    {
        if (!requireInteractionToOpen)
        {
            return;
        }

        if (playerOverlapCount <= 0)
        {
            return;
        }

        if (WasKeyPressedThisFrame(interactKey))
        {
            TryOpenDoor();
        }
    }

    private bool TryOpenDoor()
    {
        if (doorAnimator == null)
        {
            return false;
        }

        if (requireObjective)
        {
            DemoObjectiveManager manager = DemoObjectiveManager.Instance;
            if (manager == null || !manager.IsCurrentObjective(requiredObjectiveId))
            {
                return false;
            }

            if (completeObjectiveOnOpen)
            {
                manager.CompleteObjective(requiredObjectiveId);
            }
        }

        if (requireReaderUnlock)
        {
            if (requiredReader == null || !requiredReader.IsUnlocked)
            {
                return false;
            }
        }

        if (requireConsoleUnlock && !consoleUnlocked)
        {
            return false;
        }

        if (!HasOpenParameter())
        {
            return false;
        }

        doorAnimator.SetBool(openParameterHash, true);
        return true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);

        if (doorAnimator == null)
        {
            return;
        }

        if (!HasOpenParameter())
        {
            return;
        }

        doorAnimator.SetBool(openParameterHash, false);
    }

    public void UnlockAccessFromConsole()
    {
        consoleUnlocked = true;

        // If the player is already in range when unlock occurs, open immediately.
        if (playerOverlapCount > 0 && !requireInteractionToOpen)
        {
            TryOpenDoor();
        }
    }

    public void DisableConsoleRequirement()
    {
        requireConsoleUnlock = false;
        consoleUnlocked = true;

        if (playerOverlapCount > 0 && !requireInteractionToOpen)
        {
            TryOpenDoor();
        }
    }

    public void ResetConsoleUnlock()
    {
        consoleUnlocked = false;
    }

    private bool HasOpenParameter()
    {
        foreach (AnimatorControllerParameter parameter in doorAnimator.parameters)
        {
            if (parameter.nameHash == openParameterHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        Debug.LogWarning($"DoorProximityTrigger: Animator on '{doorAnimator.gameObject.name}' does not contain bool parameter '{openParameter}'.", this);
        return false;
    }

    private bool WasKeyPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        switch (key)
        {
            case KeyCode.F:
                return Keyboard.current.fKey.wasPressedThisFrame;
            case KeyCode.E:
                return Keyboard.current.eKey.wasPressedThisFrame;
            case KeyCode.Q:
                return Keyboard.current.qKey.wasPressedThisFrame;
            case KeyCode.Return:
                return Keyboard.current.enterKey.wasPressedThisFrame;
            case KeyCode.KeypadEnter:
                return Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            case KeyCode.Escape:
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            default:
                return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }
}
