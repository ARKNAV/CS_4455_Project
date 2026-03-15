using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager handling mission state, fail conditions, and scene management.
/// Listens for MissionFailEvent from the suspicion/alert system.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static Action LoseTriggerEvent;
    public static GameManager Instance { get; private set; }

    [Header("Mission Settings")]
    [Tooltip("Delay before reloading the scene after mission failure")]
    public float failReloadDelay = 2f;

    [Tooltip("Whether the mission has failed (prevents duplicate triggers)")]
    public bool MissionFailed { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        EventManager.AddListener<MissionFailEvent, string, string>(OnMissionFail);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<MissionFailEvent, string, string>(OnMissionFail);
    }

    private void OnMissionFail(string reason, string _unused)
    {
        if (MissionFailed) return;
        MissionFailed = true;

        Debug.Log($"[GameManager] Mission Failed: {reason}");

        // Disable player controls
        DisguiseSystem ds = FindFirstObjectByType<DisguiseSystem>();
        if (ds != null)
        {
            CharacterInputController cinput = ds.GetComponent<CharacterInputController>();
            if (cinput != null) cinput.enabled = false;

            BasicControlScript bcs = ds.GetComponent<BasicControlScript>();
            if (bcs != null) bcs.enabled = false;
        }
        LoseTriggerEvent.Invoke();
        // Reload after delay
        //Invoke(nameof(ReloadCurrentScene), failReloadDelay);
    }

    public static void TriggerLose()
    {
        if (Instance != null)
        {
            Instance.OnMissionFail("Captured!", "");
            return;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReloadCurrentScene()
    {
        MissionFailed = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
