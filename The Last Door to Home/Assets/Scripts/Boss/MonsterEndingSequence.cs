using UnityEngine;
/*
Purpose: Controls boss, enemy, damage, or boss-ending behavior.
Attached GameObject: Boss/enemy GameObject, damage hitbox, or boss-scene controller.
Main responsibilities: Updates combat movement/state, resolves contact damage, handles defeat, and triggers ending or door behavior.
Inputs: Player position, colliders, serialized combat settings, health/progression state, and scene triggers.
Outputs or effects: Moves enemies, applies damage, updates animations, changes story/ending state, or loads scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify combat states, damage timing, defeat conditions, and ending transitions.
*/

public class MonsterEndingSequence : SharedEndingSequence
{
    // Plays the play ending sequence or audio feedback.
    public void PlayEnding(GameObject playerObject)
    {
        StartEnding(playerObject);
    }
}
