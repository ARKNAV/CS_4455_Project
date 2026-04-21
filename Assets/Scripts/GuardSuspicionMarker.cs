using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GuardAI))]
public class GuardSuspicionMarker : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Vertical offset above the guard's pivot")]
    [SerializeField] float heightOffset = 2.2f;

    [Tooltip("Amplitude of the vertical bob in world units")]
    [SerializeField] float bobAmplitude = 0.1f;

    [Tooltip("Bob cycles per second (higher = faster)")]
    [SerializeField] float bobSpeed = 3f;

    [Tooltip("World-space scale applied to the canvas")]
    [SerializeField] float canvasScale = 0.01f;

    [Header("Appearance")]
    [SerializeField] string markerText = "!";
    [SerializeField] int fontSize = 120;
    [SerializeField] Color investigateColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] Color chaseColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Tooltip("Extra scale multiplier while chasing (1 = identical size)")]
    [SerializeField] float chaseScaleMultiplier = 1.25f;

    private GuardAI _guard;
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private Text _text;
    private Outline _outline;
    private Camera _cam;

    void Awake()
    {
        _guard = GetComponent<GuardAI>();
        BuildMarker();
    }

    void LateUpdate()
    {
        if (_guard == null || _canvas == null) return;

        bool suspicious = _guard.IsSuspicious && !_guard.IsBeingTakenDown;
        if (_canvas.gameObject.activeSelf != suspicious)
            _canvas.gameObject.SetActive(suspicious);

        if (!suspicious) return;

        bool chasing = _guard.IsChasing;
        if (_text != null)
            _text.color = chasing ? chaseColor : investigateColor;

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        Vector3 basePos = transform.position + Vector3.up * (heightOffset + bob);
        _canvasRect.position = basePos;

        float scale = canvasScale * (chasing ? chaseScaleMultiplier : 1f);
        _canvasRect.localScale = new Vector3(scale, scale, scale);

        Camera cam = GetCamera();
        if (cam != null)
        {
            Vector3 toCam = _canvasRect.position - cam.transform.position;
            if (toCam.sqrMagnitude > 0.0001f)
                _canvasRect.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }
    }

    private Camera GetCamera()
    {
        if (_cam != null && _cam.isActiveAndEnabled) return _cam;
        _cam = Camera.main;
        return _cam;
    }

    private void BuildMarker()
    {
        GameObject canvasGo = new GameObject("SuspicionMarkerCanvas");
        canvasGo.transform.SetParent(null, worldPositionStays: true);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 100;
        _canvasRect = canvasGo.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = new Vector2(128f, 160f);
        _canvasRect.localScale = Vector3.one * canvasScale;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, worldPositionStays: false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        _text = textGo.AddComponent<Text>();
        _text.text = markerText;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.fontSize = fontSize;
        _text.fontStyle = FontStyle.Bold;
        _text.color = investigateColor;
        _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        _text.verticalOverflow = VerticalWrapMode.Overflow;
        _text.raycastTarget = false;
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _outline = textGo.AddComponent<Outline>();
        _outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        _outline.effectDistance = new Vector2(2f, -2f);

        _canvas.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_canvas != null)
            Destroy(_canvas.gameObject);
    }
}
