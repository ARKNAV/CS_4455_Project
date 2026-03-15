using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
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

    public static void TriggerLose()
    {
        if (Instance != null)
        {
            Instance.HandleMissionFail("Captured!");
            return;
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleMissionFail(string reason)
    {
        if (MissionFailed) return;
        MissionFailed = true;

        Debug.Log($"[GameManager] Mission Failed: {reason}");

        DisguiseSystem ds = FindFirstObjectByType<DisguiseSystem>();
        if (ds != null)
        {
            CharacterInputController cinput = ds.GetComponent<CharacterInputController>();
            if (cinput != null) cinput.enabled = false;

            BasicControlScript bcs = ds.GetComponent<BasicControlScript>();
            if (bcs != null) bcs.enabled = false;
        }

            Invoke(nameof(ReloadCurrentScene), failReloadDelay);
    }

    private void ReloadCurrentScene()
    {
        MissionFailed = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
