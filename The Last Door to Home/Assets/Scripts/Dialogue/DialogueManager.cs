using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("拖入对话框面板")]
    public GameObject dialoguePanel;

    [Header("拖入 TextMeshPro - Text 组件")]
    public TextMeshProUGUI dialogueText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // 保护整个 DialogueSystem 根物体，而不是只保护自己
        transform.root.SetParent(null);
        DontDestroyOnLoad(transform.root.gameObject);
    }

    public void ShowDialogue(string content)
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        if (dialogueText != null)
            dialogueText.text = content;
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (dialogueText != null)
            dialogueText.text = "";
    }
}