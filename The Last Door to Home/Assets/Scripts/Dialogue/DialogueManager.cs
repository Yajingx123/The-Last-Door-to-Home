using UnityEngine;
using TMPro;
using System;
using System.Collections;

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

        pendingOptionAction = null;
    }

    public void CloseDialogueAfterOption()
    {
        if (isWaitingForOptionChoice)
        {
            EndDialogue();
        }
    }

    public void ContinueDialogueAfterOption(string[] texts)
    {
        if (texts == null || texts.Length == 0)
        {
            CloseDialogueAfterOption();
            return;
        }

        if (!isDialogueActive) 
        {
            ShowDialogue(texts);
            return;
        }

        isWaitingForOptionChoice = false;
        pendingOptionAction = null;
        currentDialogues = texts;
        dialogueIndex = 0;
        dialogueStartFrame = Time.frameCount;
        dialogueText.text = currentDialogues[dialogueIndex];
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
}
