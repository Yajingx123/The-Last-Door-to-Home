using UnityEngine;
using System.Collections.Generic;

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

    void Start()
    {
        // 只在本次游戏里判断是否拾取
        if (Inventory.HasCollected(itemUniqueID))
        {
            gameObject.SetActive(false);
        }
    }

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

    public void PickUp()
    {
        if (Inventory.HasCollected(itemUniqueID)) return;

        Inventory.AddItem(itemName, itemType, itemUniqueID);
        gameObject.SetActive(false);
    }

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
                    ShowDialogueIfAny(afterPickOptionDialogues);
                }
            },
            new OptionMenu.OptionEntry
            {
                text = leaveOptionText,
                canExecute = () => true,
                onExecute = () => ShowDialogueIfAny(afterLeaveOptionDialogues)
            }
        };

        OptionMenu.Instance.ShowOptions(entries, true, null);
    }

    private void ShowDialogueIfAny(string[] lines)
    {
        if (DialogueManager.Instance == null) return;
        if (lines == null || lines.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(lines);
    }
}
