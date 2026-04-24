using UnityEngine;

public class PointTowardsPlayerDisguiseBoxArrow : MonoBehaviour
{
    public GameObject player;

    [Header("Animation")]
    public float bobAmplitude  = 0.18f;
    public float bobFrequency  = 1.8f;
    public float pulseAmount   = 0.15f;
    public float pulseFrequency = 2.2f;

    private DisguiseBox _box;
    private Vector3 _basePos, _baseScale;

    void Start()
    {
        _box       = GetComponentInParent<DisguiseBox>();
        _basePos   = transform.localPosition;
        _baseScale = transform.localScale;

        if (player == null)
        {
            DisguiseSystem ds = FindFirstObjectByType<DisguiseSystem>();
            if (ds != null) player = ds.gameObject;
        }
    }

    void Update()
    {
        if (_box != null && _box.isUsed) { gameObject.SetActive(false); return; }

        if (player != null)
        {
            Vector3 dir = player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        float t = Time.time;
        transform.localPosition = _basePos + Vector3.up * Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.localScale    = _baseScale * (1f + Mathf.Abs(Mathf.Sin(t * pulseFrequency * Mathf.PI)) * pulseAmount);
    }
}
