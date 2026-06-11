using UnityEngine;
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
*/

public class ObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("多段对话（每一行按一次回车） / Multi-Line Dialogue (Press Return Per Line)")]
    [TextArea(3, 10)]
    public string[] dialogues;

    [Header("阅读插图（可选） / Reading Illustration (Optional)")]
    public bool showDialogueImage;
    public Sprite dialogueImageSprite;

    // Handles player interaction with this object.
    public void OnInteract()
    {
        if (DialogueManager.Instance == null) return;

        if (showDialogueImage && dialogueImageSprite != null)
        {
            DialogueManager.Instance.ShowDialogueImage(dialogueImageSprite);
        }

        // Dialogue-only interaction path.
        DialogueManager.Instance.ShowDialogue(dialogues);
    }
}
