using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;
/*
Purpose: Displays dialogue text and controls dialogue-driven input locking.
Attached GameObject: Dialogue UI controller GameObject or runtime singleton.
Main responsibilities: Shows dialogue lines, advances text, plays dialogue audio, and reports whether player control is locked.
Inputs: Dialogue line arrays, optional callbacks, player input, and audio settings.
Outputs or effects: Updates dialogue UI, locks/unlocks gameplay input, invokes callbacks, and plays text/audio feedback.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify line advance, callbacks, audio, skipped text, and interaction blocking.
*/

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public static event Action DialogueStarted;
    public static event Action<string, int, int> DialogueLineShown;
    public static event Action DialogueEnded;

    [Header("UI / 界面")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("对话框开关动画 / Dialogue Panel Open-Close Animation")]
    [SerializeField] private float panelAnimDuration = 0.18f;
    [SerializeField] private AnimationCurve panelAnimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("打字机效果 / Typewriter Effect")]
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

    [Header("玩家控制（拖Player物体） / Player Control (Drag Player Object)")]
    public GameObject player;

    [Header("对话插图（可选） / Dialogue Illustration (Optional)")]
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
    public float DialogueImageAnimDuration => imageAnimDuration;
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

    // 初始化单例和UI引用，让对话系统一开始就准备好。
    // Initializes component references and singleton ownership before Start runs.
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

    // 场景启动后预热对话UI，避免第一次显示时文字或布局闪一下。
    // Prepares runtime state after the scene has finished its initial setup.
    IEnumerator Start()
    {
        yield return null;
        PrewarmDialogueUI();
    }

    // 对象销毁时清理单例和打字音效，避免留下旧状态。
    // Cleans up runtime references before the object is destroyed.
    void OnDestroy()
    {
        StopTypingSfxImmediate();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 找到玩家对象，方便对话时锁住玩家移动。
    // Resolves the best available value for the requested data.
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

    // 缓存玩家身上的移动脚本，之后锁控制时不用反复查找。
    // Caches references or values needed by cache player movement scripts.
    private void CachePlayerMovementScripts()
    {
        if (player == null) return;
        cachedMovementScripts = player.GetComponents<MonoBehaviour>();
    }

    // 提前刷新对话框UI，让第一次打开对话框更稳定。
    // Handles the prewarm dialogue ui step for this script.
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

    // 每帧检测回车键，用来跳过打字或进入下一句对话。
    // Reads per-frame input and updates frame-dependent runtime state.
    void Update()
    {
        if (EscapeMenuController.IsMenuOpen) return;
        if (InventoryMenuController.IsOpen) return;

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

    // 开始一段对话，显示第一句文字并锁住玩家控制。
    // Shows the show dialogue UI or dialogue flow.
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

    // 推进到下一句对话，如果已经是最后一句就结束或打开选项。
    // Handles the advance dialogue step for this script.
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

    // 在最后一句对话后显示选项，比如拾取或做选择。
    // Shows the show option with last line UI or dialogue flow.
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

    // 结束当前对话，关闭UI、恢复玩家控制，并执行结束回调。
    // Ends the end dialogue flow and restores the required state.
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

    // 选项处理完后关闭对话流程。
    // Closes the related UI or gameplay flow.
    public void CloseDialogueAfterOption()
    {
        if (isWaitingForOptionChoice)
        {
            EndDialogue();
        }
    }

    // 选完选项后继续播放新的对话内容。
    // Continues the continue dialogue after option flow from its current state.
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

    // 显示对话插图，让某句剧情可以配一张图片。
    // Shows the show dialogue image UI or dialogue flow.
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

    // 隐藏对话插图，通常在对话结束时调用。
    // Hides the UI immediately and resets transient state.
    public void HideDialogueImage()
    {
        if (dialogueImagePanel == null || dialogueImage == null) return;
        if (!isDialogueImageActive && !dialogueImagePanel.activeSelf) return;
        PlayDialogueImageAnim(false);
    }

    // 锁定或解锁玩家移动，防止对话时还能乱走。
    // Handles the lock player step for this script.
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

    // 播放对话框打开动画。
    // Plays the play panel open anim sequence or audio feedback.
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

    // 播放对话框关闭动画，并停止正在进行的打字效果。
    // Plays the play panel close anim sequence or audio feedback.
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

    // 用缩放Y轴的方式控制对话框打开或收起。
    // Handles the animate panel scale y step for this script.
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

    // 设置对话框的Y轴缩放值，用来配合开关动画。
    // Updates the requested value or component state.
    void SetPanelScaleY(float scaleY01)
    {
        if (dialoguePanelRect == null) return;

        dialoguePanelRect.localScale = new Vector3(
            dialoguePanelBaseScale.x,
            dialoguePanelBaseScale.y * scaleY01,
            dialoguePanelBaseScale.z
        );
    }

    // 确保对话框显示在插图前面，不被图片挡住。
    // Ensures the required ensure dialogue panel in front of image objects or state exist.
    void EnsureDialoguePanelInFrontOfImage()
    {
        if (dialoguePanel == null || dialogueImagePanel == null) return;
        if (dialoguePanel.transform.parent != dialogueImagePanel.transform.parent) return;
        dialoguePanel.transform.SetAsLastSibling();
    }

    // 初始化对话插图UI，找到图片组件并准备淡入淡出效果。
    // Updates the requested value or component state.
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

    // 播放对话插图的显示或隐藏动画。
    // Plays the play dialogue image anim sequence or audio feedback.
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

    // 控制插图淡入淡出和上下移动，让图片出现得更自然。
    // Handles the animate dialogue image step for this script.
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

    // 通知其他系统当前显示了哪一句对话。如果要做某一句话时出现图片或其他效果，通过这个函数通知出去。
    // Handles the notify line shown step for this script.
    // If an image or other effect appears when making a certain sentence, notify it through this function.
    private void NotifyLineShown()
    {
        if (currentDialogues == null || dialogueIndex < 0 || dialogueIndex >= currentDialogues.Length)
        {
            return;
        }

        DialogueLineShown?.Invoke(currentDialogues[dialogueIndex], dialogueIndex, currentDialogues.Length);
    }

    // 显示当前这句对话，并决定是否使用打字机效果。
    // Shows the show current dialogue line UI or dialogue flow.
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

    // 逐字显示当前句子，制造打字机效果和停顿节奏。
    // Handles the type current line routine step for this script.
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

    // 立刻显示完整当前句子，用来跳过打字过程。
    // Completes the complete current line instantly step immediately.
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

    // 开始播放打字音效，根据普通台词或引号台词选择声音。
    // Starts the start typing sfx sequence or runtime effect.
    private void StartTypingSfx()
    {
        bool isQuotedLine = IsQuotedLine(GetCurrentDialogueLine());
        AudioClip clip = ResolveTypingClip(isQuotedLine);

        if (clip == null) return;

        float volume = ResolveTypingVolume(isQuotedLine);

        AudioManager.EnsureInstance().PlayTypingLoop(clip, volume);
    }

    // 立刻停止打字音效，避免对话结束后声音继续播放。
    // Stops the stop typing sfx immediate sequence or runtime effect.
    private void StopTypingSfxImmediate()
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.StopTypingLoop();
    }

    // 获取当前正在显示的这一句对话文本,给 StartTypingSfx() 用。因为打字音效需要判断当前这句有没有引号
    // Returns the requested value or runtime object.
    // For StartTypingSfx(). Because the typing sound effect needs to determine whether the current sentence has quotation marks
    private string GetCurrentDialogueLine()
    {
        if (currentDialogues == null || dialogueIndex < 0 || dialogueIndex >= currentDialogues.Length)
        {
            return string.Empty;
        }

        return currentDialogues[dialogueIndex] ?? string.Empty;
    }

    // 判断这一句是不是带引号的台词，用来切换特殊打字音效。
    // Returns whether is quoted line is true for the current state.
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

    // 根据当前台词类型选择要播放的打字音效。
    // Resolves the best available value for the requested data.
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

    // 根据当前台词类型选择打字音效音量。
    // Resolves the best available value for the requested data.
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

    // 根据标点符号决定打字时要不要短暂停顿。
    // Returns the requested value or runtime object.
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

    // 判断当前点号是不是普通句号，而不是省略号的一部分。
    // Returns whether is standalone period is true for the current state.
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

    // 判断当前字符是不是省略号，并判断是不是省略号结尾。
    // Returns whether is ellipsis dot is true for the current state.
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
