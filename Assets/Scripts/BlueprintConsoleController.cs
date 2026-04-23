using System;
using System.Collections;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Collider))]
public class BlueprintConsoleController : MonoBehaviour
{
    public static Action WinTriggerEvent;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactionDistance = 3f;

    [Header("Requirements")]
    [SerializeField] private int requiredMissionKeys = 2;

    [Header("Objective")]
    [SerializeField] private bool completeObjectiveOnSuccess = true;
    [SerializeField] private string blueprintObjectiveId = "collect_blueprints";

    [Header("Feedback")]
    [SerializeField] private string interactPrompt = "Press F to access blueprint console";
    [SerializeField] private string lockedMessage = "Center console locked: collect both keys.";
    [SerializeField] private string successMessage = "Blueprints secured.";
    [SerializeField] private float feedbackDuration = 2f;

    [Header("Blueprint Download FX (DOTween)")]
    [SerializeField] private bool playDownloadAnimation = true;
    [SerializeField] private CanvasGroup blueprintDownloadCanvas;
    [SerializeField] private Canvas[] canvasesToHideWhileDownload;
    [SerializeField] private GameObject[] uiObjectsToHideWhileDownload;
    [SerializeField] private Image downloadProgressFill;
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private RectTransform scanLine;
    [SerializeField] private CanvasGroup[] blueprintLineGroups;
    [SerializeField] private float introFadeDuration = 0.25f;
    [SerializeField] private float downloadDuration = 4f;
    [SerializeField] private float outroFadeDuration = 0.2f;
    [SerializeField] private string downloadingHeader = "DOWNLOADING BLUEPRINT PACKAGE";
    [SerializeField] private string completeHeader = "BLUEPRINT PACKAGE SECURED";
    [SerializeField] private string downloadingStatus = "Decrypting and reconstructing schematics...";
    [SerializeField] private string completeStatus = "Transfer complete. Exfiltration objective achieved.";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip accessDeniedClip;
    [SerializeField] private AudioClip accessGrantedClip;

    [Header("Victory Animation (Optional)")]
    [SerializeField] private bool triggerPlayerVictoryAnimation = true;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string victoryTriggerName = "Victory";
    [SerializeField] private float delayBeforeWinEvent = 1f;

    private Transform playerTransform;
    private Transform cachedPlayerByTag;
    private bool playerInRange;
    private bool promptVisible;
    private bool used;
    private bool interactionBusy;
    private bool missionFailed;
    private Tween scanLineTween;
    private int victoryTriggerHash;
    private bool[] cachedCanvasStates;
    private bool[] cachedUiObjectStates;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        victoryTriggerHash = Animator.StringToHash(victoryTriggerName);

        if (blueprintDownloadCanvas != null)
        {
            blueprintDownloadCanvas.alpha = 0f;
            blueprintDownloadCanvas.interactable = false;
            blueprintDownloadCanvas.blocksRaycasts = false;
            blueprintDownloadCanvas.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameManager.LoseTriggerEvent += OnMissionFailed;
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Update()
    {
        if (missionFailed)
        {
            HidePrompt();
            return;
        }

        if (used || interactionBusy)
        {
            HidePrompt();
            return;
        }

        bool inRange = IsPlayerWithinInteractionRange();
        if (!inRange)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (!WasKeyPressedThisFrame(interactKey))
        {
            return;
        }

        StartCoroutine(TryUseConsoleFlow());
    }

    private bool IsPlayerWithinInteractionRange()
    {
        Transform player = playerTransform;
        if (player == null)
        {
            if (cachedPlayerByTag == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
                if (playerObject != null)
                {
                    cachedPlayerByTag = playerObject.transform;
                }
            }

            player = cachedPlayerByTag;
        }

        if (player == null)
        {
            playerInRange = false;
            return false;
        }

        float maxDistance = Mathf.Max(0.1f, interactionDistance);
        float sqrDistance = (transform.position - player.position).sqrMagnitude;
        bool inRange = sqrDistance <= maxDistance * maxDistance;
        playerInRange = inRange;
        if (inRange)
        {
            playerTransform = player;
        }

        return inRange;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        playerTransform = other.transform;

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
        playerTransform = null;
        HidePrompt();
    }

    private IEnumerator TryUseConsoleFlow()
    {
        if (missionFailed)
        {
            yield break;
        }

        interactionBusy = true;

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            if (playerTransform != null)
            {
                inventory = playerTransform.GetComponentInParent<PlayerInventory>();
            }
        }

        if (inventory == null)
        {
            Debug.LogWarning("BlueprintConsoleController: No PlayerInventory found. Add PlayerInventory to the player.", this);
            interactionBusy = false;
            yield break;
        }

        int requiredKeys = Mathf.Max(0, requiredMissionKeys);
        if (inventory.MissionKeysCollected < requiredKeys)
        {
            ShowMessage(lockedMessage, feedbackDuration);
            PlayClip(accessDeniedClip);
            interactionBusy = false;
            yield break;
        }

        if (!inventory.CollectBlueprints())
        {
            interactionBusy = false;
            yield break;
        }

        used = true;
        HidePrompt();

        if (completeObjectiveOnSuccess && DemoObjectiveManager.Instance != null)
        {
            DemoObjectiveManager manager = DemoObjectiveManager.Instance;
            if (manager.IsCurrentObjective(blueprintObjectiveId))
            {
                manager.CompleteObjective(blueprintObjectiveId);
            }
        }

        PlayClip(accessGrantedClip);

        if (playDownloadAnimation)
        {
            yield return StartCoroutine(PlayBlueprintDownloadSequence());
        }

        if (missionFailed || (GameManager.Instance != null && GameManager.Instance.MissionFailed))
        {
            interactionBusy = false;
            yield break;
        }

        TryPlayVictoryAnimation();

        float delay = Mathf.Max(0f, delayBeforeWinEvent);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        ShowMessage(successMessage, feedbackDuration);
        WinTriggerEvent?.Invoke();
        interactionBusy = false;
    }

    private void TryPlayVictoryAnimation()
    {
        if (!triggerPlayerVictoryAnimation || string.IsNullOrWhiteSpace(victoryTriggerName))
        {
            return;
        }

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponentInParent<Animator>();
        }

        if (playerAnimator == null)
        {
            return;
        }

        playerAnimator.ResetTrigger(victoryTriggerHash);
        playerAnimator.SetTrigger(victoryTriggerHash);
    }

    private IEnumerator PlayBlueprintDownloadSequence()
    {
        if (blueprintDownloadCanvas == null)
        {
            yield break;
        }

        CachePanelVisibilityStates();
        SetOtherPanelsVisible(false);

        blueprintDownloadCanvas.gameObject.SetActive(true);
        blueprintDownloadCanvas.interactable = false;
        blueprintDownloadCanvas.blocksRaycasts = true;
        blueprintDownloadCanvas.alpha = 0f;

        if (downloadProgressFill != null)
        {
            downloadProgressFill.fillAmount = 0f;
        }

        if (headerText != null)
        {
            headerText.text = downloadingHeader;
        }

        if (statusText != null)
        {
            statusText.text = downloadingStatus;
        }

        if (percentText != null)
        {
            percentText.text = "0%";
        }

        for (int i = 0; i < blueprintLineGroups.Length; ++i)
        {
            if (blueprintLineGroups[i] == null)
            {
                continue;
            }

            blueprintLineGroups[i].alpha = 0f;
        }

        yield return blueprintDownloadCanvas.DOFade(1f, Mathf.Max(0.01f, introFadeDuration)).SetEase(Ease.OutSine).WaitForCompletion();

        if (scanLine != null)
        {
            Vector2 scanStart = scanLine.anchoredPosition;
            Vector2 scanEnd = new Vector2(scanStart.x, -scanStart.y);
            scanLineTween = DOTween.To(() => scanLine.anchoredPosition, value => scanLine.anchoredPosition = value, scanEnd, 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(this);
        }

        for (int i = 0; i < blueprintLineGroups.Length; ++i)
        {
            CanvasGroup lineGroup = blueprintLineGroups[i];
            if (lineGroup == null)
            {
                continue;
            }

            lineGroup.DOFade(1f, 0.3f).SetDelay(i * 0.2f).SetEase(Ease.OutSine).SetId(this);
        }

        if (downloadProgressFill != null)
        {
            Tween fillTween = downloadProgressFill.DOFillAmount(1f, Mathf.Max(0.1f, downloadDuration))
                .SetEase(Ease.Linear)
                .SetId(this)
                .OnUpdate(UpdatePercentText);

            yield return fillTween.WaitForCompletion();
        }
        else
        {
            float timer = 0f;
            float duration = Mathf.Max(0.1f, downloadDuration);
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / duration);
                UpdatePercentText(progress);
                yield return null;
            }
        }

        if (headerText != null)
        {
            headerText.text = completeHeader;
            headerText.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.6f).SetId(this);
        }

        if (statusText != null)
        {
            statusText.text = completeStatus;
        }

        if (percentText != null)
        {
            percentText.text = "100%";
        }

        if (scanLineTween != null)
        {
            scanLineTween.Kill();
            scanLineTween = null;
        }

        yield return new WaitForSeconds(0.35f);
        yield return blueprintDownloadCanvas.DOFade(0f, Mathf.Max(0.01f, outroFadeDuration)).SetEase(Ease.InSine).WaitForCompletion();

        blueprintDownloadCanvas.blocksRaycasts = false;
        blueprintDownloadCanvas.gameObject.SetActive(false);
        RestoreOtherPanelsVisibility();
    }

    private void CachePanelVisibilityStates()
    {
        if (canvasesToHideWhileDownload == null)
        {
            cachedCanvasStates = new bool[0];
        }
        else
        {
            cachedCanvasStates = new bool[canvasesToHideWhileDownload.Length];
            for (int i = 0; i < canvasesToHideWhileDownload.Length; ++i)
            {
                cachedCanvasStates[i] = canvasesToHideWhileDownload[i] != null && canvasesToHideWhileDownload[i].enabled;
            }
        }

        if (uiObjectsToHideWhileDownload == null)
        {
            cachedUiObjectStates = new bool[0];
            return;
        }

        cachedUiObjectStates = new bool[uiObjectsToHideWhileDownload.Length];
        for (int i = 0; i < uiObjectsToHideWhileDownload.Length; ++i)
        {
            cachedUiObjectStates[i] = uiObjectsToHideWhileDownload[i] != null && uiObjectsToHideWhileDownload[i].activeSelf;
        }
    }

    private void SetOtherPanelsVisible(bool visible)
    {
        if (canvasesToHideWhileDownload != null)
        {
            for (int i = 0; i < canvasesToHideWhileDownload.Length; ++i)
            {
                Canvas canvas = canvasesToHideWhileDownload[i];
                if (canvas == null)
                {
                    continue;
                }

                if (blueprintDownloadCanvas != null && canvas.gameObject == blueprintDownloadCanvas.gameObject)
                {
                    continue;
                }

                canvas.enabled = visible;
            }
        }

        if (uiObjectsToHideWhileDownload == null)
        {
            return;
        }

        for (int i = 0; i < uiObjectsToHideWhileDownload.Length; ++i)
        {
            GameObject uiObject = uiObjectsToHideWhileDownload[i];
            if (uiObject == null)
            {
                continue;
            }

            if (blueprintDownloadCanvas != null && uiObject == blueprintDownloadCanvas.gameObject)
            {
                continue;
            }

            uiObject.SetActive(visible);
        }
    }

    private void RestoreOtherPanelsVisibility()
    {
        if (canvasesToHideWhileDownload != null && cachedCanvasStates != null)
        {
            for (int i = 0; i < canvasesToHideWhileDownload.Length; ++i)
            {
                if (i >= cachedCanvasStates.Length)
                {
                    break;
                }

                Canvas canvas = canvasesToHideWhileDownload[i];
                if (canvas != null)
                {
                    canvas.enabled = cachedCanvasStates[i];
                }
            }
        }

        if (uiObjectsToHideWhileDownload == null || cachedUiObjectStates == null)
        {
            return;
        }

        for (int i = 0; i < uiObjectsToHideWhileDownload.Length; ++i)
        {
            if (i >= cachedUiObjectStates.Length)
            {
                break;
            }

            GameObject uiObject = uiObjectsToHideWhileDownload[i];
            if (uiObject != null)
            {
                uiObject.SetActive(cachedUiObjectStates[i]);
            }
        }
    }

    private void UpdatePercentText()
    {
        if (downloadProgressFill == null)
        {
            return;
        }

        UpdatePercentText(downloadProgressFill.fillAmount);
    }

    private void UpdatePercentText(float progress)
    {
        if (percentText != null)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
            percentText.text = percent + "%";
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
            InteractionFeedbackHUD.Instance.ShowMessage(interactPrompt, 0f);
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

    private void ShowMessage(string message, float duration)
    {
        if (InteractionFeedbackHUD.Instance != null)
        {
            InteractionFeedbackHUD.Instance.ShowMessage(message, duration);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
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

    private void OnDisable()
    {
        GameManager.LoseTriggerEvent -= OnMissionFailed;

        RestoreOtherPanelsVisibility();

        if (scanLineTween != null)
        {
            scanLineTween.Kill();
            scanLineTween = null;
        }

        DOTween.Kill(this);
    }

    private void OnMissionFailed()
    {
        missionFailed = true;
        interactionBusy = false;
        HidePrompt();

        StopAllCoroutines();
        ForceCloseBlueprintOverlay();
    }

    private void ForceCloseBlueprintOverlay()
    {
        if (scanLineTween != null)
        {
            scanLineTween.Kill();
            scanLineTween = null;
        }

        DOTween.Kill(this);

        if (blueprintDownloadCanvas != null)
        {
            blueprintDownloadCanvas.alpha = 0f;
            blueprintDownloadCanvas.interactable = false;
            blueprintDownloadCanvas.blocksRaycasts = false;
            blueprintDownloadCanvas.gameObject.SetActive(false);
        }

        RestoreOtherPanelsVisibility();
    }
}
