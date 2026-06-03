using System;
using UnityEngine;

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
