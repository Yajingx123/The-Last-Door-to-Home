using UnityEngine;
using System;
using System.Collections.Generic;

/*
Purpose: Lets one interactable object route the player to one of several PickableItem flows.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Show an optional intro dialogue, then a menu of pickable items plus cancel.
Inputs: Player interaction calls, inspector-configured PickableItem GameObjects, and dialogue UI state.
Outputs or effects: Starts the selected PickableItem interaction or closes/cancels the menu.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector item references, option text, cancel behavior, and collected-item filtering.
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

        public string GetOptionText()
        {
            PickableItem item = Item;
            if (!string.IsNullOrWhiteSpace(optionTextOverride)) return optionTextOverride;
            if (item != null && !string.IsNullOrWhiteSpace(item.itemName)) return item.itemName;
            return itemObject != null ? itemObject.name : "Item";
        }
    }

    [Header("前置对白（最后一句后弹出选择菜单，可空）")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("所有可拿物品都已拿完后的对白（可空）")]
    [TextArea(2, 6)]
    public string[] allCollectedDialogues;

    [Header("可选择拾取的物品")]
    public PickableChoice[] choices;

    [Header("取消选项")]
    public string cancelOptionText = "Cancel";

    // Executes this object interaction when the player activates it.
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

    // Builds one menu entry for each available item, followed by cancel.
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

    // Checks whether at least one configured PickableItem can still be picked.
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

    // Shows the fallback dialogue after every configured item has been collected.
    private void ShowAllCollectedDialogue()
    {
        if (DialogueManager.Instance == null) return;

        if (allCollectedDialogues != null && allCollectedDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(allCollectedDialogues);
        }
    }

    // Hands control to the selected PickableItem so it can show its own pick/leave choice.
    private void StartPickableItemFlow(PickableItem item)
    {
        if (item == null) return;
        item.OnInteract();
    }

    // Cancels the chooser and restores player control when there is no dialogue to close.
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
