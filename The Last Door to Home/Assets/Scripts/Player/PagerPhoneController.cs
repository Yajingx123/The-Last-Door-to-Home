using System.Collections.Generic;
using UnityEngine;

public class PagerPhoneController : MonoBehaviour
{
    [Header("触发设置")]
    public KeyCode openMenuKey = KeyCode.P;
    public string pagerUniqueID = "pager_01";

    [Header("电话菜单文案")]
    public string callAOptionText = "打电话给A";
    [TextArea(2, 8)]
    public string[] callADialogues;

    public string callBOptionText = "打电话给B";
    [TextArea(2, 8)]
    public string[] callBDialogues;

    public string callCOptionText = "打电话给C";
    [TextArea(2, 8)]
    public string[] callCDialogues;

    public string dontCallOptionText = "不打";
    [TextArea(2, 8)]
    public string[] dontCallDialogues;

    private bool callMenuOpen;

    void Update()
    {
        if (!Input.GetKeyDown(openMenuKey)) return;
        TryOpenCallMenu();
    }

    public void TryOpenCallMenu()
    {
        if (!Inventory.HasCollected(pagerUniqueID)) return;
        if (callMenuOpen) return;
        if (DialogueManager.Instance == null || OptionMenu.Instance == null) return;
        if (!DialogueManager.Instance.CanStartInteraction) return;

        callMenuOpen = true;

        var entries = new List<OptionMenu.OptionEntry>
        {
            new OptionMenu.OptionEntry
            {
                text = callAOptionText,
                canExecute = () => true,
                onExecute = () => ShowCallDialogue(callADialogues)
            },
            new OptionMenu.OptionEntry
            {
                text = callBOptionText,
                canExecute = () => true,
                onExecute = () => ShowCallDialogue(callBDialogues)
            },
            new OptionMenu.OptionEntry
            {
                text = callCOptionText,
                canExecute = () => true,
                onExecute = () => ShowCallDialogue(callCDialogues)
            },
            new OptionMenu.OptionEntry
            {
                text = dontCallOptionText,
                canExecute = () => true,
                onExecute = () => ShowCallDialogue(dontCallDialogues)
            }
        };

        OptionMenu.Instance.ShowOptions(entries, true, () => { callMenuOpen = false; });
    }

    private void ShowCallDialogue(string[] lines)
    {
        if (DialogueManager.Instance == null) return;
        if (lines == null || lines.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(lines);
    }
}
