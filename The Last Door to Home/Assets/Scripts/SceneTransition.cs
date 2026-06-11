using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/*
Purpose: Provides a shared fade transition for scene loading.
Attached GameObject: SceneTransition GameObject or runtime-created singleton.
Main responsibilities: Creates the fade overlay, blocks input during transitions, loads scenes, and fades in after loading.
Inputs: Scene names, optional before-load callbacks, fade colors, durations, and scene-loaded events.
Outputs or effects: Updates transition UI alpha, invokes callbacks, and loads Unity scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify repeated load requests are blocked and fade timing behaves correctly across scenes.
*/

public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;

    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.55f;
    [SerializeField] private float postLoadBlackHoldDuration = 0.12f;
    [SerializeField] private Color fadeColor = Color.black;

    private Canvas transitionCanvas;
    private CanvasGroup canvasGroup;
    private Image fadeImage;
    private bool isTransitioning;
    private bool shouldFadeInAfterLoad;
    private float activeFadeInDuration;

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    // Loads the requested data, scene, or runtime content.
    public static void LoadScene(string sceneName, Action beforeSceneLoad = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.");
            return;
        }

        EnsureInstance();
        if (instance == null || instance.isTransitioning) return;
        instance.StartCoroutine(instance.LoadSceneRoutine(sceneName, beforeSceneLoad, instance.fadeColor, instance.fadeOutDuration, instance.fadeInDuration));
    }

    // Loads the requested data, scene, or runtime content.
    public static void LoadSceneWithFadeColor(string sceneName, Color transitionFadeColor, Action beforeSceneLoad = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.");
            return;
        }

        EnsureInstance();
        if (instance == null || instance.isTransitioning) return;
        instance.StartCoroutine(instance.LoadSceneRoutine(sceneName, beforeSceneLoad, transitionFadeColor, instance.fadeOutDuration, instance.fadeInDuration));
    }

    // Loads the requested data, scene, or runtime content.
    public static void LoadSceneWithFadeColor(string sceneName, Color transitionFadeColor, float customFadeOutDuration, float customFadeInDuration, Action beforeSceneLoad = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.");
            return;
        }

        EnsureInstance();
        if (instance == null || instance.isTransitioning) return;
        instance.StartCoroutine(instance.LoadSceneRoutine(sceneName, beforeSceneLoad, transitionFadeColor, customFadeOutDuration, customFadeInDuration));
    }

    // Finds or creates the shared runtime instance used by this system.
    private static void EnsureInstance()
    {
        if (instance != null) return;

        instance = FindObjectOfType<SceneTransition>();
        if (instance != null)
        {
            instance.Initialize();
            return;
        }

        GameObject go = new GameObject("SceneTransition");
        instance = go.AddComponent<SceneTransition>();
        instance.Initialize();
    }

    // Initializes component references and singleton ownership before Start runs.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    // Registers callbacks or resets transient state when the component becomes active.
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Unregisters callbacks when the component becomes inactive.
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Creates required runtime objects and prepares this system for use.
    private void Initialize()
    {
        if (transitionCanvas != null)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        transitionCanvas = gameObject.GetComponent<Canvas>();
        if (transitionCanvas == null)
        {
            transitionCanvas = gameObject.AddComponent<Canvas>();
        }

        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        GameObject imageObject = new GameObject("Fade");
        imageObject.transform.SetParent(transform, false);
        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        activeFadeInDuration = fadeInDuration;
    }

    // Loads the requested data, scene, or runtime content.
    private IEnumerator LoadSceneRoutine(string sceneName, Action beforeSceneLoad, Color transitionFadeColor, float customFadeOutDuration, float customFadeInDuration)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;
        activeFadeInDuration = Mathf.Max(0.01f, customFadeInDuration);
        if (fadeImage != null)
        {
            fadeImage.color = transitionFadeColor;
        }

        yield return Fade(0f, 1f, customFadeOutDuration, false);

        beforeSceneLoad?.Invoke();
        shouldFadeInAfterLoad = true;
        SceneManager.LoadScene(sceneName);
    }

    // Handles the on scene loaded step for this script.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldFadeInAfterLoad) return;
        StartCoroutine(FadeInAfterLoad());
    }

    // Fades the related visual element for the fade in after load step.
    private IEnumerator FadeInAfterLoad()
    {
        shouldFadeInAfterLoad = false;
        canvasGroup.alpha = 1f;
        yield return new WaitForEndOfFrame();

        float holdDuration = Mathf.Max(0f, postLoadBlackHoldDuration);
        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return Fade(1f, 0f, activeFadeInDuration, true);
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    // Fades the related visual element for the fade step.
    private IEnumerator Fade(float from, float to, float duration, bool easeOut)
    {
        if (canvasGroup == null) yield break;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            if (easeOut)
            {
                t = 1f - Mathf.Pow(1f - t, 3f);
            }
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
