using UnityEngine;

public class SceneBgmPlayer : MonoBehaviour
{
    [Header("进入场景自动播放")]
    public AudioClip bgmClip;
    public float fadeOutDuration = 1f;
    public float fadeInDuration = 1f;
    [Range(0f, 1f)] public float volume = 1f;
    public bool restartIfSameClip = false;

    private void Start()
    {
        if (bgmClip == null) return;
        AudioManager.EnsureInstance().PlayBgm(bgmClip, fadeOutDuration, fadeInDuration, volume, restartIfSameClip);
    }
}
