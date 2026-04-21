using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [SerializeField] private bool hasKeycard;
    [SerializeField] private int missionKeysCollected;
    [SerializeField] private int missionKeysRequired = 2;
    [SerializeField] private bool hasBlueprints;

    public bool HasKeycard
    {
        get { return hasKeycard; }
    }

    public int MissionKeysCollected
    {
        get { return missionKeysCollected; }
    }

    public int MissionKeysRequired
    {
        get { return Mathf.Max(0, missionKeysRequired); }
    }

    public bool HasRequiredMissionKeys
    {
        get { return MissionKeysCollected >= MissionKeysRequired; }
    }

    public bool HasBlueprints
    {
        get { return hasBlueprints; }
    }

    public event Action<bool> OnKeycardStateChanged;
    public event Action<int, int> OnMissionKeyCountChanged;
    public event Action<bool> OnBlueprintStateChanged;

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

    public bool CollectMissionKey()
    {
        int required = MissionKeysRequired;
        if (required <= 0)
        {
            return false;
        }

        if (missionKeysCollected >= required)
        {
            return false;
        }

        missionKeysCollected++;
        OnMissionKeyCountChanged?.Invoke(missionKeysCollected, required);
        return true;
    }

    public void ClearMissionKeys()
    {
        if (missionKeysCollected == 0)
        {
            return;
        }

        missionKeysCollected = 0;
        OnMissionKeyCountChanged?.Invoke(missionKeysCollected, MissionKeysRequired);
    }

    public bool CollectBlueprints()
    {
        if (hasBlueprints)
        {
            return false;
        }

        hasBlueprints = true;
        OnBlueprintStateChanged?.Invoke(hasBlueprints);
        return true;
    }

    public void ClearBlueprints()
    {
        if (!hasBlueprints)
        {
            return;
        }

        hasBlueprints = false;
        OnBlueprintStateChanged?.Invoke(hasBlueprints);
    }
}
