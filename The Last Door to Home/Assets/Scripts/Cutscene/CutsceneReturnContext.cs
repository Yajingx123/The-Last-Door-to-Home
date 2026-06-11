using UnityEngine;
using UnityEngine.SceneManagement;
/*
Purpose: Controls a cutscene sequence or stores cutscene return context.
Attached GameObject: Cutscene scene controller GameObject, or static context helper when applicable.
Main responsibilities: Displays slides/text, handles timing and input, plays audio, and transitions to the next scene.
Inputs: Serialized cutscene assets, player input, timing settings, audio clips, and return-scene context.
Outputs or effects: Updates cutscene UI, plays audio, records return data, and loads follow-up scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify slide order, skip/advance input, audio timing, and final scene transition.
*/

public static class CutsceneReturnContext
{
    private static string returnSceneName;
    private static Vector2 returnPlayerPosition;
    private static bool hasReturnScene;

    public static bool HasSavedContext => hasReturnScene && !string.IsNullOrWhiteSpace(returnSceneName);

    // Saves the current data or runtime state.
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

    // Applies the requested visual, audio, or gameplay state.
    public static void ApplyReturnSpawn()
    {
        if (!HasSavedContext) return;

        PlayerSpawn.SPAWN_POSITION = returnPlayerPosition;
        PlayerSpawn.NEED_SPAWN = true;
    }

    // Returns the requested value or runtime object.
    public static string GetReturnSceneName()
    {
        return returnSceneName;
    }

    // Clears the stored runtime state managed by this system.
    public static void Clear()
    {
        returnSceneName = string.Empty;
        returnPlayerPosition = Vector2.zero;
        hasReturnScene = false;
    }

    // Resolves the best available value for the requested data.
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
