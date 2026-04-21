using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MissionKeyPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool completeObjectiveOnPickup = true;
    [SerializeField] private string objectiveId = "collect_center_key_1";

    [Header("Feedback")]
    [SerializeField] private string pickupMessage = "Mission key collected";
    [SerializeField] private float pickupMessageDuration = 2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pickupClip;

    private bool collected;

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
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag(playerTag))
        {
            return;
        }

        PlayerInventory inventory = PlayerInventory.Instance;
        if (inventory == null)
        {
            inventory = other.GetComponentInParent<PlayerInventory>();
        }

        if (inventory == null)
        {
            Debug.LogWarning("MissionKeyPickup: No PlayerInventory found. Add PlayerInventory to the player.", this);
            return;
        }

        if (!inventory.CollectMissionKey())
        {
            return;
        }

        collected = true;

        if (completeObjectiveOnPickup && DemoObjectiveManager.Instance != null)
        {
            DemoObjectiveManager manager = DemoObjectiveManager.Instance;
            if (manager.IsCurrentObjective(objectiveId))
            {
                manager.CompleteObjective(objectiveId);
            }
        }

        if (InteractionFeedbackHUD.Instance != null)
        {
            string suffix = inventory.MissionKeysRequired > 0
                ? $" ({inventory.MissionKeysCollected}/{inventory.MissionKeysRequired})"
                : string.Empty;
            InteractionFeedbackHUD.Instance.ShowMessage(pickupMessage + suffix, pickupMessageDuration);
        }

        if (audioSource != null && pickupClip != null)
        {
            audioSource.PlayOneShot(pickupClip);
        }

        gameObject.SetActive(false);
    }
}
