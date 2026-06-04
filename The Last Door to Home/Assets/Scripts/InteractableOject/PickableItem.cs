using UnityEngine;
using System.Collections.Generic;

/*
Purpose: Manages p ic ka bl ei te m behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class PickableItem : MonoBehaviour, IInteractable
{
    [Header("物品唯一ID")]
    public string itemUniqueID = "flower_01";

    [Header("物品名称")]
    public string itemName = "Flower";

    [Header("物品类型")]
    public ItemType itemType;

    [Header("拾取前对白（最后一句会出现拾取选项）")]
    [TextArea(3, 10)]
    public string[] prePickDialogues;

    [Header("选项文案")]
    public string pickOptionText = "Pick Up";
    public string leaveOptionText = "Leave";

    [Header("选择Pick Up后的对白（可空）")]
    [TextArea(2, 6)]
    public string[] afterPickOptionDialogues;

    [Header("选择Leave后的对白（可空）")]
    [TextArea(2, 6)]
    public string[] afterLeaveOptionDialogues;

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        // 只在本次游戏里判断是否拾取
        if (Inventory.HasCollected(itemUniqueID))
        {
            gameObject.SetActive(false);
        }
    }

    // Executes this object interaction when the player activates it.
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

    // Performs the pickup logic for this collectible item.
    public void PickUp()
    {
        if (Inventory.HasCollected(itemUniqueID)) return;

        Inventory.AddItem(itemName, itemType, itemUniqueID);
        gameObject.SetActive(false);
    }

    // Shows the pickup option menu for this collectible item.
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

        // PickableItem 选项确认后不立刻关对话框，避免“先关再开”的闪断感。
        // 若有后续对白就直接衔接显示；没有后续对白时再主动关闭。
        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    // Shows follow-up dialogue after an option result or closes the flow when needed.
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
