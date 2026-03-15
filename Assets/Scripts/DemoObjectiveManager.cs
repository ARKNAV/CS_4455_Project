using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class DemoObjectiveStep
{
    public string id;
    [TextArea]
    public string text;
}

public class DemoObjectiveManager : MonoBehaviour
{
    public static DemoObjectiveManager Instance { get; private set; }

    [Header("Objective Order")]
    [SerializeField] private List<DemoObjectiveStep> objectiveSteps = new List<DemoObjectiveStep>();

    [Header("Player References (Optional)")]
    [SerializeField] private CharacterInputController playerInput;
    [SerializeField] private PeekSystem playerPeek;

    [Header("HUD")]
    [SerializeField] private TMP_Text objectiveTextTMP;
    [SerializeField] private Text objectiveTextLegacy;
    [SerializeField] private bool createHudAtRuntime = true;
    [SerializeField] private Vector2 hudOffset = new Vector2(20f, -20f);
    [SerializeField] private int fontSize = 28;
    [SerializeField] private Color textColor = Color.white;

    private int currentIndex;
    private Canvas hudCanvas;

    public event Action<string> OnObjectiveChanged;
    public event Action<string> OnObjectiveCompleted;
    public event Action OnAllObjectivesCompleted;

    public bool IsFinished
    {
        get { return currentIndex >= objectiveSteps.Count; }
    }

    public string CurrentObjectiveId
    {
        get
        {
            if (IsFinished)
            {
                return string.Empty;
            }

            return objectiveSteps[currentIndex].id;
        }
    }

    public string CurrentObjectiveText
    {
        get
        {
            if (IsFinished)
            {
                return "Objective Complete";
            }

            return objectiveSteps[currentIndex].text;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Reset()
    {
        objectiveSteps = new List<DemoObjectiveStep>
        {
            new DemoObjectiveStep
            {
                id   = "crouch",
                text = "<color=#FFCC44><b>▶  STAY HIDDEN</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Crouch behind the large crate\n" +
                       "<color=#556677><size=11>[LEFT CTRL]  to crouch</size></color>"
            },
            new DemoObjectiveStep
            {
                id   = "peek",
                text = "<color=#FFCC44><b>▶  OBSERVE UNSEEN</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Peek around the corner — do not get spotted\n" +
                       "<color=#556677><size=11>[Q] peek left   [E] peek right</size></color>"
            },
            new DemoObjectiveStep
            {
                id   = "takedown",
                text = "<color=#FF6644><b>▶  NEUTRALIZE TARGET</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Silently take down the dock guard\n" +
                       "<color=#556677><size=11>[F]  near a guard to execute takedown</size></color>"
            },
            new DemoObjectiveStep
            {
                id   = "disguise",
                text = "<color=#44CCFF><b>▶  CHANGE IDENTITY</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Acquire a guard disguise\n" +
                       "<color=#556677><size=11>[F]  near disguise box  —or—  near downed guard</size></color>"
            },
            new DemoObjectiveStep
            {
                id   = "idcard",
                text = "<color=#FFCC44><b>▶  RETRIEVE CREDENTIALS</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Locate the Dock Supervisor ID Card\n" +
                       "<color=#556677><size=11>[F]  to pick up the keycard</size></color>"
            },
            new DemoObjectiveStep
            {
                id   = "exit",
                text = "<color=#44FF88><b>▶  BREACH THE DOOR</b></color>\n" +
                       "<color=#888888>──────────────────────────</color>\n" +
                       "Use the ID Card on the security reader\n" +
                       "<color=#556677><size=11>[F]  on the keycard panel to unlock</size></color>"
            }
        };
    }

    void Start()
    {
        ResolvePlayerRefs();

        if (objectiveTextTMP == null)
        {
            objectiveTextTMP = FindFirstObjectByType<TMP_Text>();
        }

        if (objectiveTextLegacy == null)
        {
            objectiveTextLegacy = FindFirstObjectByType<Text>();
        }

        if (!HasManualHudReference() && createHudAtRuntime)
        {
            EnsureHud();
        }

        if (!HasAnyHudReference())
        {
            Debug.LogWarning("DemoObjectiveManager: No HUD text assigned. Assign Objective Text TMP or Objective Text Legacy in the inspector.", this);
        }

        RefreshHud();
    }

    void Update()
    {
        if (IsFinished)
        {
            return;
        }

        if (CurrentObjectiveId == "crouch" && playerInput != null && playerInput.IsCrouching)
        {
            CompleteObjective("crouch");
        }
        else if (CurrentObjectiveId == "peek" && playerPeek != null && playerPeek.IsPeeking)
        {
            CompleteObjective("peek");
        }
    }

    public bool IsCurrentObjective(string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId) || IsFinished)
        {
            return false;
        }

        return string.Equals(CurrentObjectiveId, objectiveId, StringComparison.OrdinalIgnoreCase);
    }

    public bool CompleteObjective(string objectiveId)
    {
        if (!IsCurrentObjective(objectiveId))
        {
            return false;
        }

        string completedId = CurrentObjectiveId;
        currentIndex++;

        OnObjectiveCompleted?.Invoke(completedId);

        if (IsFinished)
        {
            RefreshHud();
            OnAllObjectivesCompleted?.Invoke();
            return true;
        }

        RefreshHud();
        OnObjectiveChanged?.Invoke(CurrentObjectiveId);
        return true;
    }

    private void ResolvePlayerRefs()
    {
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<CharacterInputController>();
        }

        if (playerPeek == null)
        {
            playerPeek = FindFirstObjectByType<PeekSystem>();
        }
    }

    private void EnsureHud()
    {
        hudCanvas = FindFirstObjectByType<Canvas>();

        if (hudCanvas == null)
        {
            GameObject canvasGo = new GameObject("ObjectiveCanvas");
            hudCanvas = canvasGo.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (objectiveTextLegacy == null)
        {
            GameObject textGo = new GameObject("ObjectiveText");
            textGo.transform.SetParent(hudCanvas.transform, false);
            objectiveTextLegacy = textGo.AddComponent<Text>();

            RectTransform rect = objectiveTextLegacy.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = hudOffset;
            rect.sizeDelta = new Vector2(900f, 120f);

            objectiveTextLegacy.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            objectiveTextLegacy.fontSize = fontSize;
            objectiveTextLegacy.color = textColor;
            objectiveTextLegacy.alignment = TextAnchor.UpperLeft;
            objectiveTextLegacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            objectiveTextLegacy.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    private void RefreshHud()
    {
        if (objectiveTextTMP != null)
        {
            objectiveTextTMP.text = CurrentObjectiveText;
            return;
        }

        if (objectiveTextLegacy != null)
        {
            objectiveTextLegacy.text = CurrentObjectiveText;
            return;
        }
    }

    private bool HasManualHudReference()
    {
        return objectiveTextTMP != null || objectiveTextLegacy != null;
    }

    private bool HasAnyHudReference()
    {
        if (objectiveTextTMP != null || objectiveTextLegacy != null)
        {
            return true;
        }

        return false;
    }
}