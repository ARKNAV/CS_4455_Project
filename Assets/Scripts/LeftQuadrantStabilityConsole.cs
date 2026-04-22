using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class LeftQuadrantStabilityConsole : MonoBehaviour
{
    public static bool IsPuzzleInteractionActive { get; private set; }
    public static int LastPuzzleCloseFrame { get; private set; } = -1;

    [System.Serializable]
    public class SectorSlider
    {
        public string sectorName = "Sector";
        public Slider slider;
        public float targetValue = 0.75f;
        [Range(0.001f, 0.25f)] public float tolerance = 0.02f;
        public TextMeshProUGUI digitText;
        public string solvedDigit = "0";
        public Graphic[] greenWhenAligned;
        [HideInInspector] public Color[] defaultColors;
    }

    [Header("UI References (Assign in Inspector)")]
    public GameObject puzzleUIPanel;
    public CanvasGroup puzzleCanvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI loreText;
    public SectorSlider[] sectorSliders = new SectorSlider[4];

    [Header("Prompt + Interaction")]
    public float interactionDistance = 3f;
    public KeyCode interactKey = KeyCode.F;
    public KeyCode cancelKey = KeyCode.Escape;
    public string interactionPromptText = "Press F to sync core stability";

    [Header("Player Lock (While Puzzle Open)")]
    public bool disablePlayerControlWhileOpen = true;

    [Header("Camera Pan (Optional)")]
    public bool useCameraPan = true;
    public Transform consoleCameraView;
    public float cameraPanDuration = 0.35f;
    public AnimationCurve cameraPanCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("UI Visibility (Optional)")]
    public Canvas[] canvasesToHideWhileOpen;
    public GameObject[] uiObjectsToHideWhileOpen;

    [Header("Behavior")]
    public bool keepSolvedDigitsVisible = false;
    public bool freezeSolvedSliders = false;
    public bool clearGeneratedCodeOnStart = true;

    [Header("Objective Integration (Optional)")]
    public bool completeObjectiveOnSolve = true;
    public string solveObjectiveId = "stability_signature";

    private const string DefaultTitle = "STABILITY SYNCHRONIZATION";
    private const string DefaultReadyMessage = "Align all sectors to reveal stability signature";
    private const string DefaultSolvedMessagePrefix = "SIGNATURE LOCKED: ";
    private const string DefaultLore = "NOTICE: Core stability must be manually synced every hour. Security Gate [S2-EX] will only accept the current stability signature as an entry key.";

    private GameObject player;
    private Camera mainCamera;
    private bool isUIVisible;
    private bool interactionBusy;
    private bool promptVisible;
    private bool puzzleSolved;
    private bool[] sectorSolved;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Vector3 preInteractionCameraPosition;
    private Quaternion preInteractionCameraRotation;
    private bool hasPreInteractionCameraPose;

    private MonoBehaviour[] cachedPlayerComponents;
    private Behaviour[] cachedCameraControlComponents;
    private bool[] cachedCameraControlInitialStates;
    private bool[] cachedCanvasInitialStates;
    private bool[] cachedUiObjectActiveStates;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        mainCamera = Camera.main;

        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }

        if (puzzleCanvasGroup == null && puzzleUIPanel != null)
        {
            puzzleCanvasGroup = puzzleUIPanel.GetComponent<CanvasGroup>();
        }

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }

        if (clearGeneratedCodeOnStart)
        {
            StabilitySignatureCode.ClearCode();
        }

        ApplyDefaultText();
        CachePlayerComponents();
        CacheCameraControlComponents();
        CacheCanvasStates();
        CacheUiObjectStates();
        SetupSliders();
        ResetPuzzleVisualState();
    }

    private void Update()
    {
        if (isUIVisible)
        {
            HidePrompt();

            bool closeRequested = !interactionBusy && WasKeyPressedThisFrame(cancelKey);
            bool closeByEscape = closeRequested && cancelKey == KeyCode.Escape;
            if (closeRequested)
            {
                StartCoroutine(ClosePuzzleFlow(closeByEscape));
            }

            return;
        }

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
                StartCoroutine(OpenPuzzleFlow());
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private IEnumerator OpenPuzzleFlow()
    {
        interactionBusy = true;
        IsPuzzleInteractionActive = true;
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

        OpenPuzzleUI();
        interactionBusy = false;
    }

    private IEnumerator ClosePuzzleFlow(bool closedByEscape = false)
    {
        interactionBusy = true;
        ClosePuzzleUI();

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
        IsPuzzleInteractionActive = false;

        if (closedByEscape)
        {
            LastPuzzleCloseFrame = Time.frameCount;
        }
    }

    private void OpenPuzzleUI()
    {
        isUIVisible = true;
        SetOtherCanvasesVisible(false);
        SetUiObjectsVisible(false);

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(true);
        }

        if (puzzleCanvasGroup != null)
        {
            puzzleCanvasGroup.alpha = 1f;
            puzzleCanvasGroup.interactable = true;
            puzzleCanvasGroup.blocksRaycasts = true;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePuzzleUI()
    {
        isUIVisible = false;
        RestoreOtherCanvasesVisibility();
        RestoreUiObjectsVisibility();

        if (puzzleUIPanel != null)
        {
            puzzleUIPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ShowPrompt();
    }

    private void ApplyDefaultText()
    {
        if (titleText != null && string.IsNullOrWhiteSpace(titleText.text))
        {
            titleText.text = DefaultTitle;
        }

        if (statusText != null)
        {
            statusText.text = DefaultReadyMessage;
            statusText.color = Color.white;
        }

        if (loreText != null && string.IsNullOrWhiteSpace(loreText.text))
        {
            loreText.text = DefaultLore;
        }
    }

    private void SetupSliders()
    {
        int sliderCount = sectorSliders != null ? sectorSliders.Length : 0;
        sectorSolved = new bool[sliderCount];

        for (int i = 0; i < sliderCount; ++i)
        {
            SectorSlider sector = sectorSliders[i];
            if (sector == null || sector.slider == null)
            {
                continue;
            }

            int index = i;
            sector.slider.onValueChanged.AddListener(delegate { OnSliderValueChanged(index); });

            if (sector.greenWhenAligned == null)
            {
                continue;
            }

            sector.defaultColors = new Color[sector.greenWhenAligned.Length];
            for (int j = 0; j < sector.greenWhenAligned.Length; ++j)
            {
                Graphic g = sector.greenWhenAligned[j];
                sector.defaultColors[j] = g != null ? g.color : Color.white;
            }
        }
    }

    private void ResetPuzzleVisualState()
    {
        puzzleSolved = false;

        if (sectorSolved == null)
        {
            return;
        }

        for (int i = 0; i < sectorSolved.Length; ++i)
        {
            sectorSolved[i] = false;

            SectorSlider sector = sectorSliders[i];
            if (sector == null)
            {
                continue;
            }

            if (sector.digitText != null)
            {
                sector.digitText.text = string.Empty;
                sector.digitText.enabled = false;
            }

            if (sector.greenWhenAligned == null)
            {
                continue;
            }

            for (int j = 0; j < sector.greenWhenAligned.Length; ++j)
            {
                Graphic g = sector.greenWhenAligned[j];
                if (g == null)
                {
                    continue;
                }

                if (sector.defaultColors != null && j < sector.defaultColors.Length)
                {
                    g.color = sector.defaultColors[j];
                }
            }
        }

        if (statusText != null)
        {
            statusText.text = DefaultReadyMessage;
            statusText.color = Color.white;
        }
    }

    private void OnSliderValueChanged(int index)
    {
        if (index < 0 || sectorSliders == null || index >= sectorSliders.Length)
        {
            return;
        }

        SectorSlider sector = sectorSliders[index];
        if (sector == null || sector.slider == null)
        {
            return;
        }

        bool aligned = Mathf.Abs(sector.slider.value - sector.targetValue) <= Mathf.Max(0.001f, sector.tolerance);
        sectorSolved[index] = aligned;

        if (aligned)
        {
            ApplySectorAlignedVisuals(sector, true);

            if (sector.digitText != null)
            {
                sector.digitText.text = string.IsNullOrEmpty(sector.solvedDigit) ? "0" : sector.solvedDigit;
                sector.digitText.enabled = true;
            }

            if (freezeSolvedSliders)
            {
                sector.slider.interactable = false;
            }
        }
        else
        {
            ApplySectorAlignedVisuals(sector, false);

            if (sector.digitText != null && !keepSolvedDigitsVisible)
            {
                sector.digitText.text = string.Empty;
                sector.digitText.enabled = false;
            }

            if (!freezeSolvedSliders)
            {
                sector.slider.interactable = true;
            }
        }

        EvaluatePuzzleCompletion();
    }

    private void ApplySectorAlignedVisuals(SectorSlider sector, bool aligned)
    {
        if (sector == null || sector.greenWhenAligned == null)
        {
            return;
        }

        for (int i = 0; i < sector.greenWhenAligned.Length; ++i)
        {
            Graphic g = sector.greenWhenAligned[i];
            if (g == null)
            {
                continue;
            }

            Color targetColor = Color.white;
            if (aligned)
            {
                targetColor = Color.green;
            }
            else if (sector.defaultColors != null && i < sector.defaultColors.Length)
            {
                targetColor = sector.defaultColors[i];
            }

            g.color = targetColor;
        }
    }

    private void EvaluatePuzzleCompletion()
    {
        if (sectorSolved == null || sectorSolved.Length == 0)
        {
            return;
        }

        for (int i = 0; i < sectorSolved.Length; ++i)
        {
            if (!sectorSolved[i])
            {
                puzzleSolved = false;
                return;
            }
        }

        if (puzzleSolved)
        {
            return;
        }

        puzzleSolved = true;
        string signature = BuildSignatureCode();
        StabilitySignatureCode.SetCode(signature);

        CompleteSolveObjective();

        if (statusText != null)
        {
            statusText.text = DefaultSolvedMessagePrefix + signature;
            statusText.color = Color.green;
        }

        Debug.Log($"LeftQuadrantStabilityConsole: Puzzle solved. Stability signature is {signature}.", this);
    }

    private string BuildSignatureCode()
    {
        if (sectorSliders == null || sectorSliders.Length == 0)
        {
            return string.Empty;
        }

        string code = string.Empty;
        for (int i = 0; i < sectorSliders.Length; ++i)
        {
            string digit = sectorSliders[i] != null ? sectorSliders[i].solvedDigit : "0";
            if (string.IsNullOrEmpty(digit))
            {
                digit = "0";
            }

            code += digit;
        }

        return code;
    }

    private void CompleteSolveObjective()
    {
        if (!completeObjectiveOnSolve || string.IsNullOrWhiteSpace(solveObjectiveId))
        {
            return;
        }

        DemoObjectiveManager manager = DemoObjectiveManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (manager.IsCurrentObjective(solveObjectiveId))
        {
            manager.CompleteObjective(solveObjectiveId);
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

    private void CacheUiObjectStates()
    {
        if (uiObjectsToHideWhileOpen == null)
        {
            cachedUiObjectActiveStates = new bool[0];
            return;
        }

        cachedUiObjectActiveStates = new bool[uiObjectsToHideWhileOpen.Length];
        for (int i = 0; i < uiObjectsToHideWhileOpen.Length; ++i)
        {
            cachedUiObjectActiveStates[i] = uiObjectsToHideWhileOpen[i] != null && uiObjectsToHideWhileOpen[i].activeSelf;
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

            if (puzzleUIPanel != null && canvas.gameObject == puzzleUIPanel)
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

    private void SetUiObjectsVisible(bool visible)
    {
        if (uiObjectsToHideWhileOpen == null)
        {
            return;
        }

        for (int i = 0; i < uiObjectsToHideWhileOpen.Length; ++i)
        {
            GameObject uiObject = uiObjectsToHideWhileOpen[i];
            if (uiObject == null)
            {
                continue;
            }

            if (puzzleUIPanel != null && uiObject == puzzleUIPanel)
            {
                continue;
            }

            uiObject.SetActive(visible);
        }
    }

    private void RestoreUiObjectsVisibility()
    {
        if (uiObjectsToHideWhileOpen == null || cachedUiObjectActiveStates == null)
        {
            return;
        }

        for (int i = 0; i < uiObjectsToHideWhileOpen.Length; ++i)
        {
            if (i >= cachedUiObjectActiveStates.Length)
            {
                break;
            }

            GameObject uiObject = uiObjectsToHideWhileOpen[i];
            if (uiObject != null)
            {
                uiObject.SetActive(cachedUiObjectActiveStates[i]);
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

    private void OnDestroy()
    {
        if (disablePlayerControlWhileOpen)
        {
            SetPlayerComponentsEnabled(true);
        }

        RestoreCameraControlComponents();
        RestoreOtherCanvasesVisibility();
        RestoreUiObjectsVisibility();

        if (IsPuzzleInteractionActive)
        {
            IsPuzzleInteractionActive = false;
        }

        HidePrompt();
    }
}
