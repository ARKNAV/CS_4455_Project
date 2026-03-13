using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeycardPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool completeObjectiveOnPickup = true;
    [SerializeField] private string objectiveId = "idcard";

    [Header("Feedback")]
    [SerializeField] private string pickupMessage = "Key card collected";
    [SerializeField] private float pickupMessageDuration = 2f;

    private bool collected;

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
            Debug.LogWarning("KeycardPickup: No PlayerInventory found. Add PlayerInventory to the player.", this);
            return;
        }

        if (!inventory.CollectKeycard())
        {
            return;
        }

        collected = true;

        if (completeObjectiveOnPickup && DemoObjectiveManager.Instance != null)
        {
            DemoObjectiveManager.Instance.CompleteObjective(objectiveId);
        }

        if (InteractionFeedbackHUD.Instance != null)
        {
            InteractionFeedbackHUD.Instance.ShowMessage(pickupMessage, pickupMessageDuration);
        }

        gameObject.SetActive(false);
    }
}
