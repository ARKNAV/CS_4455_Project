using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class KeycardReaderController : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    [Header("Reader Visual")]
    [SerializeField] private Renderer readerRenderer;
    [SerializeField] private string colorPropertyName = "_BaseColor";
    [SerializeField] private string fallbackColorPropertyName = "_Color";
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";
    [SerializeField] private bool driveEmissionColor = true;
    [SerializeField] private float emissionIntensityMultiplier = 2f;
    [SerializeField] private bool resetToIdleAfterInteraction = true;
    [SerializeField] private float resultColorHoldDuration = 0.8f;
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color accessDeniedColor = Color.red;
    [SerializeField] private Color accessGrantedColor = Color.green;

    [Header("Door")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string doorOpenBool = "character_nearby";
    [SerializeField] private bool keepDoorOpenAfterUnlock = true;

    [Header("Player Animation")]
    [SerializeField] private bool triggerPlayerArmAnimation = true;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string useReaderTrigger = "UseKeypad";

    [Header("Interaction Alignment")]
    [SerializeField] private Transform interactionAnchor;
    [SerializeField] private bool snapPlayerToAnchor = true;
    [SerializeField] private bool walkToAnchor = true;
    [SerializeField] private float walkToAnchorSpeed = 2.25f;
    [SerializeField] private float walkStopDistance = 0.05f;
    [SerializeField] private float maxWalkToAnchorTime = 2f;
    [SerializeField] private string locomotionSpeedParameter = "speed";
    [SerializeField] private float autoWalkAnimatorSpeed = 1f;
    [SerializeField] private bool alignPlayerRotationToAnchor = true;
    [SerializeField] private float playerYawOffsetDegrees = 0f;
    [SerializeField] private float interactionLockDuration = 1.25f;
    [SerializeField] private bool disableInputDuringInteraction = true;
    [SerializeField] private bool disableMovementDuringInteraction = true;

    [Header("Feedback")]
    [SerializeField] private string needsCardMessage = "You need to collect the key card first.";
    [SerializeField] private string accessGrantedMessage = "Access granted. Door unlocked.";
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource interactionAudioSource;
    [SerializeField] private AudioClip keypadSwipeClip;
    [SerializeField] private AudioClip accessDeniedClip;
    [SerializeField] private AudioClip accessGrantedClip;
    [SerializeField] private AudioClip doorOpenClip;

    [Header("Objective")]
    [SerializeField] private bool completeExitObjectiveOnSuccess = true;
    [SerializeField] private string exitObjectiveId = "exit";

    private bool playerInRange;
    private bool isUnlocked;
    private bool interactionInProgress;
    private int doorOpenBoolHash;
    private int useReaderTriggerHash;
    private MaterialPropertyBlock materialPropertyBlock;
    private Transform interactingPlayerRoot;
    private Coroutine colorResetRoutine;

    public static Action WinTriggerEvent;

    public bool IsUnlocked
    {
        get { return isUnlocked; }
    }

    private void Awake()
    {
        if (readerRenderer == null)
        {
            readerRenderer = GetComponentInChildren<Renderer>();
        }

        if (interactionAudioSource == null)
        {
            interactionAudioSource = GetComponent<AudioSource>();
        }

        if (interactionAudioSource == null)
        {
            interactionAudioSource = gameObject.AddComponent<AudioSource>();
            interactionAudioSource.playOnAwake = false;
            interactionAudioSource.spatialBlend = 0f;
        }

        doorOpenBoolHash = Animator.StringToHash(doorOpenBool);
        useReaderTriggerHash = Animator.StringToHash(useReaderTrigger);
        materialPropertyBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        SetReaderColor(idleColor);
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInRange || interactionInProgress)
        {
            return;
        }

        if (!WasInteractPressedThisFrame())
        {
            return;
        }

        StartCoroutine(HandleInteractionFlow());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        interactingPlayerRoot = other.transform.root;

        if (playerAnimator == null)
        {
            playerAnimator = other.GetComponentInParent<Animator>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;

        if (!keepDoorOpenAfterUnlock && !isUnlocked)
        {
            SetDoorOpen(false);
        }
    }

    private IEnumerator HandleInteractionFlow()
    {
        interactionInProgress = true;

        Transform playerRoot = ResolvePlayerRootTransform();
        CharacterInputController playerInput = playerRoot != null ? playerRoot.GetComponent<CharacterInputController>() : null;
        BasicControlScript movementController = playerRoot != null ? playerRoot.GetComponent<BasicControlScript>() : null;
        Rigidbody playerBody = playerRoot != null ? playerRoot.GetComponent<Rigidbody>() : null;

        bool wasInputEnabled = playerInput != null && playerInput.enabled;
        bool wasMovementEnabled = movementController != null && movementController.enabled;

        if (disableInputDuringInteraction && playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (disableMovementDuringInteraction && movementController != null)
        {
            movementController.enabled = false;
        }

        if (snapPlayerToAnchor && interactionAnchor != null && playerRoot != null)
        {
            if (walkToAnchor)
            {
                yield return StartCoroutine(WalkPlayerToAnchor(playerRoot, playerBody));
            }
            else
            {
                MovePlayerToAnchor(playerRoot, playerBody);
            }
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        if (triggerPlayerArmAnimation && playerAnimator != null && !string.IsNullOrWhiteSpace(useReaderTrigger))
        {
            playerAnimator.SetTrigger(useReaderTriggerHash);
        }

        PlayClip(keypadSwipeClip);

        if (interactionLockDuration > 0f)
        {
            yield return new WaitForSeconds(interactionLockDuration);
        }

        EvaluateAccess();

        if (disableInputDuringInteraction && playerInput != null)
        {
            playerInput.enabled = wasInputEnabled;
        }

        if (disableMovementDuringInteraction && movementController != null)
        {
            movementController.enabled = wasMovementEnabled;
        }

        interactionInProgress = false;
    }

    private void EvaluateAccess()
    {
        bool wasUnlocked = isUnlocked;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            ShowFeedback("No inventory found on player.", Color.yellow);
            return;
        }

        if (!inventory.HasKeycard)
        {
            SetReaderColor(accessDeniedColor);
            SetDoorOpen(false);
            PlayClip(accessDeniedClip);
            ShowFeedback(needsCardMessage, accessDeniedColor);
            QueueReaderIdleReset();
            return;
        }

        isUnlocked = true;
        SetReaderColor(accessGrantedColor);
        SetDoorOpen(true);
        PlayClip(accessGrantedClip);
        if (!wasUnlocked)
        {
            PlayClip(doorOpenClip);
        }
        ShowFeedback(accessGrantedMessage, accessGrantedColor);
        QueueReaderIdleReset();

        if (completeExitObjectiveOnSuccess && DemoObjectiveManager.Instance != null)
        {
            DemoObjectiveManager manager = DemoObjectiveManager.Instance;
            if (manager.IsCurrentObjective(exitObjectiveId))
            {
                manager.CompleteObjective(exitObjectiveId);
            }
        }

        WinTriggerEvent?.Invoke();
    }

    private void QueueReaderIdleReset()
    {
        if (!resetToIdleAfterInteraction)
        {
            return;
        }

        if (colorResetRoutine != null)
        {
            StopCoroutine(colorResetRoutine);
        }

        colorResetRoutine = StartCoroutine(ResetReaderColorAfterDelay());
    }

    private IEnumerator ResetReaderColorAfterDelay()
    {
        float delay = Mathf.Max(0f, resultColorHoldDuration);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SetReaderColor(idleColor);
        colorResetRoutine = null;
    }

    private Transform ResolvePlayerRootTransform()
    {
        if (interactingPlayerRoot != null)
        {
            return interactingPlayerRoot;
        }

        if (playerAnimator != null)
        {
            return playerAnimator.transform.root;
        }

        return null;
    }

    private void MovePlayerToAnchor(Transform playerRoot, Rigidbody playerBody)
    {
        Quaternion targetRotation = interactionAnchor.rotation * Quaternion.Euler(0f, playerYawOffsetDegrees, 0f);

        if (playerBody != null)
        {
            playerBody.position = interactionAnchor.position;
            if (alignPlayerRotationToAnchor)
            {
                playerBody.rotation = targetRotation;
            }

            return;
        }

        playerRoot.position = interactionAnchor.position;
        if (alignPlayerRotationToAnchor)
        {
            playerRoot.rotation = targetRotation;
        }
    }

    private IEnumerator WalkPlayerToAnchor(Transform playerRoot, Rigidbody playerBody)
    {
        float elapsed = 0f;
        float minSpeed = Mathf.Max(0.01f, walkToAnchorSpeed);
        float stopDistance = Mathf.Max(0.01f, walkStopDistance);

        if (playerAnimator != null && !string.IsNullOrWhiteSpace(locomotionSpeedParameter))
        {
            playerAnimator.SetFloat(locomotionSpeedParameter, autoWalkAnimatorSpeed);
        }

        while (elapsed < maxWalkToAnchorTime)
        {
            Vector3 current = playerBody != null ? playerBody.position : playerRoot.position;
            Vector3 targetPlanar = new Vector3(interactionAnchor.position.x, current.y, interactionAnchor.position.z);
            Vector3 toTarget = targetPlanar - current;
            float distance = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                break;
            }

            Vector3 next = Vector3.MoveTowards(current, targetPlanar, minSpeed * Time.deltaTime);
            if (playerBody != null)
            {
                playerBody.MovePosition(next);
            }
            else
            {
                playerRoot.position = next;
            }

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 facingDir = toTarget;
                facingDir.y = 0f;
                Quaternion facing = Quaternion.LookRotation(facingDir.normalized, Vector3.up);
                if (playerBody != null)
                {
                    playerBody.MoveRotation(Quaternion.RotateTowards(playerBody.rotation, facing, 720f * Time.deltaTime));
                }
                else
                {
                    playerRoot.rotation = Quaternion.RotateTowards(playerRoot.rotation, facing, 720f * Time.deltaTime);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        MovePlayerToAnchor(playerRoot, playerBody);

        if (playerAnimator != null && !string.IsNullOrWhiteSpace(locomotionSpeedParameter))
        {
            playerAnimator.SetFloat(locomotionSpeedParameter, 0f);
        }
    }

    private void SetDoorOpen(bool isOpen)
    {
        if (doorAnimator == null)
        {
            return;
        }

        doorAnimator.SetBool(doorOpenBoolHash, isOpen);
    }

    private void ShowFeedback(string message, Color messageColor)
    {
        if (InteractionFeedbackHUD.Instance == null)
        {
            return;
        }

        InteractionFeedbackHUD.Instance.ShowMessage(message, messageColor, feedbackDuration);
    }

    private void PlayClip(AudioClip clip)
    {
        if (interactionAudioSource != null && clip != null)
        {
            interactionAudioSource.PlayOneShot(clip);
        }
    }

    private void SetReaderColor(Color targetColor)
    {
        if (readerRenderer == null)
        {
            return;
        }

        readerRenderer.GetPropertyBlock(materialPropertyBlock);

        if (!string.IsNullOrWhiteSpace(colorPropertyName))
        {
            materialPropertyBlock.SetColor(colorPropertyName, targetColor);
        }

        if (!string.IsNullOrWhiteSpace(fallbackColorPropertyName))
        {
            materialPropertyBlock.SetColor(fallbackColorPropertyName, targetColor);
        }

        if (driveEmissionColor && !string.IsNullOrWhiteSpace(emissionColorPropertyName))
        {
            Color emissionColor = targetColor * Mathf.Max(0f, emissionIntensityMultiplier);
            materialPropertyBlock.SetColor(emissionColorPropertyName, emissionColor);
        }

        readerRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private bool WasInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return false;
        }

        if (!TryMapKeyCodeToInputSystemKey(interactKey, out Key mappedKey))
        {
            return false;
        }

        var keyControl = Keyboard.current[mappedKey];
        return keyControl != null && keyControl.wasPressedThisFrame;
#else
        return Input.GetKeyDown(interactKey);
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private bool TryMapKeyCodeToInputSystemKey(KeyCode keyCode, out Key key)
    {
        switch (keyCode)
        {
            case KeyCode.Alpha0:
                key = Key.Digit0;
                return true;
            case KeyCode.Alpha1:
                key = Key.Digit1;
                return true;
            case KeyCode.Alpha2:
                key = Key.Digit2;
                return true;
            case KeyCode.Alpha3:
                key = Key.Digit3;
                return true;
            case KeyCode.Alpha4:
                key = Key.Digit4;
                return true;
            case KeyCode.Alpha5:
                key = Key.Digit5;
                return true;
            case KeyCode.Alpha6:
                key = Key.Digit6;
                return true;
            case KeyCode.Alpha7:
                key = Key.Digit7;
                return true;
            case KeyCode.Alpha8:
                key = Key.Digit8;
                return true;
            case KeyCode.Alpha9:
                key = Key.Digit9;
                return true;
        }

        return Enum.TryParse(keyCode.ToString(), true, out key);
    }
#endif
}
