using UnityEngine;
/*
Purpose: Provides main-menu button actions for starting, continuing, loading, and quitting the game.
Attached GameObject: Main menu controller GameObject referenced by UI button OnClick events.
Main responsibilities: Routes menu button requests into scene transitions, new-game setup, save-slot UI, or application quit.
Inputs: Button OnClick events, configured new-game scene name, and shared game/session state.
Outputs or effects: Clears or updates runtime state, opens the Continue panel, starts scene transitions, or quits Play Mode/application.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each main-menu button calls the expected method and handles empty scene names safely.
*/

public class SceneLoader : MonoBehaviour
{
    [Header("New Game 跳转场景 / New Game Target Scene")]
    public string newGameSceneName = "IntroCutscene";

    // Loads the requested data, scene, or runtime content.
    public void LoadScene(string sceneName)
    {
        SceneTransition.LoadScene(sceneName);
    }

    // Starts the start new game sequence or runtime effect.
    public void StartNewGame()
    {
        if (string.IsNullOrWhiteSpace(newGameSceneName))
        {
            Debug.LogWarning("SceneLoader: newGameSceneName 为空，无法开始新游戏。", this);
            return;
        }

        Inventory.Clear();
        PlayerSpawn.NEED_SPAWN = false;
        CutsceneReturnContext.Clear();
        GameSessionTracker.StartNewSession();
        SceneTransition.LoadScene(newGameSceneName);
    }

    // Continues the continue game flow from its current state.
    public void ContinueGame()
    {
        MainMenuLoadPanelController.OpenPanel();
    }

    // Quits the application or exits Play Mode in the Unity Editor.
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stop Play Mode in the Unity Editor.
        #endif
    }

    // Handles the exit game step for this script.
    public void ExitGame()
    {
        QuitGame();
    }
}
