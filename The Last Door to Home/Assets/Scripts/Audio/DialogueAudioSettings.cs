using System;
using UnityEngine;

[Serializable]
/*
Purpose: Defines the runtime behavior or data handled by DialogueAudioSettings.
Attached GameObject: Attach this script to the GameObject that owns this behavior, or use it as a data/static helper when no GameObject is required.
Main responsibilities: Coordinates serialized settings, runtime state, and calls from related game systems.
Inputs: Inspector values, Unity lifecycle callbacks, player input, scene state, and method parameters where applicable.
Outputs or effects: Updates component state, shared game state, UI, audio, scene flow, or serialized data as required by the script.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify the Inspector references, expected Play Mode behavior, edge cases, and any connected UI/audio/scene flow.
*/

public class DialogueAudioSettings
{
    [Header("对话开始时切入的BGM（可空） / BGM On Dialogue Start (Optional)")]
    public AudioClip bgmOnStart;
    public float bgmOnStartFadeOutDuration = 1f;
    public float bgmOnStartFadeInDuration = 1f;
    [Range(0f, 1f)] public float bgmOnStartVolume = 1f;

    [Header("对话结束后切入的BGM（可空） / BGM On Dialogue End (Optional)")]
    public AudioClip bgmOnComplete;
    public float bgmOnCompleteFadeOutDuration = 1f;
    public float bgmOnCompleteFadeInDuration = 1f;
    [Range(0f, 1f)] public float bgmOnCompleteVolume = 1f;

    [Header("打字机音效（可空） / Typewriter SFX (Optional)")]
    public AudioClip typingSfx;
    [Range(0f, 1f)] public float typingSfxVolume = 0.25f;
    [Min(1)] public int typingSfxEveryNCharacters = 2;

    [Header("双引号内容专用音效（可空） / Quoted Text SFX (Optional)")]
    public AudioClip quotedTypingSfx;
    [Range(0f, 1f)] public float quotedTypingSfxVolume = 0.25f;
}
