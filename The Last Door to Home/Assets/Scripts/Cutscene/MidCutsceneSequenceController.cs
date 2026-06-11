using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
/*
Purpose: Controls a cutscene sequence or stores cutscene return context.
Attached GameObject: Cutscene scene controller GameObject, or static context helper when applicable.
Main responsibilities: Displays slides/text, handles timing and input, plays audio, and transitions to the next scene.
Inputs: Serialized cutscene assets, player input, timing settings, audio clips, and return-scene context.
Outputs or effects: Updates cutscene UI, plays audio, records return data, and loads follow-up scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify slide order, skip/advance input, audio timing, and final scene transition.
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

    [Header("Beats / 剧情段落")]
    public CutsceneBeat[] beats;

    [Header("UI / 界面")]
    public Image slideImage;
    public CanvasGroup contentCanvasGroup;

    [Header("Dialogue / 对话")]
    public DialogueAudioSettings dialogueAudioSettings;

    [Header("BGM Line Cues / 背景音乐台词触发点")]
    public BgmLineCue[] bgmLineCues;

    [Header("Scene Return / 场景返回")]
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

    // Prepares runtime state after the scene has finished its initial setup.
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

    // Cleans up runtime references before the object is destroyed.
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

    // Begins the begin narration next frame sequence.
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

    // Caches references or values needed by cache bgm line cues.
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

    // Handles the event or callback associated with this method.
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

    // Handles the event or callback associated with this method.
    private void HandleDialogueEnded()
    {
        if (!isSequenceActive) return;

        isSequenceActive = false;
        DialogueManager.DialogueLineShown -= HandleDialogueLineShown;
        DialogueManager.DialogueEnded -= HandleDialogueEnded;
        LoadReturnScene();
    }

    // Fades the related visual element for the fade in opening beat step.
    private IEnumerator FadeInOpeningBeat()
    {
        yield return StartCoroutine(FadeCurrentBeat(0f, 1f));
    }

    // Handles the swap beat image routine step for this script.
    private IEnumerator SwapBeatImageRoutine(int beatIndex)
    {
        yield return StartCoroutine(FadeCurrentBeat(1f, 0f));
        ApplyBeatImageInstantly(beatIndex);
        yield return StartCoroutine(FadeCurrentBeat(0f, 1f));
        imageSwapRoutine = null;
    }

    // Fades the related visual element for the fade current beat step.
    private IEnumerator FadeCurrentBeat(float from, float to)
    {
        IEnumerator contentFade = FadeContent(from, to);
        IEnumerator imageFade = FadeSlideImage(from, to);

        while (contentFade.MoveNext() | imageFade.MoveNext())
        {
            yield return null;
        }
    }

    // Fades the related visual element for the fade content step.
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

    // Fades the related visual element for the fade slide image step.
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

    // Updates the requested value or component state.
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

    // Applies the requested visual, audio, or gameplay state.
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

    // Attempts the requested operation and reports whether it succeeded.
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

    // Loads the requested data, scene, or runtime content.
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

    // Responds when the RectTransform size changes.
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

    // Handles the fit image to available height step for this script.
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

    // Caches references or values needed by cache configured image height.
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
