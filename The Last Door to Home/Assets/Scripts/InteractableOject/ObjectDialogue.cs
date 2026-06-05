using UnityEngine;

/*
Purpose: Manages o bj ec td ia lo gu e behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("多段对话（每一行按一次回车）")]
    [TextArea(3, 10)]
    public string[] dialogues;

    [Header("阅读插图（可选）")]
    public bool showDialogueImage;
    public Sprite dialogueImageSprite;

    // Executes this object interaction when the player activates it.
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
