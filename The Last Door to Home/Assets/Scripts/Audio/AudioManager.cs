using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("默认设置")]
    [SerializeField] private float defaultBgmFadeOutDuration = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultBgmVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float defaultSfxVolume = 1f;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private AudioSource typingSource;
    private AudioSource footstepSource;
    private Coroutine bgmRoutine;

    public AudioClip CurrentBgmClip => bgmSource != null ? bgmSource.clip : null;

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

    private void Initialize()
    {
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

    public void PlayBgm(AudioClip clip, float fadeOutDuration = -1f, float fadeInDuration = -1f, float targetVolume = -1f, bool restartIfSameClip = false)
    {
        Initialize();

        float resolvedFadeOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultBgmFadeOutDuration;
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

        bgmRoutine = StartCoroutine(SwitchBgmRoutine(clip, resolvedFadeOut, resolvedVolume));
    }

    public void StopBgm(float fadeOutDuration = -1f)
    {
        Initialize();

        if (bgmRoutine != null)
        {
            StopCoroutine(bgmRoutine);
        }

        float resolvedFadeOut = fadeOutDuration >= 0f ? fadeOutDuration : defaultBgmFadeOutDuration;
        bgmRoutine = StartCoroutine(SwitchBgmRoutine(null, resolvedFadeOut, 0f));
    }

    public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;

        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        sfxSource.pitch = 1f;
    }

    public void PlayTypingLoop(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;

        float resolvedVolume = Mathf.Clamp01(volumeScale);

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

    public void StopTypingLoop()
    {
        Initialize();

        if (!typingSource.isPlaying) return;

        typingSource.Stop();
        typingSource.clip = null;
        typingSource.pitch = 1f;
    }

    public void PlayFootstep(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        Initialize();

        if (clip == null) return;
        if (footstepSource.isPlaying) return;

        footstepSource.clip = clip;
        footstepSource.volume = Mathf.Clamp01(volumeScale);
        footstepSource.pitch = pitch;
        footstepSource.loop = false;
        footstepSource.Play();
    }

    public void StopFootstep()
    {
        Initialize();

        if (!footstepSource.isPlaying) return;

        footstepSource.Stop();
        footstepSource.clip = null;
        footstepSource.pitch = 1f;
    }

    private IEnumerator SwitchBgmRoutine(AudioClip nextClip, float fadeOutDuration, float targetVolume)
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
        bgmSource.volume = targetVolume;
        bgmSource.Play();
        bgmRoutine = null;
    }

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
