using System.Collections;
using UnityEngine;
/*
Purpose: Centralizes music, sound effect, and footstep audio playback.
Attached GameObject: AudioManager GameObject or runtime-created singleton.
Main responsibilities: Creates audio sources, applies volume settings, plays BGM/SFX/footsteps, and persists volume preferences.
Inputs: Audio clips, volume values, scene music requests, and menu volume controls.
Outputs or effects: Controls AudioSource playback and exposes current/default volume state.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify BGM transitions, SFX playback, footstep stopping, and volume persistence.
*/

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("默认设置 / Default Settings")]
    [SerializeField] private float defaultBgmFadeOutDuration = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultBgmVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource typingSource;
    private AudioSource footstepSource;
    private Coroutine bgmRoutine;
    private bool hasCapturedInitialDefaults;
    private float initialBgmVolume;
    private float initialSfxVolume;

    public AudioClip CurrentBgmClip => bgmSource != null ? bgmSource.clip : null;
    public float BgmVolume => defaultBgmVolume;
    public float SfxVolume => defaultSfxVolume;
    public float DefaultBgmVolume => initialBgmVolume;
    public float DefaultSfxVolume => initialSfxVolume;

    // Finds or creates the shared runtime instance used by this system.
    public static AudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        AudioManager found = FindObjectOfType<AudioManager>();
        if (found != null)
        {
            found.Initialize();
            return found;
        }

        GameObject go = new GameObject("AudioManager");
        Instance = go.AddComponent<AudioManager>();
        Instance.Initialize();
        return Instance;
    }

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Initialize();
    }

    // Creates required runtime objects and prepares this system for use.
    private void Initialize()
    {
        if (!hasCapturedInitialDefaults)
        {
            initialBgmVolume = Mathf.Clamp01(defaultBgmVolume);
            initialSfxVolume = Mathf.Clamp01(defaultSfxVolume);
            hasCapturedInitialDefaults = true;
        }

        if (bgmSource != null && sfxSource != null && typingSource != null && footstepSource != null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        bgmSource = GetOrCreateSource("BGM", true);
        sfxSource = GetOrCreateSource("SFX", false);
        typingSource = GetOrCreateSource("TypingSFX", true);
        footstepSource = GetOrCreateSource("FootstepSFX", false);

        bgmSource.volume = defaultBgmVolume;
        sfxSource.volume = defaultSfxVolume;
        typingSource.volume = defaultSfxVolume;
        footstepSource.volume = defaultSfxVolume;
    }

    // Returns the requested value or runtime object.
    private AudioSource GetOrCreateSource(string childName, bool loop)
    {
        Transform child = transform.Find(childName);
        AudioSource source = child != null ? child.GetComponent<AudioSource>() : null;

        if (source == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            source = go.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    // Plays the play bgm sequence or audio feedback.
    public void PlayBgm(AudioClip clip, float fadeOutDuration = -1f, float fadeInDuration = -1f, float targetVolume = -1f, bool restartIfSameClip = false)
    {
        Initialize();

        float resolvedFadeOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultBgmFadeOutDuration;
        float resolvedFadeIn = fadeInDuration >= 0f ? fadeInDuration : defaultBgmFadeOutDuration;
        float resolvedVolume = targetVolume >= 0f ? targetVolume : defaultBgmVolume;

        if (!restartIfSameClip && bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.volume = resolvedVolume;
            return;
        }

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        bgmRoutine = StartCoroutine(SwitchBgmRoutine(clip, resolvedFadeOut, resolvedFadeIn, resolvedVolume));
    }

    // Stops the stop bgm sequence or runtime effect.
    public void StopBgm(float fadeOutDuration = -1f)
    {
        Initialize();

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        float resolvedFadeOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultBgmFadeOutDuration;
        bgmRoutine = StartCoroutine(SwitchBgmRoutine(null, resolvedFadeOut, 0f, 0f));
    }

    // Updates the requested value or component state.
    public void SetBgmVolume(float volume)
    {
        Initialize();

        defaultBgmVolume = Mathf.Clamp01(volume);
        if (bgmSource != null)
        {
            bgmSource.volume = defaultBgmVolume;
        }
    }

    // Updates the requested value or component state.
    public void SetSfxVolume(float volume)
    {
        Initialize();

        defaultSfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = defaultSfxVolume;
        }

        if (typingSource != null)
        {
            typingSource.volume = defaultSfxVolume;
        }

        if (footstepSource != null)
        {
            footstepSource.volume = defaultSfxVolume;
        }
    }

    // Resets the reset bgm volume to default state to its default value.
    public void ResetBgmVolumeToDefault()
    {
        SetBgmVolume(initialBgmVolume);
    }

    // Resets the reset sfx volume to default state to its default value.
    public void ResetSfxVolumeToDefault()
    {
        SetSfxVolume(initialSfxVolume);
    }

    // Plays the play sfx sequence or audio feedback.
    public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        sfxSource.pitch = 1f;
    }

    // Plays the play typing loop sequence or audio feedback.
    public void PlayTypingLoop(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;

        float resolvedVolume = Mathf.Clamp01(volumeScale) * defaultSfxVolume;

        if (typingSource.isPlaying && typingSource.clip == clip)
        {
            typingSource.volume = resolvedVolume;
            typingSource.pitch = pitch;
            return;
        }

        typingSource.Stop();
        typingSource.clip = clip;
        typingSource.volume = resolvedVolume;
        typingSource.pitch = pitch;
        typingSource.loop = true;
        typingSource.Play();
    }

    // Stops the stop typing loop sequence or runtime effect.
    public void StopTypingLoop()
    {
        Initialize();

        if (!typingSource.isPlaying) return;

        typingSource.Stop();
        typingSource.clip = null;
        typingSource.pitch = 1f;
    }

    // Plays the play footstep sequence or audio feedback.
    public void PlayFootstep(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;
        if (footstepSource.isPlaying) return;

        footstepSource.clip = clip;
        footstepSource.volume = Mathf.Clamp01(volumeScale) * defaultSfxVolume;
        footstepSource.pitch = pitch;
        footstepSource.loop = false;
        footstepSource.Play();
    }

    // Stops the stop footstep sequence or runtime effect.
    public void StopFootstep()
    {
        Initialize();

        if (!footstepSource.isPlaying) return;

        footstepSource.Stop();
        footstepSource.clip = null;
        footstepSource.pitch = 1f;
    }

    // Switches to the target scene or state for switch bgm routine.
    private IEnumerator SwitchBgmRoutine(AudioClip nextClip, float fadeOutDuration, float fadeInDuration, float targetVolume)
    {
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            yield return FadeBgm(bgmSource.volume, 0f, fadeOutDuration);
            bgmSource.Stop();
            bgmSource.clip = null;
        }

        if (nextClip == null)
        {
            bgmRoutine = null;
            yield break;
        }

        bgmSource.clip = nextClip;
        bgmSource.volume = fadeInDuration > 0f ? 0f : targetVolume;
        bgmSource.Play();

        if (fadeInDuration > 0f)
        {
            yield return FadeBgm(0f, targetVolume, fadeInDuration);
        }

        bgmRoutine = null;
    }

    // Fades the related visual element for the fade bgm step.
    private IEnumerator FadeBgm(float from, float to, float duration)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        if (duration <= 0f)
        {
            bgmSource.volume = to;
            yield break;
        }

        float elapsed = 0f;
        bgmSource.volume = from;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            bgmSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        bgmSource.volume = to;
    }
}
