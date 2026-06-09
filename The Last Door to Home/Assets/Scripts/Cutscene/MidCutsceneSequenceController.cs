using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

/*
Purpose: Manages mid-game cutscene playback through the shared dialogue system and returns to the previous gameplay scene.
Attached GameObject: Mid-cutscene controller GameObject.
Main responsibilities: Coordinate narration lines, beat images, line-based BGM changes, and the return-to-scene flow.
Inputs: Inspector configuration, dialogue callbacks, scene-return context, and runtime UI references.
Outputs or effects: Shows cutscene images, routes narration through DialogueManager, switches BGM, and restores gameplay scene state.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify dialogue progression, line-based BGM changes, and scene return with restored player position after playback completes.
*/

public class MidCutsceneSequenceController : MonoBehaviour
{
    public enum ReturnSceneMode
    {
        SavedSceneIfAvailable,
        ExplicitScene
    }

    [Serializable]
    public class CutsceneBeat
    {
        public Sprite image;
        [TextArea(2, 6)]
        public string narration;
    }

    [Serializable]
    public class BgmLineCue
    {
        [Min(0)] public int lineNumber = 0;
        public AudioClip bgmClip;
        public float fadeOutDuration = 1f;
        public float fadeInDuration = 1f;
        [Range(0f, 1f)] public float volume = 1f;
        public bool restartIfSameClip = false;
    }

    [Header("Beats")]
    public CutsceneBeat[] beats;

    [Header("UI")]
    public Image slideImage;
    public CanvasGroup contentCanvasGroup;

    [Header("Dialogue")]
    public DialogueAudioSettings dialogueAudioSettings;

    [Header("BGM Line Cues")]
    public BgmLineCue[] bgmLineCues;

    [Header("Scene Return")]
    [SerializeField] private ReturnSceneMode returnSceneMode = ReturnSceneMode.SavedSceneIfAvailable;
    public string fallbackSceneName;
    public Vector2 fallbackSpawnPosition;

    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float imageFadeDuration = 0.45f;

    private readonly Dictionary<int, BgmLineCue> bgmCueByLineNumber = new Dictionary<int, BgmLineCue>();

    private int currentIndex;
    private float configuredImageHeight = -1f;
    private bool isSequenceActive;
    private Coroutine imageSwapRoutine;

    // Prepares runtime state after the scene finishes its initial setup.
    private void Start()
    {
        CacheConfiguredImageHeight();
        CacheBgmLineCues();

        if (beats == null || beats.Length == 0)
        {
            LoadReturnScene();
            return;
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0f;
        }

        SetSlideImageAlpha(0f);
        currentIndex = 0;
        ApplyBeatImageInstantly(currentIndex);
        StartCoroutine(FadeInOpeningBeat());

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("MidCutsceneSequenceController: DialogueManager.Instance 为空，无法播放中场旁白。", this);
            return;
        }

        DialogueManager.DialogueLineShown += HandleDialogueLineShown;
        DialogueManager.DialogueEnded += HandleDialogueEnded;
        isSequenceActive = true;
        StartCoroutine(BeginNarrationNextFrame());
    }

    // Cleans up event subscriptions if this cutscene controller is destroyed mid-playback.
    private void OnDestroy()
    {
        DialogueManager.DialogueLineShown -= HandleDialogueLineShown;
        DialogueManager.DialogueEnded -= HandleDialogueEnded;
    }

    // Builds the narration array consumed by the shared dialogue system.
    private string[] BuildNarrationLines()
    {
        string[] lines = new string[beats.Length];

        for (int i = 0; i < beats.Length; i++)
        {
            lines[i] = beats[i] != null ? beats[i].narration ?? string.Empty : string.Empty;
        }

        return lines;
    }

    // Starts narration one frame later so the dialogue UI is fully ready on fresh scene load.
    private IEnumerator BeginNarrationNextFrame()
    {
        yield return null;

        if (!isSequenceActive || DialogueManager.Instance == null)
        {
            yield break;
        }

        DialogueManager.Instance.ShowDialogue(
            BuildNarrationLines(),
            null,
            null,
            null,
            dialogueAudioSettings
        );
    }

    // Caches line-to-BGM mappings for fast lookup while the dialogue advances.
    private void CacheBgmLineCues()
    {
        bgmCueByLineNumber.Clear();
        if (bgmLineCues == null) return;

        for (int i = 0; i < bgmLineCues.Length; i++)
        {
            BgmLineCue cue = bgmLineCues[i];
            if (cue == null) continue;

            int safeLineNumber = Mathf.Max(0, cue.lineNumber);
            bgmCueByLineNumber[safeLineNumber] = cue;
        }
    }

    // Responds when the dialogue system shows a new line for this cutscene.
    private void HandleDialogueLineShown(string lineText, int lineIndex, int totalLines)
    {
        if (!isSequenceActive) return;
        if (lineIndex < 0 || lineIndex >= beats.Length) return;

        bool shouldAnimateImage = lineIndex != currentIndex;
        currentIndex = lineIndex;

        if (shouldAnimateImage)
        {
            if (imageSwapRoutine != null)
            {
                StopCoroutine(imageSwapRoutine);
                imageSwapRoutine = null;
            }

            imageSwapRoutine = StartCoroutine(SwapBeatImageRoutine(currentIndex));
        }
        else
        {
            ApplyBeatImageInstantly(currentIndex);
        }

        TryApplyBgmCue(lineIndex);
    }

    // Returns to gameplay once this cutscene-owned dialogue finishes.
    private void HandleDialogueEnded()
    {
        if (!isSequenceActive) return;

        isSequenceActive = false;
        DialogueManager.DialogueLineShown -= HandleDialogueLineShown;
        DialogueManager.DialogueEnded -= HandleDialogueEnded;
        LoadReturnScene();
    }

    // Fades in the first beat after scene load.
    private IEnumerator FadeInOpeningBeat()
    {
        yield return StartCoroutine(FadeCurrentBeat(0f, 1f));
    }

    // Swaps the current beat image with the usual fade timing.
    private IEnumerator SwapBeatImageRoutine(int beatIndex)
    {
        yield return StartCoroutine(FadeCurrentBeat(1f, 0f));
        ApplyBeatImageInstantly(beatIndex);
        yield return StartCoroutine(FadeCurrentBeat(0f, 1f));
        imageSwapRoutine = null;
    }

    // Fades the image panel and image alpha together.
    private IEnumerator FadeCurrentBeat(float from, float to)
    {
        IEnumerator contentFade = FadeContent(from, to);
        IEnumerator imageFade = FadeSlideImage(from, to);

        while (contentFade.MoveNext() | imageFade.MoveNext())
        {
            yield return null;
        }
    }

    // Fades the cutscene content group over the requested duration.
    private IEnumerator FadeContent(float from, float to)
    {
        if (contentCanvasGroup == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, fadeDuration);
        float elapsed = 0f;
        contentCanvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            contentCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        contentCanvasGroup.alpha = to;
    }

    // Fades the beat image alpha for smoother visual transitions.
    private IEnumerator FadeSlideImage(float from, float to)
    {
        if (slideImage == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, imageFadeDuration);
        float elapsed = 0f;
        SetSlideImageAlpha(from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetSlideImageAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetSlideImageAlpha(to);
    }

    // Applies the requested alpha directly to the current beat image.
    private void SetSlideImageAlpha(float alpha)
    {
        if (slideImage == null)
        {
            return;
        }

        Color color = slideImage.color;
        color.a = Mathf.Clamp01(alpha);
        slideImage.color = color;
    }

    // Applies the beat image immediately without any transition.
    private void ApplyBeatImageInstantly(int beatIndex)
    {
        if (slideImage == null) return;
        if (beats == null || beatIndex < 0 || beatIndex >= beats.Length) return;

        CutsceneBeat beat = beats[beatIndex];
        slideImage.sprite = beat != null ? beat.image : null;
        slideImage.preserveAspect = true;

        if (slideImage.sprite != null)
        {
            FitImageToAvailableHeight();
        }
    }

    // Starts the configured BGM change when the current narration line has a cue.
    private void TryApplyBgmCue(int lineNumber)
    {
        if (!bgmCueByLineNumber.TryGetValue(lineNumber, out BgmLineCue cue))
        {
            return;
        }

        if (cue.bgmClip == null)
        {
            AudioManager.EnsureInstance().StopBgm(cue.fadeOutDuration);
            return;
        }

        AudioManager.EnsureInstance().PlayBgm(
            cue.bgmClip,
            cue.fadeOutDuration,
            cue.fadeInDuration,
            cue.volume,
            cue.restartIfSameClip
        );
    }

    // Returns either to the saved gameplay scene or to an explicitly configured scene, depending on the selected mode.
    private void LoadReturnScene()
    {
        if (returnSceneMode == ReturnSceneMode.SavedSceneIfAvailable && CutsceneReturnContext.HasSavedContext)
        {
            string returnSceneName = CutsceneReturnContext.GetReturnSceneName();
            SceneTransition.LoadScene(returnSceneName, () =>
            {
                CutsceneReturnContext.ApplyReturnSpawn();
                CutsceneReturnContext.Clear();
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(fallbackSceneName))
        {
            Debug.LogWarning("MidCutsceneSequenceController: 没有配置可用的返回场景。", this);
            return;
        }

        CutsceneReturnContext.Clear();
        SceneTransition.LoadScene(fallbackSceneName, () =>
        {
            PlayerSpawn.SPAWN_POSITION = fallbackSpawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
        });
    }

    // Refreshes layout-sensitive visuals after the rect transform changes size.
    private void OnRectTransformDimensionsChange()
    {
        if (slideImage != null && slideImage.sprite != null)
        {
            if (configuredImageHeight <= 0f)
            {
                CacheConfiguredImageHeight();
            }

            FitImageToAvailableHeight();
        }
    }

    // Fits the cutscene image to the currently available layout height.
    private void FitImageToAvailableHeight()
    {
        if (slideImage == null || slideImage.sprite == null)
        {
            return;
        }

        RectTransform imageRect = slideImage.rectTransform;
        float targetHeight = configuredImageHeight;
        if (targetHeight <= 0f)
        {
            targetHeight = imageRect.rect.height;
            configuredImageHeight = targetHeight;
        }
        if (targetHeight <= 0f) return;

        Rect spriteRect = slideImage.sprite.rect;
        if (spriteRect.height <= 0f)
        {
            return;
        }

        float aspect = spriteRect.width / spriteRect.height;
        float targetWidth = targetHeight * aspect;

        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        imageRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    // Caches the configured image height used by the cutscene layout.
    private void CacheConfiguredImageHeight()
    {
        if (slideImage == null)
        {
            return;
        }

        float height = slideImage.rectTransform.rect.height;
        if (height > 0f)
        {
            configuredImageHeight = height;
        }
    }
}
