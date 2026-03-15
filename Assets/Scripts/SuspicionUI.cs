using UnityEngine;
using UnityEngine.UI;

public class SuspicionUI : MonoBehaviour
{
    public DisguiseSystem disguiseSystem;
    public RectTransform suspicionFill;
    public Image suspicionFillImage;
    public Text clearanceLabel;
    public Text alertStateLabel;
    public Text zoneLabel;
    public Text disguiseLabel;
    public float barLerpSpeed = 8f;

    private float displayedSuspicion;

    void Start()
    {
        if (disguiseSystem == null)
            disguiseSystem = FindFirstObjectByType<DisguiseSystem>();

        if (suspicionFill == null)
            FindFillByName("SuspicionFill");

        if (alertStateLabel == null || clearanceLabel == null)
            FindLabelsByName();

        displayedSuspicion = 0f;
        if (suspicionFill != null)
            suspicionFill.localScale = new Vector3(0f, 1f, 1f);
    }

    void Update()
    {
        if (disguiseSystem == null) return;

        UpdateSuspicionBar();
        UpdateLabels();
    }

    private void FindFillByName(string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null) return;

        suspicionFill = go.GetComponent<RectTransform>();
        suspicionFillImage = go.GetComponent<Image>();
        if (suspicionFill != null)
            suspicionFill.pivot = new Vector2(0f, 0.5f);
    }

    private void FindLabelsByName()
    {
        GameObject suspicionLabel = GameObject.Find("SuspicionLabel");
        if (suspicionLabel != null && alertStateLabel == null)
            alertStateLabel = suspicionLabel.GetComponent<Text>();

        GameObject disguiseLabel = GameObject.Find("DisguiseLabel");
        if (disguiseLabel != null && clearanceLabel == null)
            clearanceLabel = disguiseLabel.GetComponent<Text>();
    }

    private void UpdateSuspicionBar()
    {
        float target = disguiseSystem.SuspicionNormalized;
        displayedSuspicion = Mathf.Lerp(displayedSuspicion, target, Time.deltaTime * barLerpSpeed);
        if (Mathf.Abs(displayedSuspicion - target) < 0.001f)
            displayedSuspicion = target;

        if (suspicionFill != null)
            suspicionFill.localScale = new Vector3(Mathf.Clamp01(displayedSuspicion), 1f, 1f);

        if (suspicionFillImage != null)
        {
            if (displayedSuspicion >= 0.8f)
                suspicionFillImage.color = new Color(0.9f, 0.2f, 0.15f, 1f);
            else if (displayedSuspicion >= 0.5f)
                suspicionFillImage.color = new Color(0.9f, 0.5f, 0.05f, 1f);
            else if (displayedSuspicion >= 0.2f)
                suspicionFillImage.color = new Color(0.9f, 0.7f, 0.1f, 1f);
            else
                suspicionFillImage.color = new Color(0.3f, 0.7f, 0.3f, 1f);
        }
    }

    private void UpdateLabels()
    {
        if (clearanceLabel != null)
        {
            if (disguiseSystem.IsDisguised)
            {
                string cl = GetGuardLabel(disguiseSystem.CurrentClearance).ToUpper();
                clearanceLabel.text = $"DISGUISE: <color=#88CCFF>{cl}</color>";
            }
            else
            {
                clearanceLabel.text = "DISGUISE: <color=#667788>NONE</color>";
            }
        }

        if (disguiseLabel != null)
        {
            if (disguiseSystem.IsDisguised && disguiseSystem.currentOutfit != null)
            {
                string outfitName = disguiseSystem.currentOutfit.name
                    .Replace("DisguiseOutfit_", "")
                    .Replace("_", " ")
                    .ToUpper();
                disguiseLabel.text = $"DISGUISE: <color=#88CCFF>{outfitName}</color>";
            }
            else if (disguiseSystem.IsChanging)
            {
                disguiseLabel.text = "DISGUISE: <color=#FFCC00>CHANGING...</color>";
            }
            else
            {
                disguiseLabel.text = "DISGUISE: <color=#667788>NONE</color>";
            }
        }

        if (alertStateLabel != null)
        {
            float s = disguiseSystem.SuspicionNormalized;
            if (s >= 0.8f)
                alertStateLabel.text = "<color=#FF3333>ALERT</color>";
            else if (s >= 0.5f)
                alertStateLabel.text = "<color=#FF8800>CAUTION</color>";
            else if (s >= 0.2f)
                alertStateLabel.text = "<color=#FFCC00>WATCHED</color>";
            else
                alertStateLabel.text = "SUSPICION";
        }

        if (zoneLabel != null)
        {
            SecurityZone zone = disguiseSystem.CurrentZone;
            if (zone != null)
            {
                bool hasAccess = disguiseSystem.CurrentClearance >= zone.RequiredClearance;
                string color = hasAccess ? "#88FF88" : "#FF4444";
                string suffix = hasAccess ? "" : "  ▲ RESTRICTED";
                zoneLabel.text = $"<color={color}>{zone.ZoneName}{suffix}</color>";
            }
            else
            {
                zoneLabel.text = "";
            }
        }
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
}
