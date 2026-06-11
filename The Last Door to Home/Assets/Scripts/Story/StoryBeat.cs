using UnityEngine;

[CreateAssetMenu(fileName = "StoryBeat", menuName = "Story/Story Beat")]
/*
Purpose: Defines or executes story progression logic.
Attached GameObject: Story controller, trigger object, or ScriptableObject asset depending on the script type.
Main responsibilities: Tracks story flags/counters, evaluates story beats, and triggers dialogue or ending flows.
Inputs: Story event IDs, flags, counters, configured beats, interaction triggers, and scene state.
Outputs or effects: Updates story state, starts dialogue/events, blocks progression when needed, or triggers ending sequences.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each story event fires once or repeats as designed and survives save/load when required.
*/

public class StoryBeat : ScriptableObject
{
    [Header("匹配 / Matching")]
    public string eventId;
    public int priority = 0;

    [Header("条件：必须已有这些Flag / Conditions: Required Flags")]
    public string[] requiredFlags;

    [Header("条件：不能有这些Flag / Conditions: Forbidden Flags")]
    public string[] blockedFlags;

    [Header("条件：必须拥有这些物品ID / Conditions: Required Item IDs")]
    public string[] requiredItemUniqueIDs;

    [Header("条件：不能拥有这些物品ID / Conditions: Forbidden Item IDs")]
    public string[] blockedItemUniqueIDs;

    [Header("执行 / Execution")]
    [TextArea(2, 6)]
    public string[] dialogues;
    public DialogueAudioSettings dialogueAudioSettings;
    [Header("阅读插图（可选） / Reading Illustration (Optional)")]
    public bool showDialogueImage = false;
    public Sprite dialogueImageSprite;
    public string[] setFlagsOnPlay;
    public bool blockDefaultAction = false;

    [Header("递进台词（可选） / Progressive Lines (Optional)")]
    public bool useAttemptNarration = false;
    [Tooltip("开启后：该Beat不会被\"只播放一次\"拦截，可多次触发以推进attemptDialogues。")]
    public bool allowRepeatForAttemptNarration = false;
    public string attemptCounterKey = "";
    [TextArea(2, 6)]
    public string[] attemptDialogues;
    public bool clampToLastAttemptLine = true;
}
