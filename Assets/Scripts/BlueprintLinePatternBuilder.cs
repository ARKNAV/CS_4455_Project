using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlueprintLinePatternBuilder : MonoBehaviour
{
    [System.Serializable]
    public class LineGroupTarget
    {
        public RectTransform groupRoot;
        public string groupLabel = "";
    }

    [Header("Targets")]
    [SerializeField] private LineGroupTarget[] lineGroups;

    [Header("Style")]
    [SerializeField] private Color lineColor = new Color(0.67f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color labelColor = new Color(0.8f, 0.95f, 1f, 0.95f);
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField] private int labelFontSize = 18;

    [Header("Behavior")]
    [SerializeField] private bool regenerateOnStart;

    private const string GeneratedPrefix = "GEN_";

    private struct Segment
    {
        public Vector2 position;
        public float length;
        public float angle;

        public Segment(float x, float y, float len, float ang)
        {
            position = new Vector2(x, y);
            length = len;
            angle = ang;
        }
    }

    private void Start()
    {
        if (regenerateOnStart)
        {
            GeneratePatterns();
        }
    }

    [ContextMenu("Generate Blueprint Patterns")]
    public void GeneratePatterns()
    {
        if (lineGroups == null)
        {
            return;
        }

        for (int i = 0; i < lineGroups.Length; ++i)
        {
            LineGroupTarget target = lineGroups[i];
            if (target == null || target.groupRoot == null)
            {
                continue;
            }

            ClearGeneratedChildren(target.groupRoot);
            BuildPreset(i, target);
        }
    }

    [ContextMenu("Clear Generated Blueprint Patterns")]
    public void ClearPatterns()
    {
        if (lineGroups == null)
        {
            return;
        }

        for (int i = 0; i < lineGroups.Length; ++i)
        {
            LineGroupTarget target = lineGroups[i];
            if (target == null || target.groupRoot == null)
            {
                continue;
            }

            ClearGeneratedChildren(target.groupRoot);
        }
    }

    private void BuildPreset(int index, LineGroupTarget target)
    {
        int preset = index % 6;
        switch (preset)
        {
            case 0:
                BuildCoreRoomPattern(target.groupRoot, target.groupLabel);
                break;
            case 1:
                BuildPowerRoutingPattern(target.groupRoot, target.groupLabel);
                break;
            case 2:
                BuildConduitGridPattern(target.groupRoot, target.groupLabel);
                break;
            case 3:
                BuildShaftPattern(target.groupRoot, target.groupLabel);
                break;
            case 4:
                BuildNodeClusterPattern(target.groupRoot, target.groupLabel);
                break;
            default:
                BuildAngularMeshPattern(target.groupRoot, target.groupLabel);
                break;
        }
    }

    private void BuildCoreRoomPattern(RectTransform root, string customLabel)
    {
        DrawBox(root, new Vector2(0f, 0f), 260f, 150f);
        DrawBox(root, new Vector2(0f, 0f), 130f, 75f);
        DrawSegment(root, new Segment(-165f, 0f, 70f, 0f));
        DrawSegment(root, new Segment(165f, 0f, 70f, 0f));
        DrawSegment(root, new Segment(0f, 95f, 70f, 90f));
        DrawSegment(root, new Segment(0f, -95f, 70f, 90f));
        DrawLabel(root, new Vector2(-105f, 95f), ResolveLabel(customLabel, "CORE A1"));
    }

    private void BuildPowerRoutingPattern(RectTransform root, string customLabel)
    {
        Segment[] segments =
        {
            new Segment(-170f, 60f, 120f, 0f),
            new Segment(-110f, 20f, 80f, 90f),
            new Segment(-40f, 20f, 140f, 0f),
            new Segment(40f, -25f, 90f, 90f),
            new Segment(95f, -25f, 125f, 0f),
            new Segment(152f, 35f, 120f, 90f),
            new Segment(35f, 95f, 85f, 0f),
            new Segment(-35f, 95f, 60f, 90f),
            new Segment(-35f, 125f, 75f, 0f)
        };

        DrawSegments(root, segments);
        DrawLabel(root, new Vector2(-185f, 105f), ResolveLabel(customLabel, "POWER BUS"));
    }

    private void BuildConduitGridPattern(RectTransform root, string customLabel)
    {
        for (int i = -2; i <= 2; ++i)
        {
            DrawSegment(root, new Segment(i * 60f, 0f, 200f, 90f));
        }

        for (int i = -2; i <= 2; ++i)
        {
            DrawSegment(root, new Segment(0f, i * 45f, 280f, 0f));
        }

        DrawSegment(root, new Segment(-120f, 90f, 85f, 32f));
        DrawSegment(root, new Segment(120f, -90f, 85f, 32f));
        DrawLabel(root, new Vector2(-145f, 125f), ResolveLabel(customLabel, "CONDUIT GRID"));
    }

    private void BuildShaftPattern(RectTransform root, string customLabel)
    {
        DrawBox(root, new Vector2(0f, 0f), 90f, 220f);
        DrawSegment(root, new Segment(0f, 120f, 170f, 0f));
        DrawSegment(root, new Segment(0f, -120f, 170f, 0f));
        DrawSegment(root, new Segment(-130f, 120f, 80f, 90f));
        DrawSegment(root, new Segment(130f, -120f, 80f, 90f));
        DrawSegment(root, new Segment(130f, 120f, 80f, 90f));
        DrawSegment(root, new Segment(-130f, -120f, 80f, 90f));
        DrawLabel(root, new Vector2(-140f, 150f), ResolveLabel(customLabel, "SHAFT B2"));
    }

    private void BuildNodeClusterPattern(RectTransform root, string customLabel)
    {
        Vector2[] nodes =
        {
            new Vector2(-120f, 80f), new Vector2(-40f, 40f), new Vector2(35f, 90f),
            new Vector2(120f, 15f), new Vector2(65f, -75f), new Vector2(-40f, -90f), new Vector2(-130f, -25f)
        };

        for (int i = 0; i < nodes.Length; ++i)
        {
            DrawNode(root, nodes[i]);
        }

        DrawConnection(root, nodes[0], nodes[1]);
        DrawConnection(root, nodes[1], nodes[2]);
        DrawConnection(root, nodes[2], nodes[3]);
        DrawConnection(root, nodes[3], nodes[4]);
        DrawConnection(root, nodes[4], nodes[5]);
        DrawConnection(root, nodes[5], nodes[6]);
        DrawConnection(root, nodes[6], nodes[0]);
        DrawConnection(root, nodes[1], nodes[5]);

        DrawLabel(root, new Vector2(-140f, 130f), ResolveLabel(customLabel, "NODE CLUSTER"));
    }

    private void BuildAngularMeshPattern(RectTransform root, string customLabel)
    {
        Segment[] segments =
        {
            new Segment(-120f, -60f, 170f, 20f),
            new Segment(-60f, 10f, 180f, -20f),
            new Segment(20f, 80f, 160f, 18f),
            new Segment(120f, -30f, 190f, -38f),
            new Segment(-10f, -100f, 220f, 0f),
            new Segment(-160f, 35f, 120f, 90f),
            new Segment(155f, 42f, 115f, 90f)
        };

        DrawSegments(root, segments);
        DrawLabel(root, new Vector2(-150f, 120f), ResolveLabel(customLabel, "MESH ROUTE"));
    }

    private void DrawSegments(RectTransform parent, Segment[] segments)
    {
        for (int i = 0; i < segments.Length; ++i)
        {
            DrawSegment(parent, segments[i]);
        }
    }

    private void DrawSegment(RectTransform parent, Segment segment)
    {
        GameObject go = CreateElementObject(parent, GeneratedPrefix + "Line");
        Image image = go.AddComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = segment.position;
        rect.sizeDelta = new Vector2(segment.length, Mathf.Max(1f, lineThickness));
        rect.localRotation = Quaternion.Euler(0f, 0f, segment.angle);
    }

    private void DrawBox(RectTransform parent, Vector2 center, float width, float height)
    {
        DrawSegment(parent, new Segment(center.x, center.y + (height * 0.5f), width, 0f));
        DrawSegment(parent, new Segment(center.x, center.y - (height * 0.5f), width, 0f));
        DrawSegment(parent, new Segment(center.x - (width * 0.5f), center.y, height, 90f));
        DrawSegment(parent, new Segment(center.x + (width * 0.5f), center.y, height, 90f));
    }

    private void DrawNode(RectTransform parent, Vector2 position)
    {
        GameObject go = CreateElementObject(parent, GeneratedPrefix + "Node");
        Image image = go.AddComponent<Image>();
        image.color = lineColor;
        image.raycastTarget = false;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(8f, 8f);
    }

    private void DrawConnection(RectTransform parent, Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Vector2 center = (from + to) * 0.5f;
        DrawSegment(parent, new Segment(center.x, center.y, length, angle));
    }

    private void DrawLabel(RectTransform parent, Vector2 position, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        GameObject go = CreateElementObject(parent, GeneratedPrefix + "Label");
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = labelColor;
        tmp.fontSize = Mathf.Max(8, labelFontSize);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;

        if (labelFont != null)
        {
            tmp.font = labelFont;
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(260f, 32f);
    }

    private string ResolveLabel(string customLabel, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(customLabel))
        {
            return customLabel;
        }

        return fallback;
    }

    private void ClearGeneratedChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; --i)
        {
            Transform child = parent.GetChild(i);
            if (!child.name.StartsWith(GeneratedPrefix))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
#endif
            }
        }
    }

    private GameObject CreateElementObject(RectTransform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}
