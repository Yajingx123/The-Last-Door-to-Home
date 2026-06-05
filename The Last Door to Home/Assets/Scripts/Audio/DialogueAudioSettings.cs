using System;
using UnityEngine;

/*
Purpose: Manages d ia lo gu ea ud io se tt in gs behavior for this part of the game.
Attached GameObject: Scene-level audio controller GameObject.
Main responsibilities: Configure or play audio content while keeping scene and UI feedback in sync.
Inputs: Assigned clips, volume settings, and playback requests from other systems.
Outputs or effects: Starts, stops, or configures audible feedback in the scene.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[Serializable]
public class DialogueAudioSettings
{
    [Header("对话开始时切入的BGM（可空）")]
    public AudioClip bgmOnStart;
    public float bgmOnStartFadeOutDuration = 1f;
    public float bgmOnStartFadeInDuration = 1f;
    [Range(0f, 1f)] public float bgmOnStartVolume = 1f;

    [Header("对话结束后切入的BGM（可空）")]
    public AudioClip bgmOnComplete;
    public float bgmOnCompleteFadeOutDuration = 1f;
    public float bgmOnCompleteFadeInDuration = 1f;
    [Range(0f, 1f)] public float bgmOnCompleteVolume = 1f;

    [Header("打字机音效（可空）")]
    public AudioClip typingSfx;
    [Range(0f, 1f)] public float typingSfxVolume = 0.25f;
    [Min(1)] public int typingSfxEveryNCharacters = 2;

    [Header("双引号内容专用音效（可空）")]
    public AudioClip quotedTypingSfx;
    [Range(0f, 1f)] public float quotedTypingSfxVolume = 0.25f;
}
