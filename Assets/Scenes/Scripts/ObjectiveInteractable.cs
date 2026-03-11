using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectiveInteractable : MonoBehaviour
{
    [SerializeField] private string objectiveId;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool completeOnlyIfCurrent = true;
    [SerializeField] private bool disableAfterUse = true;

    private bool playerInRange;

    void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        DemoObjectiveManager manager = DemoObjectiveManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (completeOnlyIfCurrent && !manager.IsCurrentObjective(objectiveId))
        {
            return;
        }

        bool completed = manager.CompleteObjective(objectiveId);
        if (completed && disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
        }
    }
}