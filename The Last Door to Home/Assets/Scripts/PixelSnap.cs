using UnityEngine;

[ExecuteInEditMode]
/*
Purpose: Snaps an object position to a pixel grid.
Attached GameObject: Any scene object that needs pixel-perfect positioning.
Main responsibilities: Rounds the transform position to the configured pixels-per-unit grid every frame.
Inputs: Transform position and pixels-per-unit value.
Outputs or effects: Updates the transform position to a pixel-aligned coordinate.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify pixel art remains stable in Edit Mode and Play Mode.
*/

public class PixelSnap : MonoBehaviour
{
    private float pixelsPerUnit = 32; // Pixels per unit used by the current artwork.

    // Reads per-frame input and updates frame-dependent runtime state.
    void Update()
    {
        Vector3 pos = transform.position;

        // Snap the position to one-pixel precision.
        pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = pos;
    }
}
