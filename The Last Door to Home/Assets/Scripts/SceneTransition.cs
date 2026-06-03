using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    public static bool IsTransitioning => instance != null && instance.isTransitioning;

    public static void LoadScene(string sceneName, Action beforeSceneLoad = null)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.");
            return;
        }

        EnsureInstance();
        if (instance == null || instance.isTransitioning) return;
        instance.StartCoroutine(instance.LoadSceneRoutine(sceneName, beforeSceneLoad));
    }

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

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
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action beforeSceneLoad)
    {
        isTransitioning = true;
        canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, fadeOutDuration, false);

        beforeSceneLoad?.Invoke();
        shouldFadeInAfterLoad = true;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldFadeInAfterLoad) return;
        StartCoroutine(FadeInAfterLoad());
    }

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

        yield return Fade(1f, 0f, fadeInDuration, true);
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

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
