using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [SerializeField] private bool hasKeycard;

    public bool HasKeycard
    {
        get { return hasKeycard; }
    }

    public event Action<bool> OnKeycardStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool CollectKeycard()
    {
        if (hasKeycard)
        {
            return false;
        }

        hasKeycard = true;
        OnKeycardStateChanged?.Invoke(hasKeycard);
        return true;
    }

    public void ClearKeycard()
    {
        if (!hasKeycard)
        {
            return;
        }

        hasKeycard = false;
        OnKeycardStateChanged?.Invoke(hasKeycard);
    }
}
