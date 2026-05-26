using UnityEngine;
using UnityEngine.SceneManagement;
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

    private int currentIndex;
    private bool isTransitioning;
    private int sceneStartFrame = -1;

    void Start()
    {
        sceneStartFrame = Time.frameCount;

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
            slideImage.SetNativeSize();
        }

        if (captionText != null)
        {
            captionText.text = slide.caption ?? string.Empty;
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

        SceneManager.LoadScene(nextSceneName);
    }
}
