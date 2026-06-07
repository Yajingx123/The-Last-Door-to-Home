using UnityEngine;

/*
Purpose: Plays the boss-battle defeat ending flow with a two-phase dialogue sequence.
Attached GameObject: A scene object responsible for handling the monster-battle ending.
Main responsibilities: Lock the player, fade the player out, play first and second dialogue segments, manage blackout, and finish the ending.
Inputs: Serialized dialogue content, optional images and audio settings, and scene-finish configuration.
Outputs or effects: Reuses the same two-stage ending presentation style as EndingSequenceTrigger when the player loses all hearts.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the dialogue ordering, blackout layering, player fade, and final scene transition in Play Mode.
*/

public class MonsterEndingSequence : SharedEndingSequence
{
    // Starts the defeat ending flow if it has not already been triggered.
    public void PlayEnding(GameObject playerObject)
    {
        StartEnding(playerObject);
    }
}
