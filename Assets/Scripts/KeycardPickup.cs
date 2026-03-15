using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KeycardPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool completeObjectiveOnPickup = true;
    [SerializeField] private string objectiveId = "idcard";

    [Header("Requirements")]
    [SerializeField] private bool requireDisguise = true;

    [Header("Feedback")]
    [SerializeField] private string pickupMessage = "Key card collected";
    [SerializeField] private float pickupMessageDuration = 2f;
    [SerializeField] private string disguiseRequiredMessage = "You need a disguise to pick this up";
    [SerializeField] private float disguiseRequiredMessageDuration = 2f;

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

        if (requireDisguise)
        {
            DisguiseSystem disguiseSystem = other.GetComponentInParent<DisguiseSystem>();
            if (disguiseSystem == null || !disguiseSystem.IsDisguised)
            {
                if (InteractionFeedbackHUD.Instance != null)
                {
                    InteractionFeedbackHUD.Instance.ShowMessage(disguiseRequiredMessage, disguiseRequiredMessageDuration);
                }
                return;
            }
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
