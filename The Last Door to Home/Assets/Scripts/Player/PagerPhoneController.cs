using System.Collections.Generic;
using UnityEngine;
/*
Purpose: Opens the pager phone menu and plays the selected call dialogue.
Attached GameObject: Player GameObject or a player input controller object.
Main responsibilities: Checks pager ownership, opens option entries, tracks menu state, and sends selected dialogue lines to DialogueManager.
Inputs: Open-menu key, pager item ID, option text, dialogue arrays, inventory state, and menu/dialogue availability.
Outputs or effects: Displays OptionMenu entries and starts the chosen dialogue sequence.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify the menu only opens after collecting the pager and closes correctly after each option.
*/

public class PagerPhoneController : MonoBehaviour
{
    [Header("触发设置 / Trigger Settings")]
    public KeyCode openMenuKey = KeyCode.P;
    public string pagerUniqueID = "pager_01";

    [Header("电话菜单文案 / Phone Menu Text")]
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

    [Header("Pager 对话音频 / Pager Dialogue Audio")]
    public DialogueAudioSettings dialogueAudioSettings;

    private bool callMenuOpen;

    // Reads per-frame input and updates frame-dependent runtime state.
    void Update()
    {
        if (EscapeMenuController.IsMenuOpen) return;
        if (InventoryMenuController.IsOpen) return;

        if (!Input.GetKeyDown(openMenuKey)) return;
        TryOpenCallMenu();
    }

    // Attempts the requested operation and reports whether it succeeded.
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

    // Shows the show call dialogue UI or dialogue flow.
    private void ShowCallDialogue(string[] lines)
    {
        if (DialogueManager.Instance == null) return;
        if (lines == null || lines.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(lines, null, null, null, dialogueAudioSettings);
    }
}
