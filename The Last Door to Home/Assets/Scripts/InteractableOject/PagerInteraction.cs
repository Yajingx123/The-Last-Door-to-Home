using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public string skipRecordingOptionText = "先不播放";
    [TextArea(2, 8)]
    public string[] afterPlayRecordingDialogues;
    [TextArea(2, 8)]
    public string[] afterSkipRecordingDialogues;

    private bool pickupInProgress;

    void Start()
    {
        if (Inventory.HasCollected(pagerUniqueID))
        {
            HidePagerInScene();
        }
    }

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
            DialogueManager.Instance.ShowDialogue(foundDialogues, null, ShowFirstChoiceMenu);
            return;
        }

        ShowFirstChoiceMenu();
    }

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
            },
            new OptionMenu.OptionEntry
            {
                text = skipRecordingOptionText,
                canExecute = () => true,
                onExecute = () => ContinueAndPickup(afterSkipRecordingDialogues)
            }
        };

        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    private void ContinueAndPickup(string[] lines)
    {
        if (DialogueManager.Instance == null)
        {
            FinalizePickup();
            return;
        }

        if (lines != null && lines.Length > 0)
        {
            DialogueManager.Instance.ContinueDialogueAfterOption(lines);
            StartCoroutine(FinalizePickupAfterDialogueClosed());
            return;
        }

        FinalizePickup();
        DialogueManager.Instance.CloseDialogueAfterOption();
    }

    private IEnumerator FinalizePickupAfterDialogueClosed()
    {
        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            yield return null;
        }

        FinalizePickup();
    }

    private void FinalizePickup()
    {
        if (!Inventory.HasCollected(pagerUniqueID))
        {
            Inventory.AddItem(pagerItemName, pagerItemType, pagerUniqueID);
        }

        HidePagerInScene();
        pickupInProgress = false;
    }

    private void HidePagerInScene()
    {
        GameObject hideTarget = pickupHideTarget != null ? pickupHideTarget : gameObject;
        if (hideTarget != null && hideTarget.activeSelf)
        {
            hideTarget.SetActive(false);
        }
    }

}
