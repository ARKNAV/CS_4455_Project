using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveTriggerZone : MonoBehaviour
{
    [SerializeField] private string objectiveId;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireCrouching;
    [SerializeField] private bool requirePeeking;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        DemoObjectiveManager manager = DemoObjectiveManager.Instance;
        if (manager == null || !manager.IsCurrentObjective(objectiveId))
        {
            return;
        }

        CharacterInputController input = other.GetComponentInParent<CharacterInputController>();
        PeekSystem peekSystem = other.GetComponentInParent<PeekSystem>();

        if (requireCrouching && (input == null || !input.IsCrouching))
        {
            return;
        }

        if (requirePeeking && (peekSystem == null || !peekSystem.IsPeeking))
        {
            return;
        }

        bool completed = manager.CompleteObjective(objectiveId);
        if (completed)
        {
            hasTriggered = true;
        }
    }
}