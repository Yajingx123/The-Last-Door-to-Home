/*
Purpose: Defines the contract for i in te ra ct ab le used across the project.
Attached GameObject: Implemented by interactable scene objects rather than attached directly.
Main responsibilities: Expose the required interaction entry point for implementing behaviours.
Inputs: Calls from other gameplay systems that invoke the declared interaction method.
Outputs or effects: A consistent interaction surface for gameplay systems and interactable objects.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public interface IInteractable
{
    void OnInteract();
}