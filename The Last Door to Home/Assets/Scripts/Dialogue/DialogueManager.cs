using UnityEngine;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    [Header("对话框开关动画")]
    [SerializeField] private float panelAnimDuration = 0.18f;
    [SerializeField] private AnimationCurve panelAnimCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
    private RectTransform dialoguePanelRect;
    private Vector3 dialoguePanelBaseScale = Vector3.one;
    private Coroutine panelAnimRoutine;

    private RectTransform dialogueImageRect;
    private CanvasGroup dialogueImageCanvasGroup;
    private Coroutine imageAnimRoutine;
    private Vector2 imageBaseAnchoredPos;
    private bool hasImageBaseAnchoredPos;
    private bool isDialogueImageActive;

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

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("DialogueManager：未赋值Player物体！", this);
        }

        SetupDialogueImageUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (isDialogueActive && !isWaitingForOptionChoice && Input.GetKeyDown(KeyCode.Return))
        {
            if (Time.frameCount == dialogueStartFrame) return;
            AdvanceDialogue();
        }
    }

    public void ShowDialogue(string[] texts, PickableItem item = null, Action onLastLineOption = null)
    {
        if (isDialogueActive) return;
        if (texts == null || texts.Length == 0) return;

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

        PlayPanelOpenAnim();
        EnsureDialoguePanelInFrontOfImage();
        dialogueText.text = currentDialogues[dialogueIndex];

        LockPlayer(true);
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
            dialogueText.text = currentDialogues[dialogueIndex];

            if (dialogueIndex == currentDialogues.Length - 1 && pendingOptionAction != null)
            {
                ShowOptionWithLastLine();
            }
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

        pendingOptionAction = null;
    }

    public void CloseDialogueAfterOption()
    {
        if (isWaitingForOptionChoice)
        {
            EndDialogue();
        }
    }

    public void ContinueDialogueAfterOption(string[] texts, PickableItem item = null, Action onLastLineOption = null)
    {
        if (texts == null || texts.Length == 0)
        {
            CloseDialogueAfterOption();
            return;
        }

        if (!isDialogueActive)
        {
            ShowDialogue(texts, item, onLastLineOption);
            return;
        }

        pendingOptionAction = onLastLineOption;
        if (pendingOptionAction == null && item != null)
        {
            pendingOptionAction = () =>
            {
                item.ShowPickOptionsMenu();
            };
        }

        isWaitingForOptionChoice = false;
        currentDialogues = texts;
        dialogueIndex = 0;
        dialogueStartFrame = Time.frameCount;
        dialogueText.text = currentDialogues[dialogueIndex];
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
        playerRb.simulated = !lockIt;

        MonoBehaviour[] moveScripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in moveScripts)
        {
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
}
