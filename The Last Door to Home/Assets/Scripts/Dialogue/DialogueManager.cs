using UnityEngine;
using TMPro;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialoguePanel != null) dialoguePanel.SetActive(false);

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
        dialoguePanel.SetActive(true);
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
        dialoguePanel.SetActive(false);
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
}
