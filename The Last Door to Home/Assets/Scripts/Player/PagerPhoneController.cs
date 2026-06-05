using System.Collections.Generic;
using UnityEngine;

/*
Purpose: Manages p ag er ph on ec on tr ol le r behavior for this part of the game.
Attached GameObject: Player GameObject or a player-specific child object.
Main responsibilities: Read player-facing state, coordinate related components, and apply movement or presentation updates.
Inputs: Inspector references, Unity input, and state from linked gameplay managers.
Outputs or effects: Moves the player or camera, updates animations, and changes immediate gameplay feel.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

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

    [Header("Pager 对话音频")]
    public DialogueAudioSettings dialogueAudioSettings;

    private bool callMenuOpen;

    // Processes per-frame input and keeps this behaviour responsive during gameplay.
    void Update()
    {
        if (!Input.GetKeyDown(openMenuKey)) return;
        TryOpenCallMenu();
    }

    // Attempts to open the pager call menu when the device is available.
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

    // Shows the pager call dialogue sequence for the selected contact.
    private void ShowCallDialogue(string[] lines)
    {
        if (DialogueManager.Instance == null) return;
        if (lines == null || lines.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(lines, null, null, null, dialogueAudioSettings);
    }
}
