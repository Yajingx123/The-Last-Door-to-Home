using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
Purpose: Manages p ag er in te ra ct io n behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class PagerInteraction : MonoBehaviour, IInteractable
{
    [Header("Pager 物品信息")]
    public string pagerUniqueID = "pager_01";
    public string pagerItemName = "Pager";
    public ItemType pagerItemType = ItemType.Tool;

    [Header("场景中被拾取后要隐藏的物体（可选）")]
    public GameObject pickupHideTarget;

    [Header("首次发现：前置对白")]
    [TextArea(3, 10)]
    public string[] foundDialogues;

    [Header("首次发现：是否播放录音选项")]
    public string playRecordingOptionText = "播放录音";
    [TextArea(2, 8)]
    public string[] afterPlayRecordingDialogues;

    [Header("Pager 对话音频")]
    public DialogueAudioSettings dialogueAudioSettings;

    private bool pickupInProgress;

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        if (Inventory.HasCollected(pagerUniqueID))
        {
            HidePagerInScene();
        }
    }

    // Executes this object interaction when the player activates it.
    public void OnInteract()
    {
        if (DialogueManager.Instance == null || OptionMenu.Instance == null) return;
        if (pickupInProgress) return;

        if (Inventory.HasCollected(pagerUniqueID))
        {
            return;
        }

        if (foundDialogues != null && foundDialogues.Length > 0)
        {
            DialogueManager.Instance.ShowDialogue(foundDialogues, null, ShowFirstChoiceMenu, null, dialogueAudioSettings);
            return;
        }

        ShowFirstChoiceMenu();
    }

    // Shows the first pager choice menu for this interaction.
    private void ShowFirstChoiceMenu()
    {
        if (OptionMenu.Instance == null || DialogueManager.Instance == null) return;
        if (pickupInProgress) return;

        pickupInProgress = true;

        var entries = new List<OptionMenu.OptionEntry>
        {
            new OptionMenu.OptionEntry
            {
                text = playRecordingOptionText,
                canExecute = () => true,
                onExecute = () => ContinueAndPickup(afterPlayRecordingDialogues)
            }
        };

        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    // Continues the pager interaction flow and then grants the pickup.
    private void ContinueAndPickup(string[] lines)
    {
        if (DialogueManager.Instance == null)
        {
            FinalizePickup();
            return;
        }

        if (lines != null && lines.Length > 0)
        {
            DialogueManager.Instance.ContinueDialogueAfterOption(lines, null, null, null, dialogueAudioSettings);
            StartCoroutine(FinalizePickupAfterDialogueClosed());
            return;
        }

        FinalizePickup();
        DialogueManager.Instance.CloseDialogueAfterOption();
    }

    // Finalizes the pickup after the related dialogue has fully closed.
    private IEnumerator FinalizePickupAfterDialogueClosed()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        FinalizePickup();
    }

    // Finalizes the pickup flow and applies the collected-item state.
    private void FinalizePickup()
    {
        if (!Inventory.HasCollected(pagerUniqueID))
        {
            Inventory.AddItem(pagerItemName, pagerItemType, pagerUniqueID);
        }

        HidePagerInScene();
        pickupInProgress = false;
    }

    // Hides the pager object in the scene after it is collected.
    private void HidePagerInScene()
    {
        GameObject hideTarget = pickupHideTarget != null ? pickupHideTarget : gameObject;
        if (hideTarget != null && hideTarget.activeSelf)
        {
            hideTarget.SetActive(false);
        }
    }

}
