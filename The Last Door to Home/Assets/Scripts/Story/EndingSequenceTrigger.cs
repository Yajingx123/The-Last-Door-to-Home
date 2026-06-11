using UnityEngine;
/*
Purpose: Defines or executes story progression logic.
Attached GameObject: Story controller, trigger object, or ScriptableObject asset depending on the script type.
Main responsibilities: Tracks story flags/counters, evaluates story beats, and triggers dialogue or ending flows.
Inputs: Story event IDs, flags, counters, configured beats, interaction triggers, and scene state.
Outputs or effects: Updates story state, starts dialogue/events, blocks progression when needed, or triggers ending sequences.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each story event fires once or repeats as designed and survives save/load when required.
*/

public class EndingSequenceTrigger : SharedEndingSequence
{
    // Handles 2D trigger entry events for this object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StartEnding(other.gameObject);
    }
}
