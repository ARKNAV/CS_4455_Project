using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionFeedbackHUD : MonoBehaviour
{
    public static InteractionFeedbackHUD Instance { get; private set; }

    [Header("Optional Existing HUD Text")]
    [SerializeField] private TMP_Text feedbackTextTMP;
    [SerializeField] private Text feedbackTextLegacy;

    [Header("Runtime HUD")]
    [SerializeField] private bool createHudAtRuntime = true;
    [SerializeField] private Vector2 anchoredOffset = new Vector2(-20f, -20f);
    [SerializeField] private int fontSize = 24;
    [SerializeField] private Color defaultTextColor = Color.white;

    private Canvas cachedCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (!HasAnyHudReference() && createHudAtRuntime)
        {
            CreateLegacyHud();
        }

        SetText(string.Empty, defaultTextColor);
    }

    public void ShowMessage(string message, float durationSeconds = 2f)
    {
        ShowMessage(message, defaultTextColor, durationSeconds);
    }

    public void ShowMessage(string message, Color color, float durationSeconds = 2f)
    {
        SetText(message, color);

        if (durationSeconds <= 0f)
        {
            return;
        }

        CancelInvoke(nameof(ClearMessage));
        Invoke(nameof(ClearMessage), durationSeconds);
    }

    public void ClearMessage()
    {
        SetText(string.Empty, defaultTextColor);
    }

    private bool HasAnyHudReference()
    {
        return feedbackTextTMP != null || feedbackTextLegacy != null;
    }

    private void SetText(string message, Color color)
    {
        if (feedbackTextTMP != null)
        {
            feedbackTextTMP.text = message;
            feedbackTextTMP.color = color;
            return;
        }

        if (feedbackTextLegacy != null)
        {
            feedbackTextLegacy.text = message;
            feedbackTextLegacy.color = color;
        }
    }

    private void CreateLegacyHud()
    {
        cachedCanvas = FindFirstObjectByType<Canvas>();
        if (cachedCanvas == null)
        {
            GameObject canvasGo = new GameObject("InteractionFeedbackCanvas");
            cachedCanvas = canvasGo.AddComponent<Canvas>();
            cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        GameObject textGo = new GameObject("InteractionFeedbackText");
        textGo.transform.SetParent(cachedCanvas.transform, false);

        feedbackTextLegacy = textGo.AddComponent<Text>();
        Font builtInFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (builtInFont == null)
        {
            Debug.LogWarning("InteractionFeedbackHUD could not load built-in Arial font. Assign a TMP/legacy HUD text in the Inspector.");
            Destroy(textGo);
            feedbackTextLegacy = null;
            return;
        }

        feedbackTextLegacy.font = builtInFont;
        feedbackTextLegacy.fontSize = fontSize;
        feedbackTextLegacy.color = defaultTextColor;
        feedbackTextLegacy.alignment = TextAnchor.UpperRight;
        feedbackTextLegacy.horizontalOverflow = HorizontalWrapMode.Wrap;
        feedbackTextLegacy.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rect = feedbackTextLegacy.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = anchoredOffset;
        rect.sizeDelta = new Vector2(700f, 120f);
    }
}
