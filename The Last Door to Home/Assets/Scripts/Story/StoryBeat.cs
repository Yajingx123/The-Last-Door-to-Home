using UnityEngine;

[CreateAssetMenu(fileName = "StoryBeat", menuName = "Story/Story Beat")]
public class StoryBeat : ScriptableObject
{
    [Header("匹配")]
    public string eventId;
    public int priority = 0;

    [Header("条件：必须已有这些Flag")]
    public string[] requiredFlags;

    [Header("条件：不能有这些Flag")]
    public string[] blockedFlags;

    [Header("条件：必须拥有这些物品ID")]
    public string[] requiredItemUniqueIDs;

    [Header("条件：不能拥有这些物品ID")]
    public string[] blockedItemUniqueIDs;

    [Header("执行")]
    [TextArea(2, 6)]
    public string[] dialogues;
    [Header("阅读插图（可选）")]
    public bool showDialogueImage = false;
    public Sprite dialogueImageSprite;
    public string[] setFlagsOnPlay;
    public bool blockDefaultAction = false;

    [Header("递进台词（可选）")]
    public bool useAttemptNarration = false;
    [Tooltip("开启后：该Beat不会被\"只播放一次\"拦截，可多次触发以推进attemptDialogues。")]
    public bool allowRepeatForAttemptNarration = false;
    public string attemptCounterKey = "";
    [TextArea(2, 6)]
    public string[] attemptDialogues;
    public bool clampToLastAttemptLine = true;
}
