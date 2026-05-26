using UnityEngine;

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("多段对话（每一行按一次回车）")]
    [TextArea(3, 10)]
    public string[] dialogues;

    [Header("阅读插图（可选）")]
    public bool showDialogueImage;
    public Sprite dialogueImageSprite;

    public void OnInteract()
    {
        if (DialogueManager.Instance == null) return;

        if (showDialogueImage && dialogueImageSprite != null)
        {
            DialogueManager.Instance.ShowDialogueImage(dialogueImageSprite);
        }

        // 纯对白专用
        DialogueManager.Instance.ShowDialogue(dialogues);
    }
}
