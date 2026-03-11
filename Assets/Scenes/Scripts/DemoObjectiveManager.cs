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
            new DemoObjectiveStep { id = "crouch", text = "Objective: Crouch behind the large crate. (Press C)" },
            new DemoObjectiveStep { id = "peek", text = "Objective: Peek around the corner (Press Q). Do not get caught." },
            new DemoObjectiveStep { id = "takedown", text = "Objective: Take down the dock worker." },
            new DemoObjectiveStep { id = "disguise", text = "Objective: Swap into a disguise" },
            new DemoObjectiveStep { id = "idcard", text = "Objective: Retrieve the Dock Supervisor ID Card from the locker" },
            new DemoObjectiveStep { id = "exit", text = "Objective: Use the ID Card on the main door" }
        };
    }

    void Start()
    {
        ResolvePlayerRefs();

        if (objectiveTextTMP == null)
        {
            objectiveTextTMP = FindObjectOfType<TMP_Text>();
        }

        if (objectiveTextLegacy == null)
        {
            objectiveTextLegacy = FindObjectOfType<Text>();
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
            playerInput = FindObjectOfType<CharacterInputController>();
        }

        if (playerPeek == null)
        {
            playerPeek = FindObjectOfType<PeekSystem>();
        }
    }

    private void EnsureHud()
    {
        hudCanvas = FindObjectOfType<Canvas>();

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