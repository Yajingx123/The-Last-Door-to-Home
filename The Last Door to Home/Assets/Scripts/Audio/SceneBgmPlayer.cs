using UnityEngine;
/*
Purpose: Starts the configured background music for a scene.
Attached GameObject: Scene-level GameObject in scenes that need specific BGM.
Main responsibilities: Sends the configured clip and playback settings to AudioManager when the scene starts.
Inputs: Serialized AudioClip and playback options.
Outputs or effects: Starts or updates background music playback.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each scene starts the intended music and does not restart unnecessarily.
*/

public class SceneBgmPlayer : MonoBehaviour
{
    [Header("进入场景自动播放 / Auto Play On Scene Enter")]
    public AudioClip bgmClip;
    public float fadeOutDuration = 1f;
    public float fadeInDuration = 1f;
    [Range(0f, 1f)] public float volume = 1f;
    public bool restartIfSameClip = false;

    // Prepares runtime state after the scene has finished its initial setup.
    private void Start()
    {
        if (bgmClip == null) return;
        AudioManager.EnsureInstance().PlayBgm(bgmClip, fadeOutDuration, fadeInDuration, volume, restartIfSameClip);
    }
}
