using UnityEngine;

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("多段对话（每一行按一次回车）")]
    [TextArea(3, 10)]
    public string[] dialogues;

    public void OnInteract()
    {
        // 纯对白专用
        DialogueManager.Instance.ShowDialogue(dialogues);
    }
}
