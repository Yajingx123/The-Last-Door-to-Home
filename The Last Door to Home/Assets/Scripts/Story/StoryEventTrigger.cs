using UnityEngine;

/*
Purpose: Manages s to ry ev en tt ri gg er behavior for this part of the game.
Attached GameObject: Trigger collider or event-driving scene object.
Main responsibilities: Evaluate progression conditions, trigger story responses, and update narrative state.
Inputs: Event IDs, story flags, inventory state, and serialized story data.
Outputs or effects: Advances narrative state, launches dialogue, or gates gameplay actions.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[RequireComponent(typeof(Collider2D))]
public class StoryEventTrigger : MonoBehaviour
{
    [Header("事件")]
    public string eventId;

    // Handles trigger entry events for this gameplay object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (StoryDirector.Instance == null) return;
        StoryDirector.Instance.TryHandleEvent(eventId);
    }
}
