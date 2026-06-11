using UnityEngine;
/*
Purpose: Restores the player position after scene transitions.
Attached GameObject: Player GameObject.
Main responsibilities: Applies a pending spawn position once, then clears the spawn request flag.
Inputs: Static spawn position and spawn request values set before scene loading.
Outputs or effects: Updates the Player transform position and clears NEED_SPAWN.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify scene exits, save loading, and new-game starts place the player at the expected position.
*/

public class PlayerSpawn : MonoBehaviour
{
    public static Vector2 SPAWN_POSITION;
    public static bool NEED_SPAWN = false;

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        if (NEED_SPAWN)
        {
            transform.position = SPAWN_POSITION;
            NEED_SPAWN = false;
        }
    }
}
