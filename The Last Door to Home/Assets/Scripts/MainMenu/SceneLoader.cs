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
    [System.Serializable]
    public class StartingItem
    {
        [Header("物品唯一ID / Item Unique ID")]
        public string itemUniqueID;

        [Header("物品名称 / Item Name")]
        public string itemName;

        [Header("物品类型 / Item Type")]
        public ItemType itemType;

        [Header("物品详情 / Item Details")]
        [TextArea(2, 6)]
        public string itemDescription;

        [Header("图标资源路径 / Icon Resource Path")]
        [Tooltip("Optional. For a single Sprite in Resources, use a path without extension, e.g. ItemIcons/Flower. For a sliced sprite sheet, use SheetPath#SpriteName, e.g. ItemIcons/Items#flower_01.")]
        public string itemIconResourcePath;
    }

    [Header("New Game 跳转场景 / New Game Target Scene")]
    public string newGameSceneName = "IntroCutscene";

    [Header("初始物品 / Starting Items")]
    public StartingItem[] startingItems;

    // Loads the requested data, scene, or runtime content.
    public void LoadScene(string sceneName)
    {
        if (string.Equals(sceneName, newGameSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            StartNewGame();
            return;
        }

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
        AddStartingItems();
        PlayerSpawn.NEED_SPAWN = false;
        CutsceneReturnContext.Clear();
        GameSessionTracker.StartNewSession();
        SceneTransition.LoadScene(newGameSceneName);
    }

    // Adds the configured new-game inventory items after clearing previous state.
    private void AddStartingItems()
    {
        if (startingItems == null) return;

        for (int i = 0; i < startingItems.Length; i++)
        {
            StartingItem item = startingItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemUniqueID)) continue;

            Inventory.AddItem(
                item.itemName,
                item.itemType,
                item.itemUniqueID,
                item.itemDescription,
                item.itemIconResourcePath
            );
        }
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
