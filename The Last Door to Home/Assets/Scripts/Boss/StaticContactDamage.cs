using UnityEngine;

/*
Purpose: Applies monster-style contact damage from a stationary scene hazard.
Attached GameObject: Any static hazard object with a Collider2D trigger.
Main responsibilities: Resolve the shared monster controller and damage the player on trigger contact.
Inputs: Optional monster controller reference and a source label for debug logging.
Outputs or effects: Reduces player hearts through MonsterController without moving the hazard.
Authorship or assistance: Original gameplay support script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify the collider is set as a trigger and that the scene contains a MonsterController.
*/

public class StaticContactDamage : MonoBehaviour
{
    [SerializeField] private MonsterController controller;
    [SerializeField] private string sourceName = "StaticHazard";

    // Finds the shared monster controller automatically when one is not assigned.
    private void Awake()
    {
        ResolveController();
    }

    // Damages the player when they first touch this hazard.
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    // Keeps the hazard damaging when the player remains inside it.
    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    // Sends one damage request through the shared monster battle controller.
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

    // Finds the scene monster controller when the reference is left empty.
    private void ResolveController()
    {
        if (controller == null)
        {
            controller = FindObjectOfType<MonsterController>();
        }
    }
}
