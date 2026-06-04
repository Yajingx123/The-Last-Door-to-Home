using UnityEngine;

/*
Purpose: Manages s ce ne bg mp la ye r behavior for this part of the game.
Attached GameObject: Scene-level audio controller GameObject.
Main responsibilities: Configure or play audio content while keeping scene and UI feedback in sync.
Inputs: Assigned clips, volume settings, and playback requests from other systems.
Outputs or effects: Starts, stops, or configures audible feedback in the scene.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class SceneBgmPlayer : MonoBehaviour
{
    [Header("进入场景自动播放")]
    public AudioClip bgmClip;
    public float fadeOutDuration = 1f;
    public float fadeInDuration = 1f;
    [Range(0f, 1f)] public float volume = 1f;
    public bool restartIfSameClip = false;

    // Prepares runtime state after the scene finishes its initial setup.
    private void Start()
    {
        if (bgmClip == null) return;
        AudioManager.EnsureInstance().PlayBgm(bgmClip, fadeOutDuration, fadeInDuration, volume, restartIfSameClip);
    }
}
