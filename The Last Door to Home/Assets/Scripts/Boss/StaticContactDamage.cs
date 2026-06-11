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

public class StaticContactDamage : MonoBehaviour
{
    [SerializeField] private MonsterController controller;
    [SerializeField] private string sourceName = "StaticHazard";

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        ResolveController();
    }

    // Handles 2D trigger entry events for this object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    // Handles 2D trigger stay events for this object.
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    // Attempts the requested operation and reports whether it succeeded.
    private void TryDamagePlayer(Collider2D other)
    {
        ResolveController();
        PlayerMove playerMove = other.GetComponentInParent<PlayerMove>();
        if (playerMove == null || controller == null)
        {
            return;
        }

        controller.TryDamagePlayer(sourceName, this);
    }

    // Resolves the best available value for the requested data.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }
}
