using UnityEngine;
using UnityEngine.SceneManagement;

/*
Purpose: Stores the scene and player position needed to return from a temporary cutscene.
Attached GameObject: None; this is a static helper.
Main responsibilities: Capture the current gameplay scene context before entering a cutscene and restore it afterward.
Inputs: Active scene information and the current player transform.
Outputs or effects: Provides saved return-scene data and configures PlayerSpawn for the restored scene load.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify that entering and exiting a cutscene restores the player to the expected position in the previous scene.
*/

public static class CutsceneReturnContext
{
    private static string returnSceneName;
    private static Vector2 returnPlayerPosition;
    private static bool hasReturnScene;

    public static bool HasSavedContext => hasReturnScene && !string.IsNullOrWhiteSpace(returnSceneName);

    // Saves the active scene name and the current player position before entering a temporary cutscene.
    public static void SaveCurrentSceneAndPlayerPosition()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
        {
            hasReturnScene = false;
            returnSceneName = string.Empty;
            returnPlayerPosition = Vector2.zero;
            return;
        }

        returnSceneName = activeScene.name;
        returnPlayerPosition = ResolvePlayerPosition();
        hasReturnScene = true;
    }

    // Restores the saved player spawn data just before the previous scene is loaded again.
    public static void ApplyReturnSpawn()
    {
        if (!HasSavedContext) return;

        PlayerSpawn.SPAWN_POSITION = returnPlayerPosition;
        PlayerSpawn.NEED_SPAWN = true;
    }

    // Returns the saved scene name for the cutscene exit flow.
    public static string GetReturnSceneName()
    {
        return returnSceneName;
    }

    // Clears any saved cutscene return data after it has been consumed or invalidated.
    public static void Clear()
    {
        returnSceneName = string.Empty;
        returnPlayerPosition = Vector2.zero;
        hasReturnScene = false;
    }

    // Finds the current player position, falling back to the pending spawn value when needed.
    private static Vector2 ResolvePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }

        if (PlayerSpawn.NEED_SPAWN)
        {
            return PlayerSpawn.SPAWN_POSITION;
        }

        return Vector2.zero;
    }
}
