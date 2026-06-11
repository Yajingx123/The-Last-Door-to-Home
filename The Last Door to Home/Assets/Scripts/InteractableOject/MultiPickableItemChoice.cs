using UnityEngine;
using System;
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

public class MultiPickableItemChoice : MonoBehaviour, IInteractable
{
    [Serializable]
public class PickableChoice
    {
        [Tooltip("拖入带有 PickableItem 脚本的 GameObject。")]
        public GameObject itemObject;

        [Tooltip("可空。为空时会使用 PickableItem.itemName。")]
        public string optionTextOverride = "";

        public PickableItem Item
        {
            get
            {
                return itemObject != null ? itemObject.GetComponent<PickableItem>() : null;
            }
        }

        // Returns the requested value or runtime object.
        public string GetOptionText()
        {
            PickableItem item = Item;
            if (!string.IsNullOrWhiteSpace(optionTextOverride)) return optionTextOverride;
            if (item != null && !string.IsNullOrWhiteSpace(item.itemName)) return item.itemName;
            return itemObject != null ? itemObject.name : "Item";
        }
    }

    [Header("前置对白（最后一句后弹出选择菜单，可空） / Intro Dialogue (Choice Menu After Last Line, Optional)")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("所有可拿物品都已拿完后的对白（可空） / Dialogue After All Items Are Collected (Optional)")]
    [TextArea(2, 6)]
    public string[] allCollectedDialogues;

    [Header("可选择拾取的物品 / Selectable Pickup Items")]
    public PickableChoice[] choices;

    [Header("取消选项 / Cancel Option")]
    public string cancelOptionText = "Cancel";

    // Handles player interaction with this object.
    public void OnInteract()
    {
        if (OptionMenu.Instance == null) return;

        if (!HasAvailablePickableItem())
        {
            ShowAllCollectedDialogue();
            return;
        }

        List<OptionMenu.OptionEntry> entries = BuildEntries();
        if (entries.Count == 0) return;

        if (preDialogues != null && preDialogues.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(preDialogues, null, () =>
            {
                OptionMenu.Instance.ShowOptions(entries, true, null);
            });
            return;
        }

        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    // Builds data or UI objects required by this system.
    private List<OptionMenu.OptionEntry> BuildEntries()
    {
        var entries = new List<OptionMenu.OptionEntry>();

        if (choices != null)
        {
            for (int i = 0; i < choices.Length; i++)
            {
                PickableChoice choice = choices[i];
                if (choice == null) continue;

                PickableItem item = choice.Item;
                if (item == null) continue;
                if (Inventory.HasCollected(item.itemUniqueID)) continue;

                string optionText = choice.GetOptionText();
                entries.Add(new OptionMenu.OptionEntry
                {
                    text = optionText,
                    canExecute = () => true,
                    onExecute = () => StartPickableItemFlow(item)
                });
            }
        }

        entries.Add(new OptionMenu.OptionEntry
        {
            text = cancelOptionText,
            canExecute = () => true,
            onExecute = CloseOrUnlockAfterCancel
        });

        return entries;
    }

    // Returns whether the required has available pickable item condition is met.
    private bool HasAvailablePickableItem()
    {
        if (choices == null) return false;

        for (int i = 0; i < choices.Length; i++)
        {
            PickableChoice choice = choices[i];
            if (choice == null) continue;

            PickableItem item = choice.Item;
            if (item == null) continue;
            if (!Inventory.HasCollected(item.itemUniqueID)) return true;
        }

        return false;
    }

    // Shows the show all collected dialogue UI or dialogue flow.
    private void ShowAllCollectedDialogue()
    {
        if (DialogueManager.Instance == null) return;

        if (allCollectedDialogues != null && allCollectedDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(allCollectedDialogues);
        }
    }

    // Starts the start pickable item flow sequence or runtime effect.
    private void StartPickableItemFlow(PickableItem item)
    {
        if (item == null) return;
        item.OnInteract();
    }

    // Closes the related UI or gameplay flow.
    private void CloseOrUnlockAfterCancel()
    {
        if (DialogueManager.Instance == null) return;

        if (DialogueManager.Instance.IsDialogueActive)
        {
            DialogueManager.Instance.CloseDialogueAfterOption();
            return;
        }

        DialogueManager.Instance.LockPlayer(false);
    }
}
