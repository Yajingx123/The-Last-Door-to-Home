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
        if (Instance == null)
        {
            Instance = this;
            // 删掉这里对 dialoguePanel 的 DontDestroyOnLoad 调用！
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