using UnityEngine;

/*
Purpose: Manages p la ye rs pa wn behavior for this part of the game.
Attached GameObject: Player GameObject or a player-specific child object.
Main responsibilities: Read player-facing state, coordinate related components, and apply movement or presentation updates.
Inputs: Inspector references, Unity input, and state from linked gameplay managers.
Outputs or effects: Moves the player or camera, updates animations, and changes immediate gameplay feel.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class PlayerSpawn : MonoBehaviour
{
    public static Vector2 SPAWN_POSITION;
    public static bool NEED_SPAWN = false;

    // Initializes cached references and one-time component state before gameplay begins.
    private void Awake()
    {
        if (NEED_SPAWN)
        {
            transform.position = SPAWN_POSITION;
            NEED_SPAWN = false;
        }
    }
}
