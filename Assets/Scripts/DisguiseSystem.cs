using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's disguise state, security clearance, and behavioral suspicion.
/// Attach to the player character.
///
/// Key features aligned with the project design doc:
///   • Security clearance tiers (Civilian → Commander)
///   • Behavioral suspicion modifiers (loitering near guards)
///   • Sims-style VFX during disguise change
///   • Integration with SecurityZone and GuardAI systems
///
/// Disguises are permanent once equipped (no degradation).
/// </summary>
public class DisguiseSystem : MonoBehaviour
{
    // ────────────────────────────────────────────
    //  Disguise Settings
    // ────────────────────────────────────────────
    [Header("Disguise Settings")]
    [Tooltip("Outfit data that maps guard torso materials to player upper-cloth slots")]
    public DisguiseOutfit currentOutfit;

    [Tooltip("Duration of the disguise-change animation")]
    public float disguiseDuration = 2.5f;

    [Tooltip("Current security clearance granted by the active disguise")]
    [SerializeField] private SecurityClearance currentClearance = SecurityClearance.Civilian;

    // ────────────────────────────────────────────
    //  Behavioral Suspicion Modifiers
    // ────────────────────────────────────────────
    [Header("Behavioral Suspicion")]
    [Tooltip("Extra suspicion per second when loitering near high-rank NPCs")]
    public float loiterSuspicionRate = 8f;

    [Tooltip("Radius to detect nearby high-rank NPCs for loitering check")]
    public float loiterCheckRadius = 5f;

    [Tooltip("Time (seconds) before loitering suspicion starts building")]
    public float loiterGracePeriod = 3f;

    // ────────────────────────────────────────────
    //  Global Suspicion Meter
    // ────────────────────────────────────────────
    [Header("Suspicion Meter")]
    [Tooltip("Current global suspicion level (0-100). At 100 the mission fails.")]
    [Range(0f, 100f)]
    public float currentSuspicion = 0f;

    [Tooltip("Rate at which suspicion decays per second when not being observed")]
    public float suspicionDecayRate = 5f;

    [Tooltip("Delay before suspicion starts decaying after last increase")]
    public float suspicionDecayDelay = 2f;

    /// <summary>Whether the player is currently disguised.</summary>
    public bool IsDisguised { get; private set; }

    /// <summary>Whether the player is currently in the disguise-change animation.</summary>
    public bool IsChanging { get; private set; }

    /// <summary>The security clearance granted by the current disguise.</summary>
    public SecurityClearance CurrentClearance => currentClearance;

    /// <summary>Normalized suspicion (0-1) for UI display.</summary>
    public float SuspicionNormalized => Mathf.Clamp01(currentSuspicion / 100f);

    /// <summary>Always 1 when disguised (no degradation). 0 when not disguised.</summary>
    public float IntegrityNormalized => IsDisguised ? 1f : 0f;

    // ── Internal state ──
    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private Animator _animator;
    private CharacterInputController _inputController;
    private BasicControlScript _controlScript;
    private Rigidbody _rb;

    private SecurityZone _currentZone;
    private float _lastSuspicionIncreaseTime;
    private float _loiterTimer;
    private int _observerCount;
    private bool _missionFailTriggered;

    // ────────────────────────────────────────────
    //  Unity Lifecycle
    // ────────────────────────────────────────────

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _inputController = GetComponent<CharacterInputController>();
        _controlScript = GetComponent<BasicControlScript>();
        _rb = GetComponent<Rigidbody>();

        CacheOriginalMaterials();
    }

    void OnEnable()
    {
        EventManager.AddListener<SuspicionChangedEvent, float, string>(OnSuspicionChanged);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<SuspicionChangedEvent, float, string>(OnSuspicionChanged);
    }

    void Update()
    {
        if (IsChanging || _missionFailTriggered) return;

        if (IsDisguised)
        {
            UpdateBehavioralSuspicion();
        }

        UpdateSuspicionDecay();

        currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, 100f);

        if (currentSuspicion >= 100f && !_missionFailTriggered)
        {
            _missionFailTriggered = true;
            EventManager.TriggerEvent<MissionFailEvent, string, string>(
                "Full alert — cover blown!", "");
        }
    }

    // ────────────────────────────────────────────
    //  Disguise Change
    // ────────────────────────────────────────────

    /// <summary>
    /// Apply a specific disguise with a given clearance level and outfit.
    /// Called by DisguiseBox when the player interacts.
    /// Always applies the new disguise (does NOT toggle off).
    /// </summary>
    public void ApplyDisguise(SecurityClearance clearance, DisguiseOutfit outfit)
    {
        if (IsChanging || outfit == null) return;

        currentOutfit = outfit;
        StartCoroutine(ApplyDisguiseCoroutine(clearance));
    }

    private IEnumerator ApplyDisguiseCoroutine(SecurityClearance newClearance)
    {
        IsChanging = true;

        DisablePlayerControls();
        SetAnimatorIdle();
        SpawnDisguiseVFX();

        yield return new WaitForSeconds(0.3f);
        SetRenderersVisible(false);
        yield return new WaitForSeconds(disguiseDuration - 0.6f);

        SecurityClearance oldClearance = currentClearance;

        if (IsDisguised)
            RevertToOriginalMaterials();

        ApplyDisguiseMaterials();
        IsDisguised = true;
        currentClearance = newClearance;

        SetRenderersVisible(true);
        yield return new WaitForSeconds(0.3f);

        EnablePlayerControls();
        IsChanging = false;

        EventManager.TriggerEvent<DisguiseChangedEvent, SecurityClearance, SecurityClearance>(
            currentClearance, oldClearance);
    }

    private void DisablePlayerControls()
    {
        if (_inputController != null) _inputController.enabled = false;
        if (_controlScript != null) _controlScript.enabled = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void EnablePlayerControls()
    {
        if (_inputController != null) _inputController.enabled = true;
        if (_controlScript != null) _controlScript.enabled = true;
    }

    private void SetAnimatorIdle()
    {
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            _animator.SetFloat("speed", 0f);
            _animator.SetBool("isCrouching", false);
            _animator.SetBool("isSprinting", false);
            _animator.SetFloat("MoveX", 0f);
            _animator.SetFloat("MoveY", 0f);
        }
    }

    private void SpawnDisguiseVFX()
    {
        GameObject vfxObj = new GameObject("DisguiseVFX");
        vfxObj.transform.position = transform.position + Vector3.up * 1f;
        DisguiseVFX vfx = vfxObj.AddComponent<DisguiseVFX>();
        vfx.targetTransform = transform;
        vfx.duration = disguiseDuration;
        vfx.StartEffect();
    }

    // ────────────────────────────────────────────
    //  Inspection (no-op since disguises don't degrade)
    // ────────────────────────────────────────────

    /// <summary>
    /// Called by guards when they perform a close inspection.
    /// Disguises are permanent, so this only adds a small suspicion bump.
    /// </summary>
    public void OnCloseInspection()
    {
        if (!IsDisguised) return;
        AddSuspicion(5f, "Close inspection");
    }

    // ────────────────────────────────────────────
    //  Behavioral Suspicion
    // ────────────────────────────────────────────

    private void UpdateBehavioralSuspicion()
    {
        UpdateLoiterCheck();
    }

    private void UpdateLoiterCheck()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, loiterCheckRadius);
        bool nearGuard = false;

        foreach (var col in nearby)
        {
            if (col.gameObject == gameObject) continue;
            GuardAI guard = col.GetComponent<GuardAI>();
            if (guard == null) guard = col.GetComponentInParent<GuardAI>();
            if (guard != null)
            {
                nearGuard = true;
                break;
            }
        }

        if (nearGuard)
        {
            _loiterTimer += Time.deltaTime;
            if (_loiterTimer > loiterGracePeriod)
            {
                AddSuspicion(loiterSuspicionRate * Time.deltaTime, "Loitering near guards");
            }
        }
        else
        {
            _loiterTimer = Mathf.Max(0f, _loiterTimer - Time.deltaTime * 2f);
        }
    }

    // ────────────────────────────────────────────
    //  Suspicion Management
    // ────────────────────────────────────────────

    /// <summary>Add suspicion from any source.</summary>
    public void AddSuspicion(float amount, string reason)
    {
        if (amount <= 0f) return;

        // When disguised with correct clearance, suspicion builds slower
        float modifier = IsDisguised ? 0.7f : 1.5f;
        float finalAmount = amount * modifier;

        currentSuspicion += finalAmount;
        _lastSuspicionIncreaseTime = Time.time;
        currentSuspicion = Mathf.Clamp(currentSuspicion, 0f, 100f);
    }

    /// <summary>Directly reduce suspicion (e.g., after escaping detection).</summary>
    public void ReduceSuspicion(float amount)
    {
        currentSuspicion = Mathf.Max(0f, currentSuspicion - amount);
    }

    private void UpdateSuspicionDecay()
    {
        if (Time.time - _lastSuspicionIncreaseTime > suspicionDecayDelay)
        {
            if (_observerCount <= 0)
            {
                currentSuspicion -= suspicionDecayRate * Time.deltaTime;
                currentSuspicion = Mathf.Max(0f, currentSuspicion);
            }
        }
    }

    private void OnSuspicionChanged(float delta, string reason)
    {
        AddSuspicion(delta, reason);
    }

    public void AddObserver()
    {
        _observerCount++;
    }

    public void RemoveObserver()
    {
        _observerCount = Mathf.Max(0, _observerCount - 1);
    }

    public bool IsBeingObserved => _observerCount > 0;

    // ────────────────────────────────────────────
    //  Zone Management
    // ────────────────────────────────────────────

    public void SetCurrentZone(SecurityZone zone)
    {
        _currentZone = zone;
    }

    public void ClearCurrentZone(SecurityZone zone)
    {
        if (_currentZone == zone)
            _currentZone = null;
    }

    public SecurityZone CurrentZone => _currentZone;

    // ────────────────────────────────────────────
    //  Material Swap (Visual)
    // ────────────────────────────────────────────

    private void CacheOriginalMaterials()
    {
        var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in renderers)
        {
            if (!_originalMaterials.ContainsKey(r))
                _originalMaterials[r] = r.sharedMaterials.Clone() as Material[];
        }

        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in meshRenderers)
        {
            if (!_originalMaterials.ContainsKey(r))
                _originalMaterials[r] = r.sharedMaterials.Clone() as Material[];
        }
    }

    private void ApplyDisguiseMaterials()
    {
        if (currentOutfit == null) return;
        ApplyOutfitMaterials(currentOutfit);
    }

    private static void ApplyMaterialToRenderer(Renderer renderer, Material material)
    {
        Material[] newMats = new Material[renderer.sharedMaterials.Length];
        for (int i = 0; i < newMats.Length; i++)
            newMats[i] = material;
        renderer.sharedMaterials = newMats;
    }

    /// <summary>
    /// Apply outfit materials only to torso/upper-cloth slots.
    /// This keeps the player's own body rig and avoids cloning/replacing guard meshes.
    /// </summary>
    private void ApplyOutfitMaterials(DisguiseOutfit outfit)
    {
        // Find torso clothing parent transform
        Transform upperCloth = transform.Find("upper_cloth");

        if (upperCloth != null)
        {
            foreach (var r in upperCloth.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string meshName = r.sharedMesh != null ? r.sharedMesh.name : "";

                Material targetMat = null;
                if (meshName.Contains("shirtopenrolled") && outfit.shirtMaterial != null)
                    targetMat = outfit.shirtMaterial;
                else if (meshName.Contains("tshirt") && outfit.tshirtMaterial != null)
                    targetMat = outfit.tshirtMaterial;

                if (targetMat != null)
                {
                    ApplyMaterialToRenderer(r, targetMat);
                }
            }
        }

    }

    private void RevertToOriginalMaterials()
    {
        foreach (var kvp in _originalMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.sharedMaterials = kvp.Value;
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        var renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in renderers)
            r.enabled = visible;
    }
}
