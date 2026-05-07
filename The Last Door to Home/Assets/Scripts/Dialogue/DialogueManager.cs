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

    // 到最后一句时触发的可选回调（如弹出OptionMenu）
    private Action pendingOptionAction;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 把根物体（DialogueSystem）设为不销毁，而非仅当前子物体
            DontDestroyOnLoad(transform.root.gameObject); 
            // 额外：确保对话面板初始是隐藏的
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
        else
        {
            // 如果重复生成，销毁整个多余的DialogueSystem
            Destroy(transform.root.gameObject);
        }

        // 补全player的空引用保护
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("DialogueManager：未赋值Player物体！", this);
        }
    }

    void Update()
    {
        if (isDialogueActive && !isWaitingForOptionChoice && Input.GetKeyDown(KeyCode.Return))
        {
            // 避免“开启对话”和“按回车翻页”发生在同一帧导致首句被跳过
            if (Time.frameCount == dialogueStartFrame) return;
            AdvanceDialogue();
        }
    }

    // 显示多段对话
    public void ShowDialogue(string[] texts, PickableItem item = null, Action onLastLineOption = null)
    {
        if (isDialogueActive) return;
        if (texts == null || texts.Length == 0) return;

        pendingOptionAction = onLastLineOption;
        if (pendingOptionAction == null && item != null)
        {
            pendingOptionAction = () =>
            {
                if (OptionMenu.Instance != null)
                {
                    OptionMenu.Instance.ShowPickOptions(item);
                }
                else
                {
                    LockPlayer(false);
                }
            };
        }

        currentDialogues = texts;
        dialogueIndex = 0;
        isDialogueActive = true;
        dialogueStartFrame = Time.frameCount;
        dialoguePanel.SetActive(true);
        dialogueText.text = currentDialogues[dialogueIndex];

        // 完全锁死玩家
        LockPlayer(true);
    }

    void AdvanceDialogue()
    {
        // 先判断是不是已经是最后一句了
        if (dialogueIndex >= currentDialogues.Length - 1)
        {
            // 如果有待弹出的选项，则最后一句和选项同时出现
            if (pendingOptionAction != null)
            {
                ShowOptionWithLastLine();
                return;
            }

            EndDialogue(); // 最后一句说完，直接结束
        }
        else
        {
            dialogueIndex++;
            dialogueText.text = currentDialogues[dialogueIndex];

            // 切到最后一句时，立即弹出选项（不等待再次回车）
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

    // 供OptionMenu确认选择后调用：关闭对白并结束流程
    public void CloseDialogueAfterOption()
    {
        if (isWaitingForOptionChoice)
        {
            EndDialogue();
        }
    }

    // 锁/解锁玩家（禁用刚体 + 控制脚本）
    public void LockPlayer(bool lockIt)
    {
        if (playerRb == null || player == null) return;

        // 物理层面停住
        playerRb.velocity = Vector2.zero;
        playerRb.simulated = !lockIt;

        // 禁用所有移动脚本（更彻底）
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
