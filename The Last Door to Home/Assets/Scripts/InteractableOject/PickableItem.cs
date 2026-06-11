using UnityEngine;
using System.Collections.Generic;
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
*/

public class PickableItem : MonoBehaviour, IInteractable
{
    [Header("物品唯一ID / Item Unique ID")]
    public string itemUniqueID = "flower_01";

    [Header("物品名称 / Item Name")]
    public string itemName = "Flower";

    [Header("物品类型 / Item Type")]
    public ItemType itemType;

    [Header("物品详情 / Item Details")]
    [TextArea(2, 6)]
    public string itemDescription = "No description yet.";
    public Sprite itemIcon;
    [Tooltip("可选：若图标放在 Resources 目录下，填入不带扩展名的路径，读档后也能恢复图标。")]
    public string itemIconResourcePath = "";

    [Header("拾取前对白（最后一句会出现拾取选项） / Dialogue Before Pickup (Pickup Option On Last Line)")]
    [TextArea(3, 10)]
    public string[] prePickDialogues;

    [Header("选项文案 / Option Text")]
    public string pickOptionText = "Pick Up";
    public string leaveOptionText = "Leave";

    [Header("选择Pick Up后的对白（可空） / Dialogue After Pick Up (Optional)")]
    [TextArea(2, 6)]
    public string[] afterPickOptionDialogues;

    [Header("选择Leave后的对白（可空） / Dialogue After Leave (Optional)")]
    [TextArea(2, 6)]
    public string[] afterLeaveOptionDialogues;

    // Prepares runtime state after the scene has finished its initial setup.
    void Start()
    {
        // Check collection state only for the current game session.
        if (Inventory.HasCollected(itemUniqueID))
        {
            gameObject.SetActive(false);
        }
    }

    // Handles player interaction with this object.
    public void OnInteract()
    {
        if (DialogueManager.Instance == null) return;

        if (Inventory.HasCollected(itemUniqueID))
        {
            return;
        }

        if (prePickDialogues != null && prePickDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(prePickDialogues, null, ShowPickOptionsMenu);
            return;
        }

        ShowPickOptionsMenu();
    }

    // Picks up the configured item and updates inventory state.
    public void PickUp()
    {
        if (Inventory.HasCollected(itemUniqueID)) return;

        Inventory.AddItem(itemName, itemType, itemUniqueID, itemDescription, itemIconResourcePath, itemIcon);
        gameObject.SetActive(false);
    }

    // Shows the show pick options menu UI or dialogue flow.
    public void ShowPickOptionsMenu()
    {
        if (OptionMenu.Instance == null || DialogueManager.Instance == null) return;

        var entries = new List<OptionMenu.OptionEntry>
        {
            new OptionMenu.OptionEntry
            {
                text = pickOptionText,
                canExecute = () => true,
                onExecute = () =>
                {
                    PickUp();
                    ShowDialogueAfterOptionOrClose(afterPickOptionDialogues);
                }
            },
            new OptionMenu.OptionEntry
            {
                text = leaveOptionText,
                canExecute = () => true,
                onExecute = () => ShowDialogueAfterOptionOrClose(afterLeaveOptionDialogues)
            }
        };

        // Keep the dialogue box open after confirming pickup to avoid close/reopen flicker.
        // Continue into follow-up dialogue when present; otherwise close it explicitly.
        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    // Shows the show dialogue after option or close UI or dialogue flow.
    private void ShowDialogueAfterOptionOrClose(string[] lines)
    {
        if (DialogueManager.Instance == null) return;

        if (lines != null && lines.Length > 0)
        {
            DialogueManager.Instance.ContinueDialogueAfterOption(lines);
            return;
        }

        DialogueManager.Instance.CloseDialogueAfterOption();
    }
}
