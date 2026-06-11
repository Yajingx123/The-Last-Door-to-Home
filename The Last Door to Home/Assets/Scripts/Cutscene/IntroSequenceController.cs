using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
/*
Purpose: Controls a cutscene sequence or stores cutscene return context.
Attached GameObject: Cutscene scene controller GameObject, or static context helper when applicable.
Main responsibilities: Displays slides/text, handles timing and input, plays audio, and transitions to the next scene.
Inputs: Serialized cutscene assets, player input, timing settings, audio clips, and return-scene context.
Outputs or effects: Updates cutscene UI, plays audio, records return data, and loads follow-up scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify slide order, skip/advance input, audio timing, and final scene transition.
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

    [Header("Slides / 幻灯片")]
    public IntroSlide[] slides;

    [Header("UI / 界面")]
    public Image slideImage;
    public TextMeshProUGUI captionText;
    public CanvasGroup contentCanvasGroup;

    [Header("Flow / 流程")]
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

    // Prepares runtime state after the scene has finished its initial setup.
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

    // Reads per-frame input and updates frame-dependent runtime state.
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

    // Handles the advance slide step for this script.
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

    // Applies the requested visual, audio, or gameplay state.
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

    // Fades the related visual element for the fade in opening slide step.
    private IEnumerator FadeInOpeningSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(0f, 1f));
    }

    // Fades the related visual element for the fade out current slide step.
    private IEnumerator FadeOutCurrentSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(1f, 0f));
    }

    // Fades the related visual element for the fade in current slide step.
    private IEnumerator FadeInCurrentSlide()
    {
        yield return StartCoroutine(FadeCurrentSlide(0f, 1f));
    }

    // Fades the related visual element for the fade current slide step.
    private IEnumerator FadeCurrentSlide(float from, float to)
    {
        IEnumerator contentFade = FadeContent(from, to);
        IEnumerator imageFade = FadeSlideImage(from, to);

        while (contentFade.MoveNext() | imageFade.MoveNext())
        {
            yield return null;
        }
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

    // Loads the requested data, scene, or runtime content.
    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("IntroSequenceController: nextSceneName 为空，无法跳转下一场景。", this);
            return;
        }

        SceneTransition.LoadScene(nextSceneName);
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

    // Shows the show caption UI or dialogue flow.
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

    // Handles the type caption routine step for this script.
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

    // Completes the complete caption instantly step immediately.
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
