using System.Collections;
using System.Collections.Generic;
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

public class PagerInteraction : MonoBehaviour, IInteractable
{
    [Header("Pager 物品信息 / Pager Item Info")]
    public string pagerUniqueID = "pager_01";
    public string pagerItemName = "Pager";
    public ItemType pagerItemType = ItemType.Tool;

    [Header("场景中被拾取后要隐藏的物体（可选） / Scene Object To Hide After Pickup (Optional)")]
    public GameObject pickupHideTarget;

    [Header("首次发现：前置对白 / First Discovery: Intro Dialogue")]
    [TextArea(3, 10)]
    public string[] foundDialogues;

    [Header("首次发现：是否播放录音选项 / First Discovery: Play Recording Option")]
    public string playRecordingOptionText = "播放录音";
    [TextArea(2, 8)]
    public string[] afterPlayRecordingDialogues;

    [Header("Pager 对话音频 / Pager Dialogue Audio")]
    public DialogueAudioSettings dialogueAudioSettings;

    private bool pickupInProgress;

    // Prepares runtime state after the scene has finished its initial setup.
    void Start()
    {
        if (Inventory.HasCollected(pagerUniqueID))
        {
            HidePagerInScene();
        }
    }

    // Handles player interaction with this object.
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

    // Shows the show first choice menu UI or dialogue flow.
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

    // Continues the continue and pickup flow from its current state.
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

    // Handles the finalize pickup after dialogue closed step for this script.
    private IEnumerator FinalizePickupAfterDialogueClosed()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        FinalizePickup();
    }

    // Handles the finalize pickup step for this script.
    private void FinalizePickup()
    {
        if (!Inventory.HasCollected(pagerUniqueID))
        {
            Inventory.AddItem(pagerItemName, pagerItemType, pagerUniqueID);
        }

        HidePagerInScene();
        pickupInProgress = false;
    }

    // Hides the UI immediately and resets transient state.
    private void HidePagerInScene()
    {
        GameObject hideTarget = pickupHideTarget != null ? pickupHideTarget : gameObject;
        if (hideTarget != null && hideTarget.activeSelf)
        {
            hideTarget.SetActive(false);
        }
    }

}
