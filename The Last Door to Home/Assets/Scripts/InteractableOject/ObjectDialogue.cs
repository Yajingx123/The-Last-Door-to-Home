using UnityEngine;

public class ObjectDialogue : MonoBehaviour
{
    [Header("多段对话（每一行按一次回车）")]
    [TextArea(3, 10)]
    public string[] dialogues;

    private PickableItem pickable;

    void Awake()
    {
        pickable = GetComponent<PickableItem>();
    }

    public void OnInteract()
    {
        // 直接把多段对话传给DialogueManager，可拾取物品同时传入
        DialogueManager.Instance.ShowDialogue(dialogues, pickable);
    }
}