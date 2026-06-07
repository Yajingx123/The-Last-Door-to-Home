using UnityEngine;

/*
Purpose: Manages p ix el sn ap behavior for this part of the game.
Attached GameObject: Scene object that needs the visual helper behavior.
Main responsibilities: Coordinate inspector data, runtime checks, and the main behaviour handled by this script.
Inputs: Inspector configuration, scene references, and runtime method calls.
Outputs or effects: Applies runtime side effects through component state, UI updates, or return values.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[ExecuteInEditMode]
public class PixelSnap : MonoBehaviour
{
    private float pixelsPerUnit = 32; // 你的贴图 PPU = 32

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        Vector3 pos = transform.position;

        // 核心：把位置对齐到 1像素 的精度
        pos.x = Mathf.Round(pos.x * pixelsPerUnit) / pixelsPerUnit;
        pos.y = Mathf.Round(pos.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = pos;
    }
}