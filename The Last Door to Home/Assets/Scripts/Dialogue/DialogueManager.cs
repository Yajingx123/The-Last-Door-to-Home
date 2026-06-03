using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public static event Action DialogueStarted;
    public static event Action<string, int, int> DialogueLineShown;
    public static event Action DialogueEnded;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("对话框开关动画")]
    [SerializeField] private float panelAnimDuration = 0.18f;
    [SerializeField] private AnimationCurve panelAnimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("打字机效果")]
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float charactersPerSecond = 20f;
    [SerializeField] private float commaPauseSeconds = 0.12f;
    [SerializeField] private float periodPauseSeconds = 0.22f;
    [SerializeField] private float ellipsisDotPauseSeconds = 0.16f;
    [SerializeField] private float ellipsisEndPauseSeconds = 0.4f;
    [SerializeField] private AudioClip defaultTypingSfx;
    [SerializeField] [Range(0f, 1f)] private float defaultTypingSfxVolume = 0.2f;
    [SerializeField] private AudioClip defaultQuotedTypingSfx;
    [SerializeField] [Range(0f, 1f)] private float defaultQuotedTypingSfxVolume = 0.2f;

    [Header("玩家控制（拖Player物体）")]
    public GameObject player;

    [Header("对话插图（可选）")]
    public GameObject dialogueImagePanel;
    public Image dialogueImage;
    [SerializeField] private float imageAnimDuration = 0.2f;
    [SerializeField] private float imageRiseDistance = 28f;

    private Rigidbody2D playerRb;
    private string[] currentDialogues;
    private int dialogueIndex;
    private bool isDialogueActive;
    private bool isWaitingForOptionChoice;
    private int dialogueStartFrame = -1;
    private int dialogueEndFrame = -1;

    public bool IsDialogueActive => isDialogueActive;
    public bool IsPlayerControlLocked => isDialogueActive || isWaitingForOptionChoice;
    public bool CanStartInteraction =>
        !isDialogueActive &&
        !isWaitingForOptionChoice &&
        Time.frameCount != dialogueEndFrame;

    private Action pendingOptionAction;
    private Action pendingDialogueCompleteAction;
    private RectTransform dialoguePanelRect;
    private Vector3 dialoguePanelBaseScale = Vector3.one;
    private Coroutine panelAnimRoutine;

    private RectTransform dialogueImageRect;
    private CanvasGroup dialogueImageCanvasGroup;
    private Coroutine imageAnimRoutine;
    private Coroutine typewriterRoutine;
    private Vector2 imageBaseAnchoredPos;
    private bool hasImageBaseAnchoredPos;
    private bool isDialogueImageActive;
    private MonoBehaviour[] cachedMovementScripts;
    private DialogueAudioSettings activeDialogueAudioSettings;
    private bool isTypingLine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialoguePanel != null)
        {
            dialoguePanelRect = dialoguePanel.GetComponent<RectTransform>();
            if (dialoguePanelRect != null)
            {
                dialoguePanelBaseScale = dialoguePanelRect.localScale;
                dialoguePanelRect.pivot = new Vector2(0.5f, 0.5f);
            }
            dialoguePanel.SetActive(false);
        }

        ResolvePlayerReference();
        SetupDialogueImageUI();
        CachePlayerMovementScripts();
    }

    IEnumerator Start()
    {
        yield return null;
        PrewarmDialogueUI();
    }

    void OnDestroy()
    {
        StopTypingSfxImmediate();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ResolvePlayerReference()
    {
        if (player == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                player = taggedPlayer;
            }
        }

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            return;
        }

        Debug.LogWarning("DialogueManager：未赋值Player物体，且未找到Tag=Player的对象。", this);
    }

    private void CachePlayerMovementScripts()
    {
        if (player == null) return;
        cachedMovementScripts = player.GetComponents<MonoBehaviour>();
    }

    private void PrewarmDialogueUI()
    {
        if (dialogueText != null)
        {
            dialogueText.text = " ";
            dialogueText.ForceMeshUpdate();
            dialogueText.text = "";
        }

        if (dialoguePanel != null)
        {
            bool wasActive = dialoguePanel.activeSelf;
            dialoguePanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            dialoguePanel.SetActive(wasActive);
        }
    }

    void Update()
    {
        if (isDialogueActive && !isWaitingForOptionChoice && Input.GetKeyDown(KeyCode.Return))
        {
            if (Time.frameCount == dialogueStartFrame) return;
            if (isTypingLine)
            {
                CompleteCurrentLineInstantly();
                return;
            }
            AdvanceDialogue();
        }
    }

    public void ShowDialogue(string[] texts, PickableItem item = null, Action onLastLineOption = null, Action onDialogueComplete = null, DialogueAudioSettings audioSettings = null)
    {
        if (isDialogueActive) return;
        if (texts == null || texts.Length == 0) return;

        pendingDialogueCompleteAction = onDialogueComplete;
        pendingOptionAction = onLastLineOption;
        if (pendingOptionAction == null && item != null)
        {
            pendingOptionAction = () =>
            {
                item.ShowPickOptionsMenu();
            };
        }

        currentDialogues = texts;
        dialogueIndex = 0;
        isDialogueActive = true;
        dialogueStartFrame = Time.frameCount;
        activeDialogueAudioSettings = audioSettings;

        if (audioSettings != null && audioSettings.bgmOnStart != null)
        {
            AudioManager.EnsureInstance().PlayBgm(
                audioSettings.bgmOnStart,
                audioSettings.bgmOnStartFadeOutDuration,
                audioSettings.bgmOnStartFadeInDuration,
                audioSettings.bgmOnStartVolume
            );
        }

        PlayPanelOpenAnim();
        EnsureDialoguePanelInFrontOfImage();
        ShowCurrentDialogueLine();
        LockPlayer(true);
        DialogueStarted?.Invoke();
    }

    void AdvanceDialogue()
    {
        if (dialogueIndex >= currentDialogues.Length - 1)
        {
            if (pendingOptionAction != null)
            {
                ShowOptionWithLastLine();
                return;
            }

            EndDialogue();
        }
        else
        {
            dialogueIndex++;
            ShowCurrentDialogueLine();
        }
    }

    void ShowOptionWithLastLine()
    {
        if (isWaitingForOptionChoice) return;

        if (OptionMenu.Instance != null)
        {
            isWaitingForOptionChoice = true;
            pendingOptionAction?.Invoke();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        isWaitingForOptionChoice = false;
        dialogueEndFrame = Time.frameCount;

        PlayPanelCloseAnim();
        dialogueText.text = "";
        LockPlayer(false);
        HideDialogueImage();
        StopTypingSfxImmediate();

        pendingOptionAction = null;
        Action dialogueCompleteAction = pendingDialogueCompleteAction;
        pendingDialogueCompleteAction = null;
        DialogueAudioSettings completedAudioSettings = activeDialogueAudioSettings;
        activeDialogueAudioSettings = null;

        if (completedAudioSettings != null && completedAudioSettings.bgmOnComplete != null)
        {
            AudioManager.EnsureInstance().PlayBgm(
                completedAudioSettings.bgmOnComplete,
                completedAudioSettings.bgmOnCompleteFadeOutDuration,
                completedAudioSettings.bgmOnCompleteFadeInDuration,
                completedAudioSettings.bgmOnCompleteVolume
            );
        }

        DialogueEnded?.Invoke();
        dialogueCompleteAction?.Invoke();
    }

    public void CloseDialogueAfterOption()
    {
        if (isWaitingForOptionChoice)
        {
            EndDialogue();
        }
    }

    public void ContinueDialogueAfterOption(string[] texts, PickableItem item = null, Action onLastLineOption = null, Action onDialogueComplete = null, DialogueAudioSettings audioSettings = null)
    {
        if (texts == null || texts.Length == 0)
        {
            CloseDialogueAfterOption();
            return;
        }

        if (!isDialogueActive)
        {
            ShowDialogue(texts, item, onLastLineOption, onDialogueComplete, audioSettings);
            return;
        }

        pendingDialogueCompleteAction = onDialogueComplete;
        pendingOptionAction = onLastLineOption;
        if (pendingOptionAction == null && item != null)
        {
            pendingOptionAction = () =>
            {
                item.ShowPickOptionsMenu();
            };
        }

        isWaitingForOptionChoice = false;
        if (audioSettings != null)
        {
            activeDialogueAudioSettings = audioSettings;
            if (audioSettings.bgmOnStart != null)
            {
                AudioManager.EnsureInstance().PlayBgm(
                    audioSettings.bgmOnStart,
                    audioSettings.bgmOnStartFadeOutDuration,
                    audioSettings.bgmOnStartFadeInDuration,
                    audioSettings.bgmOnStartVolume
                );
            }
        }

        currentDialogues = texts;
        dialogueIndex = 0;
        dialogueStartFrame = Time.frameCount;
        ShowCurrentDialogueLine();
    }

    public void ShowDialogueImage(Sprite sprite)
    {
        if (sprite == null)
        {
            HideDialogueImage();
            return;
        }

        SetupDialogueImageUI();
        if (dialogueImagePanel == null || dialogueImage == null)
        {
            Debug.LogWarning("DialogueManager: 对话插图引用不完整，ShowDialogueImage 被跳过。", this);
            return;
        }

        dialogueImage.sprite = sprite;
        dialogueImage.type = Image.Type.Simple;
        dialogueImage.preserveAspect = true;
        dialogueImage.SetNativeSize();
        dialogueImagePanel.SetActive(true);
        EnsureDialoguePanelInFrontOfImage();
        PlayDialogueImageAnim(true);
    }

    public void HideDialogueImage()
    {
        if (dialogueImagePanel == null || dialogueImage == null) return;
        if (!isDialogueImageActive && !dialogueImagePanel.activeSelf) return;
        PlayDialogueImageAnim(false);
    }

    public void LockPlayer(bool lockIt)
    {
        if (playerRb == null || player == null) return;

        playerRb.velocity = Vector2.zero;

        PlayerMove playerMove = player.GetComponent<PlayerMove>();
        if (lockIt && playerMove != null)
        {
            playerMove.ForceStopImmediate();
        }

        if (cachedMovementScripts == null || cachedMovementScripts.Length == 0)
        {
            CachePlayerMovementScripts();
        }

        foreach (var script in cachedMovementScripts)
        {
            if (script == null) continue;
            if (script.GetType().Name.Contains("Move") || script.GetType().Name.Contains("Movement"))
            {
                script.enabled = !lockIt;
            }
        }
    }

    void PlayPanelOpenAnim()
    {
        if (dialoguePanel == null) return;

        if (panelAnimRoutine != null)
        {
            StopCoroutine(panelAnimRoutine);
            panelAnimRoutine = null;
        }

        dialoguePanel.SetActive(true);
        panelAnimRoutine = StartCoroutine(AnimatePanelScaleY(0f, 1f, false));
    }

    void PlayPanelCloseAnim()
    {
        if (dialoguePanel == null) return;

        if (panelAnimRoutine != null)
        {
            StopCoroutine(panelAnimRoutine);
            panelAnimRoutine = null;
        }

        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        isTypingLine = false;
        StopTypingSfxImmediate();

        if (!dialoguePanel.activeSelf)
        {
            dialoguePanel.SetActive(false);
            return;
        }

        panelAnimRoutine = StartCoroutine(AnimatePanelScaleY(1f, 0f, true));
    }

    IEnumerator AnimatePanelScaleY(float fromY, float toY, bool deactivateOnFinish)
    {
        if (dialoguePanelRect == null)
        {
            dialoguePanel.SetActive(!deactivateOnFinish);
            yield break;
        }

        float duration = Mathf.Max(0.01f, panelAnimDuration);
        float t = 0f;

        SetPanelScaleY(fromY);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float eased = panelAnimCurve != null ? panelAnimCurve.Evaluate(normalized) : normalized;
            SetPanelScaleY(Mathf.LerpUnclamped(fromY, toY, eased));
            yield return null;
        }

        SetPanelScaleY(toY);

        if (deactivateOnFinish)
        {
            dialoguePanel.SetActive(false);
            SetPanelScaleY(1f);
        }

        panelAnimRoutine = null;
    }

    void SetPanelScaleY(float scaleY01)
    {
        if (dialoguePanelRect == null) return;

        dialoguePanelRect.localScale = new Vector3(
            dialoguePanelBaseScale.x,
            dialoguePanelBaseScale.y * scaleY01,
            dialoguePanelBaseScale.z
        );
    }

    void EnsureDialoguePanelInFrontOfImage()
    {
        if (dialoguePanel == null || dialogueImagePanel == null) return;
        if (dialoguePanel.transform.parent != dialogueImagePanel.transform.parent) return;
        dialoguePanel.transform.SetAsLastSibling();
    }

    void SetupDialogueImageUI()
    {
        if (dialogueImagePanel == null)
        {
            Debug.LogWarning("DialogueManager: dialogueImagePanel 未绑定，无法显示对话插图。", this);
            return;
        }

        if (dialogueImage == null)
        {
            dialogueImage = dialogueImagePanel.GetComponentInChildren<Image>(true);
            if (dialogueImage == null)
            {
                Debug.LogWarning("DialogueManager: dialogueImage 未绑定，且在 dialogueImagePanel 子层级中未找到 Image。", this);
                return;
            }
        }

        dialogueImageRect = dialogueImagePanel.GetComponent<RectTransform>();
        if (dialogueImageRect != null && !hasImageBaseAnchoredPos)
        {
            imageBaseAnchoredPos = dialogueImageRect.anchoredPosition;
            hasImageBaseAnchoredPos = true;
        }

        dialogueImageCanvasGroup = dialogueImagePanel.GetComponent<CanvasGroup>();
        if (dialogueImageCanvasGroup == null)
        {
            dialogueImageCanvasGroup = dialogueImagePanel.AddComponent<CanvasGroup>();
        }

        dialogueImageCanvasGroup.alpha = 0f;
        dialogueImagePanel.SetActive(false);
        isDialogueImageActive = false;
    }

    void PlayDialogueImageAnim(bool show)
    {
        if (dialogueImagePanel == null || dialogueImageRect == null || dialogueImageCanvasGroup == null) return;

        if (imageAnimRoutine != null)
        {
            StopCoroutine(imageAnimRoutine);
            imageAnimRoutine = null;
        }

        imageAnimRoutine = StartCoroutine(AnimateDialogueImage(show));
    }

    IEnumerator AnimateDialogueImage(bool show)
    {
        if (show) dialogueImagePanel.SetActive(true);

        float duration = Mathf.Max(0.01f, imageAnimDuration);
        float elapsed = 0f;

        float startAlpha = dialogueImageCanvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        Vector2 hiddenPos = imageBaseAnchoredPos - new Vector2(0f, imageRiseDistance);
        Vector2 startPos = dialogueImageRect.anchoredPosition;
        Vector2 targetPos = show ? imageBaseAnchoredPos : hiddenPos;

        if (show)
        {
            startPos = hiddenPos;
            dialogueImageRect.anchoredPosition = startPos;
            startAlpha = 0f;
            dialogueImageCanvasGroup.alpha = 0f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            dialogueImageCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            dialogueImageRect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, t);
            yield return null;
        }

        dialogueImageCanvasGroup.alpha = targetAlpha;
        dialogueImageRect.anchoredPosition = targetPos;
        isDialogueImageActive = show;

        if (!show)
        {
            dialogueImagePanel.SetActive(false);
            dialogueImage.sprite = null;
            dialogueImageRect.anchoredPosition = imageBaseAnchoredPos;
        }

        imageAnimRoutine = null;
    }

    private void NotifyLineShown()
    {
        if (currentDialogues == null || dialogueIndex < 0 || dialogueIndex >= currentDialogues.Length)
        {
            return;
        }

        DialogueLineShown?.Invoke(currentDialogues[dialogueIndex], dialogueIndex, currentDialogues.Length);
    }

    private void ShowCurrentDialogueLine()
    {
        if (dialogueText == null || currentDialogues == null || dialogueIndex < 0 || dialogueIndex >= currentDialogues.Length)
        {
            return;
        }

        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        string line = currentDialogues[dialogueIndex] ?? string.Empty;
        dialogueText.text = line;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();
        NotifyLineShown();

        if (!useTypewriterEffect || string.IsNullOrEmpty(line))
        {
            dialogueText.maxVisibleCharacters = int.MaxValue;
            isTypingLine = false;
            StopTypingSfxImmediate();

            if (dialogueIndex == currentDialogues.Length - 1 && pendingOptionAction != null)
            {
                ShowOptionWithLastLine();
            }

            return;
        }

        typewriterRoutine = StartCoroutine(TypeCurrentLineRoutine(line));
    }

    private IEnumerator TypeCurrentLineRoutine(string line)
    {
        isTypingLine = true;
        StartTypingSfx();
        int visibleCount = 0;
        int totalCharacters = line.Length;
        float interval = 1f / Mathf.Max(1f, charactersPerSecond);
        float elapsed = 0f;
        float pendingPause = 0f;

        while (visibleCount < totalCharacters)
        {
            if (pendingPause > 0f)
            {
                pendingPause -= Time.unscaledDeltaTime;
                yield return null;
                continue;
            }

            elapsed += Time.unscaledDeltaTime;

            while (elapsed >= interval && visibleCount < totalCharacters)
            {
                elapsed -= interval;
                visibleCount++;
                dialogueText.maxVisibleCharacters = visibleCount;

                float punctuationPause = GetPauseAfterCharacter(line, visibleCount - 1);
                if (punctuationPause > 0f)
                {
                    pendingPause = punctuationPause;
                    break;
                }
            }

            yield return null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTypingLine = false;
        typewriterRoutine = null;
        StopTypingSfxImmediate();

        if (dialogueIndex == currentDialogues.Length - 1 && pendingOptionAction != null)
        {
            ShowOptionWithLastLine();
        }
    }

    private void CompleteCurrentLineInstantly()
    {
        if (!isTypingLine) return;

        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        isTypingLine = false;
        StopTypingSfxImmediate();

        if (dialogueIndex == currentDialogues.Length - 1 && pendingOptionAction != null)
        {
            ShowOptionWithLastLine();
        }
    }

    private void StartTypingSfx()
    {
        bool isQuotedLine = IsQuotedLine(GetCurrentDialogueLine());
        AudioClip clip = ResolveTypingClip(isQuotedLine);

        if (clip == null) return;

        float volume = ResolveTypingVolume(isQuotedLine);

        AudioManager.EnsureInstance().PlayTypingLoop(clip, volume);
    }

    private void StopTypingSfxImmediate()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.StopTypingLoop();
    }

    private string GetCurrentDialogueLine()
    {
        if (currentDialogues == null || dialogueIndex < 0 || dialogueIndex >= currentDialogues.Length)
        {
            return string.Empty;
        }

        return currentDialogues[dialogueIndex] ?? string.Empty;
    }

    private bool IsQuotedLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return false;

        return line.Contains("\"") ||
               line.Contains("“") ||
               line.Contains("”") ||
               line.Contains("「") ||
               line.Contains("」") ||
               line.Contains("『") ||
               line.Contains("』");
    }

    private AudioClip ResolveTypingClip(bool isQuotedLine)
    {
        if (isQuotedLine)
        {
            if (activeDialogueAudioSettings != null && activeDialogueAudioSettings.quotedTypingSfx != null)
            {
                return activeDialogueAudioSettings.quotedTypingSfx;
            }

            if (defaultQuotedTypingSfx != null)
            {
                return defaultQuotedTypingSfx;
            }
        }

        if (activeDialogueAudioSettings != null && activeDialogueAudioSettings.typingSfx != null)
        {
            return activeDialogueAudioSettings.typingSfx;
        }

        return defaultTypingSfx;
    }

    private float ResolveTypingVolume(bool isQuotedLine)
    {
        if (isQuotedLine)
        {
            if (activeDialogueAudioSettings != null && activeDialogueAudioSettings.quotedTypingSfx != null)
            {
                return activeDialogueAudioSettings.quotedTypingSfxVolume;
            }

            if (defaultQuotedTypingSfx != null)
            {
                return defaultQuotedTypingSfxVolume;
            }
        }

        if (activeDialogueAudioSettings != null && activeDialogueAudioSettings.typingSfx != null)
        {
            return activeDialogueAudioSettings.typingSfxVolume;
        }

        return defaultTypingSfxVolume;
    }

    private float GetPauseAfterCharacter(string line, int charIndex)
    {
        if (string.IsNullOrEmpty(line) || charIndex < 0 || charIndex >= line.Length)
        {
            return 0f;
        }

        char current = line[charIndex];

        if (IsEllipsisDot(line, charIndex, out bool isEllipsisEnd))
        {
            return Mathf.Max(0f, isEllipsisEnd ? ellipsisEndPauseSeconds : ellipsisDotPauseSeconds);
        }

        if (current == ',' || current == '，')
        {
            return Mathf.Max(0f, commaPauseSeconds);
        }

        if (IsStandalonePeriod(line, charIndex))
        {
            return Mathf.Max(0f, periodPauseSeconds);
        }

        return 0f;
    }

    private bool IsStandalonePeriod(string line, int charIndex)
    {
        char current = line[charIndex];
        if (current != '.' && current != '。')
        {
            return false;
        }

        if (current == '.')
        {
            bool prevIsDot = charIndex > 0 && line[charIndex - 1] == '.';
            bool nextIsDot = charIndex < line.Length - 1 && line[charIndex + 1] == '.';
            if (prevIsDot || nextIsDot)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsEllipsisDot(string line, int charIndex, out bool isEllipsisEnd)
    {
        isEllipsisEnd = false;
        char current = line[charIndex];

        if (current == '…')
        {
            bool nextIsEllipsis = charIndex < line.Length - 1 && line[charIndex + 1] == '…';
            isEllipsisEnd = !nextIsEllipsis;
            return true;
        }

        if (current != '.')
        {
            return false;
        }

        bool prevIsDot = charIndex > 0 && line[charIndex - 1] == '.';
        bool nextIsDot = charIndex < line.Length - 1 && line[charIndex + 1] == '.';
        bool isPartOfEllipsis = prevIsDot || nextIsDot;

        if (!isPartOfEllipsis)
        {
            return false;
        }

        isEllipsisEnd = !nextIsDot;
        return true;
    }

}
