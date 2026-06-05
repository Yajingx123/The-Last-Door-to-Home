using UnityEngine;

/*
Purpose: Stores configurable s to ry be at se t data for inspector-driven workflows.
Attached GameObject: Not attached; create and configure this ScriptableObject as a project asset.
Main responsibilities: Hold serialized configuration that other runtime systems can read safely.
Inputs: Inspector-authored data referenced by runtime systems.
Outputs or effects: Serialized data assets that influence runtime behaviour when referenced.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[CreateAssetMenu(fileName = "StoryBeatSet", menuName = "Story/Story Beat Set")]
public class StoryBeatSet : ScriptableObject
{
    [Header("按顺序触发的剧情条目")]
    public StoryBeat[] beats;
}
