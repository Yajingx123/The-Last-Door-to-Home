using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [Header("New Game 跳转场景")]
    public string newGameSceneName = "IntroCutscene";

    // 跳转到指定场景（通过场景名）
    public void LoadScene(string sceneName)
    {
        SceneTransition.LoadScene(sceneName);
    }

    // MainMenu 的 New Game 调用这个
    public void StartNewGame()
    {
        if (string.IsNullOrWhiteSpace(newGameSceneName))
        {
            Debug.LogWarning("SceneLoader: newGameSceneName 为空，无法开始新游戏。", this);
            return;
        }

        Inventory.Clear();
        SceneTransition.LoadScene(newGameSceneName);
    }

    // 退出游戏（仅打包后生效，编辑器中无效果）
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 编辑器中停止运行
        #endif
    }
}
