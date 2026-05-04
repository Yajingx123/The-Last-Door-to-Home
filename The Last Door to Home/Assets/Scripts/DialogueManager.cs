using UnityEngine;
using TMPro; // 支持 TextMeshPro

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("拖入你的对话框面板")]
    public GameObject dialoguePanel;

    [Header("拖入 TextMeshPro - Text 组件")]
    public TextMeshProUGUI dialogueText; // 这是现在 Unity 默认的！

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowDialogue(string content)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = content;
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
    }
}