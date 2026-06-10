using UnityEngine;

/*
Purpose: Manages s ce ne lo ad er behavior for this part of the game.
Attached GameObject: Main menu canvas or UI controller GameObject.
Main responsibilities: Process menu navigation input and drive scene or UI transitions.
Inputs: Inspector configuration, scene references, and runtime method calls.
Outputs or effects: Applies runtime side effects through component state, UI updates, or return values.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class SceneLoader : MonoBehaviour
{
    [Header("New Game 跳转场景")]
    public string newGameSceneName = "IntroCutscene";

    // 跳转到指定场景（通过场景名）
    // Starts loading the requested scene through the transition flow.
    public void LoadScene(string sceneName)
    {
        SceneTransition.LoadScene(sceneName);
    }

    // Starts a new game flow from the main menu.
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

    // Opens the manual Continue slot list from the main menu.
    public void ContinueGame()
    {
        MainMenuLoadPanelController.OpenPanel();
    }

    // 退出游戏（仅打包后生效，编辑器中无效果）
    // Quits the application from the main menu flow.
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器中停止运行
        #endif
    }

    // Alias for UI buttons labeled Exit.
    public void ExitGame()
    {
        QuitGame();
    }
}
