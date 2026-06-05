using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/*
Purpose: Manages i nt ro se qu en ce co nt ro ll er behavior for this part of the game.
Attached GameObject: Cutscene controller GameObject.
Main responsibilities: Sequence cutscene presentation, timing, and related audiovisual cues.
Inputs: Inspector configuration, scene references, and runtime method calls.
Outputs or effects: Applies runtime side effects through component state, UI updates, or return values.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class IntroSequenceController : MonoBehaviour
{
    [Serializable]
    public class IntroSlide
    {
        public Sprite image;
        [TextArea(2, 6)]
        public string caption;
    }

    [Header("Slides")]
    public IntroSlide[] slides;

    [Header("UI")]
    public Image slideImage;
    public TextMeshProUGUI captionText;
    public CanvasGroup contentCanvasGroup;

    [Header("Flow")]
    public string nextSceneName;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float imageFadeDuration = 0.45f;
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float charactersPerSecond = 28f;

    private int currentIndex;
    private bool isTransitioning;
    private bool isTypingCaption;
    private int sceneStartFrame = -1;
    private float configuredImageHeight = -1f;
    private Coroutine captionTypeRoutine;

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        sceneStartFrame = Time.frameCount;

        CacheConfiguredImageHeight();

        if (slides == null || slides.Length == 0)
        {
            LoadNextScene();
            return;
        }

        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = 0f;
        }

        SetSlideImageAlpha(0f);

        currentIndex = 0;
        ApplySlide(currentIndex);
        StartCoroutine(FadeInOpeningSlide());
    }

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        if (isTransitioning) return;
        if (Time.frameCount == sceneStartFrame) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTypingCaption)
            {
                CompleteCaptionInstantly();
                return;
            }

            StartCoroutine(AdvanceSlide());
        }
    }

    // Advances the intro sequence to the next configured slide.
    private IEnumerator AdvanceSlide()
    {
        isTransitioning = true;

        yield return FadeOutCurrentSlide();

        currentIndex++;
        if (currentIndex >= slides.Length)
        {
            LoadNextScene();
            yield break;
        }

        ApplySlide(currentIndex);
        yield return FadeInCurrentSlide();

        isTransitioning = false;
    }

    // Applies the current slide visuals, text, and related presentation state.
    private void ApplySlide(int index)
    {
        IntroSlide slide = slides[index];

        if (slideImage != null)
        {
            slideImage.sprite = slide.image;
            slideImage.preserveAspect = true;
            FitImageToAvailableHeight();
        }

        if (captionText != null)
        {
            ShowCaption(slide.caption ?? string.Empty);
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

    // Fades in the opening slide content and image together.
    private IEnumerator FadeInOpeningSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(0f, 1f));
    }

    // Fades out the currently displayed slide before switching to the next one.
    private IEnumerator FadeOutCurrentSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(1f, 0f));
    }

    // Fades in the newly applied slide after the sprite and caption have updated.
    private IEnumerator FadeInCurrentSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(0f, 1f));
    }

    // Fades the slide image and content alpha together for a smoother scene transition.
    private IEnumerator FadeCurrentSlide(float from, float to)
    {
        IEnumerator contentFade = FadeContent(from, to);
        IEnumerator imageFade = FadeSlideImage(from, to);

        while (contentFade.MoveNext() | imageFade.MoveNext())
        {
            yield return null;
        }
    }

    // Fades only the slide image alpha to create a visible dimming effect during image changes.
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

    // Applies the requested alpha directly to the current slide image color.
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

    // Loads the next configured scene after the intro sequence finishes.
    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("IntroSequenceController: nextSceneName 为空，无法跳转下一场景。", this);
            return;
        }

        SceneTransition.LoadScene(nextSceneName);
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

    // Fits the intro image to the currently available layout height.
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

    // Shows the current intro caption and starts its reveal flow.
    private void ShowCaption(string caption)
    {
        if (captionText == null)
        {
            return;
        }

        if (captionTypeRoutine != null)
        {
            StopCoroutine(captionTypeRoutine);
            captionTypeRoutine = null;
        }

        captionText.text = caption;
        captionText.maxVisibleCharacters = 0;
        captionText.ForceMeshUpdate();

        if (!useTypewriterEffect || string.IsNullOrEmpty(caption))
        {
            captionText.maxVisibleCharacters = int.MaxValue;
            isTypingCaption = false;
            return;
        }

        captionTypeRoutine = StartCoroutine(TypeCaptionRoutine(caption));
    }

    // Reveals the intro caption text over time with the configured typing effect.
    private IEnumerator TypeCaptionRoutine(string caption)
    {
        isTypingCaption = true;
        int visibleCount = 0;
        int totalCharacters = caption.Length;
        float interval = 1f / Mathf.Max(1f, charactersPerSecond);
        float elapsed = 0f;

        while (visibleCount < totalCharacters)
        {
            elapsed += Time.unscaledDeltaTime;

            while (elapsed >= interval && visibleCount < totalCharacters)
            {
                elapsed -= interval;
                visibleCount++;
                captionText.maxVisibleCharacters = visibleCount;
            }

            yield return null;
        }

        captionText.maxVisibleCharacters = int.MaxValue;
        isTypingCaption = false;
        captionTypeRoutine = null;
    }

    // Completes the current caption immediately without waiting for typing.
    private void CompleteCaptionInstantly()
    {
        if (!isTypingCaption || captionText == null)
        {
            return;
        }

        if (captionTypeRoutine != null)
        {
            StopCoroutine(captionTypeRoutine);
            captionTypeRoutine = null;
        }

        captionText.maxVisibleCharacters = int.MaxValue;
        isTypingCaption = false;
    }
}
