using UnityEngine;

/*
Purpose: Manages e nd in gs eq ue nc et ri gg er behavior for this part of the game.
Attached GameObject: Trigger collider or event-driving scene object.
Main responsibilities: Evaluate progression conditions, trigger story responses, and update narrative state.
Inputs: Event IDs, story flags, inventory state, and serialized story data.
Outputs or effects: Advances narrative state, launches dialogue, or gates gameplay actions.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class EndingSequenceTrigger : SharedEndingSequence
{
    // Handles trigger entry events for this gameplay object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StartEnding(other.gameObject);
    }
}
