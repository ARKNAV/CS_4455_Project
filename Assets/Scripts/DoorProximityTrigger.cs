using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorProximityTrigger : MonoBehaviour
{
    public Animator doorAnimator;
    public string openParameter = "character_nearby";
    public string playerTag = "Player";

    private int openParameterHash;

    private void Awake()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }

        if (doorAnimator == null)
        {
            doorAnimator = GetComponentInParent<Animator>();
        }

        openParameterHash = Animator.StringToHash(openParameter);
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag) || doorAnimator == null)
        {
            return;
        }

        if (!HasOpenParameter())
        {
            return;
        }

        doorAnimator.SetBool(openParameterHash, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag) || doorAnimator == null)
        {
            return;
        }

        if (!HasOpenParameter())
        {
            return;
        }

        doorAnimator.SetBool(openParameterHash, false);
    }

    private bool HasOpenParameter()
    {
        foreach (AnimatorControllerParameter parameter in doorAnimator.parameters)
        {
            if (parameter.nameHash == openParameterHash && parameter.type == AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        Debug.LogWarning($"DoorProximityTrigger: Animator on '{doorAnimator.gameObject.name}' does not contain bool parameter '{openParameter}'.", this);
        return false;
    }
}
