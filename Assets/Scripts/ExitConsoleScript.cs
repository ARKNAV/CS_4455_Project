using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro;           // Required for InputField and Text
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ExitConsoleInteraction : MonoBehaviour
{
    public static bool IsConsoleInteractionActive { get; private set; }

    [Header("UI References (Assign in Inspector)")]
    public GameObject passcodeUIPanel; // The Panel we created
    public TMP_InputField inputField;  // The Input Field component
    public TextMeshProUGUI statusText; // The "ENTER ACCESS CODE" text, to update on error
    public Button submitButton;        // The Submit Button
    public CanvasGroup passcodeCanvasGroup;

    [Header("Interaction Settings")]
    public float interactionDistance = 3.0f; // How close player must be
    public KeyCode interactKey = KeyCode.F;   // The button to press
    public KeyCode cancelKey = KeyCode.Tab;
    public string interactionPromptText = "Press F to interact";

    [Header("Player Lock (While Console Open)")]
    public bool disablePlayerControlWhileOpen = true;

    [Header("Camera Pan (Optional)")]
    public bool useCameraPan = false;
    public Transform consoleCameraView;
    public float cameraPanDuration = 0.35f;
    public AnimationCurve cameraPanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI Visibility (Optional)")]
    public Canvas[] canvasesToHideWhileOpen;

    [Header("Level Settings")]
    public string correctPasscode = "8812";   // The code for Stage 2
    public bool preferGeneratedStabilitySignature = true;

    [Header("Door Unlock (Optional)")]
    public bool unlockDoorOnSuccess = true;
    public DoorProximityTrigger linkedDoorTrigger;
    public float autoFindDoorMaxDistance = 12f;

    private GameObject player;
    private bool isUIVisible = false;
    private bool promptVisible = false;
    private bool interactionBusy = false;
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Vector3 preInteractionCameraPosition;
    private Quaternion preInteractionCameraRotation;
    private bool hasPreInteractionCameraPose;
    private MonoBehaviour[] cachedPlayerComponents;
    private Behaviour[] cachedCameraControlComponents;
    private bool[] cachedCameraControlInitialStates;
    private bool[] cachedCanvasInitialStates;

    void Start()
    {
        // Find player (make sure player has the "Player" tag)
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }

        if (passcodeCanvasGroup == null && passcodeUIPanel != null)
        {
            passcodeCanvasGroup = passcodeUIPanel.GetComponent<CanvasGroup>();
        }

        // UI starts hidden
        if (passcodeUIPanel != null)
        {
            passcodeUIPanel.SetActive(false);
        }
        isUIVisible = false;

        TryAutoWireUIReferences();

        // Setup the submit button's listener automatically
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(CheckPasscode);
        }

        CachePlayerComponents();
        CacheCameraControlComponents();
        CacheCanvasStates();
    }

    void Update()
    {
        // Don't check for interaction if UI is already open
        if (isUIVisible)
        {
            HidePrompt();

            if (WasKeyPressedThisFrame(cancelKey) && !interactionBusy)
            {
                StartCoroutine(CloseConsoleFlow());
            }

            if (!interactionBusy && (WasKeyPressedThisFrame(KeyCode.Return) || WasKeyPressedThisFrame(KeyCode.KeypadEnter)))
            {
                CheckPasscode();
            }
            return;
        }

        // Check distance to player
        if (player == null)
        {
            HidePrompt();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= interactionDistance)
        {
            ShowPrompt();

            if (!interactionBusy && WasKeyPressedThisFrame(interactKey))
            {
                StartCoroutine(OpenConsoleFlow());
            }
        }
        else
        {
            HidePrompt();
        }
    }

    // --- UI Logic ---

    private IEnumerator OpenConsoleFlow()
    {
        interactionBusy = true;
        IsConsoleInteractionActive = true;
        HidePrompt();

        if (useCameraPan && mainCamera != null)
        {
            preInteractionCameraPosition = mainCamera.transform.position;
            preInteractionCameraRotation = mainCamera.transform.rotation;
            hasPreInteractionCameraPose = true;
        }

        if (disablePlayerControlWhileOpen)
        {
            SetPlayerComponentsEnabled(false);
        }

        if (useCameraPan)
        {
            SetCameraControlComponentsEnabled(false);
        }

        if (useCameraPan && mainCamera != null && consoleCameraView != null)
        {
            yield return StartCoroutine(PanCamera(mainCamera.transform, mainCamera.transform.position, mainCamera.transform.rotation, consoleCameraView.position, consoleCameraView.rotation));
        }

        OpenPasscodeUI();
        interactionBusy = false;
    }

    private IEnumerator CloseConsoleFlow()
    {
        interactionBusy = true;

        ClosePasscodeUI();

        if (useCameraPan && mainCamera != null)
        {
            Vector3 restorePosition = hasPreInteractionCameraPose ? preInteractionCameraPosition : originalCameraPosition;
            Quaternion restoreRotation = hasPreInteractionCameraPose ? preInteractionCameraRotation : originalCameraRotation;
            yield return StartCoroutine(PanCamera(mainCamera.transform, mainCamera.transform.position, mainCamera.transform.rotation, restorePosition, restoreRotation));
        }

        if (disablePlayerControlWhileOpen)
        {
            SetPlayerComponentsEnabled(true);
        }

        if (useCameraPan)
        {
            RestoreCameraControlComponents();
        }

        interactionBusy = false;
        IsConsoleInteractionActive = false;
    }

    void OpenPasscodeUI()
    {
        HidePrompt();
        isUIVisible = true;

        SetOtherCanvasesVisible(false);

        if (passcodeUIPanel != null)
        {
            passcodeUIPanel.SetActive(true);
        }

        if (passcodeCanvasGroup != null)
        {
            passcodeCanvasGroup.alpha = 1f;
            passcodeCanvasGroup.interactable = true;
            passcodeCanvasGroup.blocksRaycasts = true;
        }

        // Prepare the Input Field
        if (inputField != null)
        {
            inputField.text = ""; // Clear old attempts
            inputField.ActivateInputField(); // Automatically focuses cursor in the box
        }

        if (statusText != null)
        {
            statusText.text = "ENTER ACCESS CODE"; // Reset text color
            statusText.color = Color.white;
        }

        // ENABLE MOUSE CURSOR
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePasscodeUI()
    {
        isUIVisible = false;

        RestoreOtherCanvasesVisibility();

        if (passcodeUIPanel != null)
        {
            passcodeUIPanel.SetActive(false);
        }

        // LOCK MOUSE CURSOR BACK (assuming you have a mouse-look setup)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ShowPrompt();
    }

    public void CheckPasscode()
    {
        if (!isUIVisible || interactionBusy)
        {
            return;
        }

        // Convert to uppercase in case they type "pass" instead of "PASS" (not needed for numbers)
        string currentInput = inputField != null ? inputField.text : string.Empty;
        string expectedPasscode = correctPasscode;
        if (preferGeneratedStabilitySignature && StabilitySignatureCode.HasCode)
        {
            expectedPasscode = StabilitySignatureCode.CurrentCode;
        }

        if (currentInput == expectedPasscode)
        {
            StartCoroutine(SuccessFlow());
        }
        else
        {
            Failure();
        }
    }

    private IEnumerator SuccessFlow()
    {
        interactionBusy = true;

        if (unlockDoorOnSuccess)
        {
            Debug.Log($"ExitConsoleInteraction: unlockDoorOnSuccess is TRUE. Linked trigger: {(linkedDoorTrigger != null ? linkedDoorTrigger.name : "<null>")}", this);
            UnlockDoorFromConsole();
        }
        else
        {
            Debug.LogWarning("ExitConsoleInteraction: unlockDoorOnSuccess is FALSE on this instance, so no door unlock is attempted.", this);
        }

        // Visually confirm success
        if (statusText != null)
        {
            statusText.text = "ACCEPTED";
            statusText.color = Color.green;
        }

        Debug.Log("Passcode correct. Closing console UI.");

        float waitTimer = 0f;
        while (waitTimer < 0.25f)
        {
            waitTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return StartCoroutine(CloseConsoleFlow());
    }

    private void UnlockDoorFromConsole()
    {
        Debug.Log("ExitConsoleInteraction: Entered UnlockDoorFromConsole().", this);

        DoorProximityTrigger rootTrigger = linkedDoorTrigger;
        if (rootTrigger == null)
        {
            rootTrigger = FindNearestConsoleDoorTrigger();
            if (rootTrigger == null)
            {
                Debug.LogWarning("ExitConsoleInteraction: unlockDoorOnSuccess is enabled, but no linked or nearby console-gated DoorProximityTrigger was found.", this);
                return;
            }

            Debug.LogWarning($"ExitConsoleInteraction: linkedDoorTrigger is not assigned. Using nearest console-gated trigger '{rootTrigger.name}'.", this);
        }

        List<DoorProximityTrigger> targets = new List<DoorProximityTrigger>();
        CollectUniqueTrigger(rootTrigger, targets);

        DoorProximityTrigger[] siblings = rootTrigger.GetComponentsInParent<DoorProximityTrigger>(true);
        for (int i = 0; i < siblings.Length; ++i)
        {
            CollectUniqueTrigger(siblings[i], targets);
        }

        DoorProximityTrigger[] childTriggers = rootTrigger.GetComponentsInChildren<DoorProximityTrigger>(true);
        for (int i = 0; i < childTriggers.Length; ++i)
        {
            CollectUniqueTrigger(childTriggers[i], targets);
        }

        // Include nearby console-gated doors to catch sibling trigger setups.
        DoorProximityTrigger[] allTriggers = FindObjectsByType<DoorProximityTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float maxDistance = Mathf.Max(0f, autoFindDoorMaxDistance);
        float maxDistanceSqr = maxDistance * maxDistance;
        for (int i = 0; i < allTriggers.Length; ++i)
        {
            DoorProximityTrigger candidate = allTriggers[i];
            if (candidate == null || !candidate.requireConsoleUnlock)
            {
                continue;
            }

            float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance <= maxDistanceSqr)
            {
                CollectUniqueTrigger(candidate, targets);
            }
        }

        int unlockCount = 0;
        string unlockedNames = string.Empty;
        for (int i = 0; i < targets.Count; ++i)
        {
            if (targets[i] == null)
            {
                continue;
            }

            targets[i].DisableConsoleRequirement();
            unlockCount++;

            if (unlockedNames.Length == 0)
            {
                unlockedNames = targets[i].name;
            }
            else
            {
                unlockedNames += ", " + targets[i].name;
            }
        }

        if (unlockCount == 0)
        {
            Debug.LogWarning("ExitConsoleInteraction: No valid DoorProximityTrigger targets were unlocked from console success.", this);
        }
        else
        {
            Debug.Log($"ExitConsoleInteraction: Unlocked {unlockCount} door trigger(s) from console success: {unlockedNames}", this);
        }
    }

    private DoorProximityTrigger FindNearestConsoleDoorTrigger()
    {
        DoorProximityTrigger[] triggers = FindObjectsByType<DoorProximityTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Debug.Log($"ExitConsoleInteraction: FindNearestConsoleDoorTrigger scanning {triggers.Length} trigger(s).", this);
        DoorProximityTrigger nearest = null;
        float nearestSqrDistance = float.MaxValue;
        float maxDistanceSqr = Mathf.Max(0f, autoFindDoorMaxDistance) * Mathf.Max(0f, autoFindDoorMaxDistance);
        int candidatesInRange = 0;

        for (int i = 0; i < triggers.Length; ++i)
        {
            DoorProximityTrigger trigger = triggers[i];
            if (trigger == null || !trigger.requireConsoleUnlock)
            {
                continue;
            }

            float sqrDistance = (trigger.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > maxDistanceSqr)
            {
                continue;
            }

            candidatesInRange++;

            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = trigger;
            }
        }

        Debug.Log($"ExitConsoleInteraction: Found {candidatesInRange} console-gated trigger(s) within range {autoFindDoorMaxDistance}.", this);

        return nearest;
    }

    private void CollectUniqueTrigger(DoorProximityTrigger candidate, List<DoorProximityTrigger> targets)
    {
        if (candidate == null || targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; ++i)
        {
            if (targets[i] == candidate)
            {
                return;
            }
        }

        targets.Add(candidate);
    }

    void Failure()
    {
        // Visually confirm failure
        if (statusText != null)
        {
            statusText.text = "DENIED: INCORRECT CODE";
            statusText.color = Color.red;
        }

        if (inputField != null)
        {
            inputField.text = ""; // Clear input for next try
            inputField.ActivateInputField(); // Keep input field focused
        }
    }

    void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(CheckPasscode);
        }

        if (disablePlayerControlWhileOpen)
        {
            SetPlayerComponentsEnabled(true);
        }

        RestoreCameraControlComponents();
        RestoreOtherCanvasesVisibility();

        if (IsConsoleInteractionActive)
        {
            IsConsoleInteractionActive = false;
        }

        HidePrompt();
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
            case KeyCode.Escape:
                return Keyboard.current.escapeKey.wasPressedThisFrame;
            case KeyCode.Tab:
                return Keyboard.current.tabKey.wasPressedThisFrame;
            case KeyCode.Return:
                return Keyboard.current.enterKey.wasPressedThisFrame;
            case KeyCode.KeypadEnter:
                return Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            default:
                return false;
        }
#else
        return Input.GetKeyDown(key);
#endif
    }

    private void TryAutoWireUIReferences()
    {
        if (passcodeUIPanel == null)
        {
            return;
        }

        if (submitButton == null)
        {
            submitButton = passcodeUIPanel.GetComponentInChildren<Button>(true);
        }

        if (inputField == null)
        {
            inputField = passcodeUIPanel.GetComponentInChildren<TMP_InputField>(true);
        }

        if (statusText == null)
        {
            statusText = passcodeUIPanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private IEnumerator PanCamera(Transform cameraTransform, Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot)
    {
        float duration = Mathf.Max(0.01f, cameraPanDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = cameraPanCurve != null ? cameraPanCurve.Evaluate(t) : t;
            cameraTransform.position = Vector3.Lerp(fromPos, toPos, easedT);
            cameraTransform.rotation = Quaternion.Slerp(fromRot, toRot, easedT);
            yield return null;
        }

        cameraTransform.position = toPos;
        cameraTransform.rotation = toRot;
    }

    private void CachePlayerComponents()
    {
        if (player == null)
        {
            cachedPlayerComponents = new MonoBehaviour[0];
            return;
        }

        BasicControlScript movement = player.GetComponent<BasicControlScript>();
        CharacterInputController input = player.GetComponent<CharacterInputController>();

        if (movement != null && input != null)
        {
            cachedPlayerComponents = new MonoBehaviour[] { movement, input };
        }
        else if (movement != null)
        {
            cachedPlayerComponents = new MonoBehaviour[] { movement };
        }
        else if (input != null)
        {
            cachedPlayerComponents = new MonoBehaviour[] { input };
        }
        else
        {
            cachedPlayerComponents = new MonoBehaviour[0];
        }
    }

    private void SetPlayerComponentsEnabled(bool enabledState)
    {
        if (cachedPlayerComponents == null)
        {
            return;
        }

        for (int i = 0; i < cachedPlayerComponents.Length; ++i)
        {
            if (cachedPlayerComponents[i] != null)
            {
                cachedPlayerComponents[i].enabled = enabledState;
            }
        }
    }

    private void CacheCameraControlComponents()
    {
        if (mainCamera == null)
        {
            cachedCameraControlComponents = new Behaviour[0];
            cachedCameraControlInitialStates = new bool[0];
            return;
        }

        List<Behaviour> controls = new List<Behaviour>();
        Behaviour[] allBehaviours = mainCamera.GetComponents<Behaviour>();

        for (int i = 0; i < allBehaviours.Length; ++i)
        {
            Behaviour behaviour = allBehaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour is CameraFollow || behaviour is EnhancedCameraFollow || behaviour is TitleScreenCamera)
            {
                controls.Add(behaviour);
            }
        }

        cachedCameraControlComponents = controls.ToArray();
        cachedCameraControlInitialStates = new bool[cachedCameraControlComponents.Length];
        for (int i = 0; i < cachedCameraControlComponents.Length; ++i)
        {
            cachedCameraControlInitialStates[i] = cachedCameraControlComponents[i] != null && cachedCameraControlComponents[i].enabled;
        }
    }

    private void SetCameraControlComponentsEnabled(bool enabledState)
    {
        if (cachedCameraControlComponents == null)
        {
            return;
        }

        for (int i = 0; i < cachedCameraControlComponents.Length; ++i)
        {
            if (cachedCameraControlComponents[i] != null)
            {
                cachedCameraControlComponents[i].enabled = enabledState;
            }
        }
    }

    private void RestoreCameraControlComponents()
    {
        if (cachedCameraControlComponents == null || cachedCameraControlInitialStates == null)
        {
            return;
        }

        for (int i = 0; i < cachedCameraControlComponents.Length; ++i)
        {
            if (cachedCameraControlComponents[i] != null && i < cachedCameraControlInitialStates.Length)
            {
                cachedCameraControlComponents[i].enabled = cachedCameraControlInitialStates[i];
            }
        }
    }

    private void CacheCanvasStates()
    {
        if (canvasesToHideWhileOpen == null)
        {
            cachedCanvasInitialStates = new bool[0];
            return;
        }

        cachedCanvasInitialStates = new bool[canvasesToHideWhileOpen.Length];
        for (int i = 0; i < canvasesToHideWhileOpen.Length; ++i)
        {
            cachedCanvasInitialStates[i] = canvasesToHideWhileOpen[i] != null && canvasesToHideWhileOpen[i].enabled;
        }
    }

    private void SetOtherCanvasesVisible(bool visible)
    {
        if (canvasesToHideWhileOpen == null)
        {
            return;
        }

        for (int i = 0; i < canvasesToHideWhileOpen.Length; ++i)
        {
            Canvas canvas = canvasesToHideWhileOpen[i];
            if (canvas == null)
            {
                continue;
            }

            if (passcodeUIPanel != null && canvas.gameObject == passcodeUIPanel)
            {
                continue;
            }

            canvas.enabled = visible;
        }
    }

    private void RestoreOtherCanvasesVisibility()
    {
        if (canvasesToHideWhileOpen == null || cachedCanvasInitialStates == null)
        {
            return;
        }

        for (int i = 0; i < canvasesToHideWhileOpen.Length; ++i)
        {
            if (i >= cachedCanvasInitialStates.Length)
            {
                break;
            }

            Canvas canvas = canvasesToHideWhileOpen[i];
            if (canvas != null)
            {
                canvas.enabled = cachedCanvasInitialStates[i];
            }
        }
    }

    private void ShowPrompt()
    {
        if (promptVisible)
        {
            return;
        }

        if (InteractionFeedbackHUD.Instance != null)
        {
            InteractionFeedbackHUD.Instance.ShowMessage(interactionPromptText, 0f);
            promptVisible = true;
        }
    }

    private void HidePrompt()
    {
        if (!promptVisible)
        {
            return;
        }

        if (InteractionFeedbackHUD.Instance != null)
        {
            InteractionFeedbackHUD.Instance.ClearMessage();
        }

        promptVisible = false;
    }
}