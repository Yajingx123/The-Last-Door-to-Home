using UnityEngine;
using UnityEngine.SceneManagement;

/*
Purpose: Tracks the current run's accumulated play time for save slots and load restore.
Attached GameObject: Auto-created runtime singleton.
Main responsibilities: Accumulate unpaused play time and expose reset/restore helpers for save workflows.
Inputs: Scene transitions, pause state, and load/new-game requests.
Outputs or effects: Maintains a shared play-time counter used by save metadata.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify new game starts at zero, paused time is ignored, and loaded saves resume their prior play time.
*/

public class GameSessionTracker : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private static GameSessionTracker instance;

    private float playTimeSeconds;
    private bool sessionStarted;

    public static float PlayTimeSeconds => instance != null ? instance.playTimeSeconds : 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void StartNewSession()
    {
        EnsureInstance();
        instance.playTimeSeconds = 0f;
        instance.sessionStarted = true;
    }

    public static void RestoreSession(float savedPlayTimeSeconds)
    {
        EnsureInstance();
        instance.playTimeSeconds = Mathf.Max(0f, savedPlayTimeSeconds);
        instance.sessionStarted = true;
    }

    public static void EnsureSessionStarted()
    {
        EnsureInstance();
        if (instance.sessionStarted) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;
        if (string.Equals(activeScene.name, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;

        instance.sessionStarted = true;
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        instance = FindObjectOfType<GameSessionTracker>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject go = new GameObject("GameSessionTracker");
        instance = go.AddComponent<GameSessionTracker>();
        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!sessionStarted) return;
        if (EscapeMenuController.IsMenuOpen) return;
        if (IsMainMenuScene(SceneManager.GetActiveScene())) return;

        playTimeSeconds += Time.unscaledDeltaTime;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid()) return;

        if (IsMainMenuScene(scene))
        {
            sessionStarted = false;
            return;
        }

        if (!sessionStarted)
        {
            sessionStarted = true;
        }
    }

    private static bool IsMainMenuScene(Scene scene)
    {
        return scene.IsValid()
            && string.Equals(scene.name, MainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);
    }
}
