using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

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
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float charactersPerSecond = 28f;

    private int currentIndex;
    private bool isTransitioning;
    private bool isTypingCaption;
    private int sceneStartFrame = -1;
    private float configuredImageHeight = -1f;
    private Coroutine captionTypeRoutine;

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

        currentIndex = 0;
        ApplySlide(currentIndex);
        StartCoroutine(FadeContent(0f, 1f));
    }

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

    private IEnumerator AdvanceSlide()
    {
        isTransitioning = true;

        yield return FadeContent(1f, 0f);

        currentIndex++;
        if (currentIndex >= slides.Length)
        {
            LoadNextScene();
            yield break;
        }

        ApplySlide(currentIndex);
        yield return FadeContent(0f, 1f);

        isTransitioning = false;
    }

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

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("IntroSequenceController: nextSceneName 为空，无法跳转下一场景。", this);
            return;
        }

        SceneTransition.LoadScene(nextSceneName);
    }

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
