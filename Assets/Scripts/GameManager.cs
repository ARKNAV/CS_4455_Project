using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static Action LoseTriggerEvent;
    public static GameManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] [Range(0f, 1f)] private float backgroundMusicVolume = 0.3f;
    [SerializeField] private bool loopBackgroundMusic = true;
    [SerializeField] private bool playMusicOnAwake = true;

    [Header("Mission Settings")]
    [Tooltip("Delay before reloading the scene after mission failure")]
    public float failReloadDelay = 2f;

    [Tooltip("Whether the mission has failed (prevents duplicate triggers)")]
    public bool MissionFailed { get; private set; }

    private AudioSource backgroundMusicSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupBackgroundMusic();
    }

    void Start()
    {
        if (playMusicOnAwake)
        {
            PlayBackgroundMusic();
        }
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

            Rigidbody rb = ds.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        LoseTriggerEvent?.Invoke();
        // Reload after delay
        //Invoke(nameof(ReloadCurrentScene), failReloadDelay);
    }

    private void SetupBackgroundMusic()
    {
        if (backgroundMusicClip == null)
        {
            return;
        }

        backgroundMusicSource = GetComponent<AudioSource>();
        if (backgroundMusicSource == null)
        {
            backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        }

        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.loop = loopBackgroundMusic;
        backgroundMusicSource.spatialBlend = 0f;
        backgroundMusicSource.volume = backgroundMusicVolume;
        backgroundMusicSource.clip = backgroundMusicClip;
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicSource == null || backgroundMusicSource.clip == null || backgroundMusicSource.isPlaying)
        {
            return;
        }

        backgroundMusicSource.Play();
    }

    private void ReloadCurrentScene()
    {
        MissionFailed = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
