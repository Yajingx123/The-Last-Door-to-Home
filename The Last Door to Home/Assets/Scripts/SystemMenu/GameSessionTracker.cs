using UnityEngine;
using UnityEngine.SceneManagement;
/*
Purpose: Tracks accumulated play time for the current game session.
Attached GameObject: Runtime-created singleton that persists across scenes.
Main responsibilities: Starts, restores, and updates the session timer while excluding main-menu and pause-menu time.
Inputs: Scene load events, pause-menu state, new-game calls, and loaded save play time.
Outputs or effects: Provides PlayTimeSeconds for save metadata and resets timing state on main-menu return.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify new games start at zero, loaded saves resume their time, and paused/main-menu time is ignored.
*/

public class GameSessionTracker : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    private static GameSessionTracker instance;

    private float playTimeSeconds;
    private bool sessionStarted;

    public static float PlayTimeSeconds => instance != null ? instance.playTimeSeconds : 0f;

    // Ensures the session tracker singleton exists after a scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // Ensures the runtime singleton exists after a scene load.
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    // Starts the start new session sequence or runtime effect.
    public static void StartNewSession()
    {
        EnsureInstance();
        instance.playTimeSeconds = 0f;
        instance.sessionStarted = true;
    }

    // Handles the restore session step for this script.
    public static void RestoreSession(float savedPlayTimeSeconds)
    {
        EnsureInstance();
        instance.playTimeSeconds = Mathf.Max(0f, savedPlayTimeSeconds);
        instance.sessionStarted = true;
    }

    // Ensures the required ensure session started objects or state exist.
    public static void EnsureSessionStarted()
    {
        EnsureInstance();
        if (instance.sessionStarted) return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid()) return;
        if (string.Equals(activeScene.name, "MainMenu", System.StringComparison.OrdinalIgnoreCase)) return;

        instance.sessionStarted = true;
    }

    // Finds or creates the shared runtime instance used by this system.
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

    // Initializes component references and singleton ownership before Start runs.
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

    // Registers callbacks or resets transient state when the component becomes active.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unregisters callbacks when the component becomes inactive.
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Creates required runtime objects and prepares this system for use.
    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Reads per-frame input and updates frame-dependent runtime state.
    private void Update()
    {
        if (!sessionStarted) return;
        if (EscapeMenuController.IsMenuOpen) return;
        if (IsMainMenuScene(SceneManager.GetActiveScene())) return;

        playTimeSeconds += Time.unscaledDeltaTime;
    }

    // Handles the on scene loaded step for this script.
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

    // Returns whether is main menu scene is true for the current state.
    private static bool IsMainMenuScene(Scene scene)
    {
        return scene.IsValid()
            && string.Equals(scene.name, MainMenuSceneName, System.StringComparison.OrdinalIgnoreCase);
    }
}
