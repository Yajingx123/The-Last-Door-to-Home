using UnityEngine;
using System.Collections;
/*
Purpose: Defines or executes story progression logic.
Attached GameObject: Story controller, trigger object, or ScriptableObject asset depending on the script type.
Main responsibilities: Tracks story flags/counters, evaluates story beats, and triggers dialogue or ending flows.
Inputs: Story event IDs, flags, counters, configured beats, interaction triggers, and scene state.
Outputs or effects: Updates story state, starts dialogue/events, blocks progression when needed, or triggers ending sequences.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each story event fires once or repeats as designed and survives save/load when required.
*/

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance;

    [Header("剧情条目（按优先级从高到低匹配） / Story Beats (High To Low Priority)")]
    public StoryBeat[] beats;

    [Header("剧情条目组（可选） / Story Beat Set (Optional)")]
    public StoryBeatSet[] beatSets;

    [Header("开场自动播放 / Auto Play On Start")]
    public bool autoPlayFirstBeatOnStart = false;
    public float autoPlayDelaySeconds = 1f;

    // 初始化剧情管理器单例，让其他物体可以找到它来触发剧情。
    // Initializes component references and singleton ownership before Start runs.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 对象销毁时清理单例引用，避免下个场景还指向旧对象。
    // Cleans up runtime references before the object is destroyed.
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 场景开始时，如果设置了自动播放，就准备播放第一段剧情。
    // Prepares runtime state after the scene has finished its initial setup.
    void Start()
    {
        if (autoPlayFirstBeatOnStart)
        {
            StartCoroutine(AutoPlayFirstBeatWhenReady());
        }
    }

    // 根据事件ID寻找合适的剧情条目，并尝试播放它。
    // Attempts the requested operation and reports whether it succeeded.
    public bool TryHandleEvent(string eventId)
    {
        StoryBeat best = FindBestBeat(eventId);
        return TryHandleBeat(best);
    }

    // 尝试执行一个剧情条目，包括检查条件、设置flag和播放对话。
    // Attempts the requested operation and reports whether it succeeded.
    public bool TryHandleBeat(StoryBeat beat)
    {
        if (beat == null) return false;
        if (!CanPlay(beat)) return false;

        ApplyFlags(beat.setFlagsOnPlay);
        string[] linesToPlay = ResolveDialogues(beat);
        bool played = PlayDialogues(beat, linesToPlay);
        if (played)
        {
            MarkBeatPlayed(beat);
        }
        IncreaseAttemptCounter(beat);
        return beat.blockDefaultAction;
    }

    // 等对话系统准备好后，再自动播放第一段剧情。
    // Handles the auto play first beat when ready step for this script.
    private IEnumerator AutoPlayFirstBeatWhenReady()
    {
        while (DialogueManager.Instance == null)
        {
            yield return null;
        }

        // Let DialogueManager finish its own Start prewarm flow first.
        yield return null;
        yield return null;

        float delay = Mathf.Max(0f, autoPlayDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        TryAutoPlayFirstBeat();
    }

    // 尝试播放列表里的第一段剧情，常用于场景开场对白。
    // Attempts the requested operation and reports whether it succeeded.
    private void TryAutoPlayFirstBeat()
    {
        if (beats == null || beats.Length == 0) return;
        if (beats[0] == null) return;
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive) return;

        StoryBeat first = beats[0];
        TryHandleBeat(first);
    }

    // 从所有剧情条目里找出匹配事件ID且优先级最高的一条。
    // Searches the scene hierarchy or data collection for the requested target.
    private StoryBeat FindBestBeat(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId)) return null;
        if (beats == null || beats.Length == 0) return null;

        StoryBeat best = null;

        for (int i = 0; i < beats.Length; i++)
        {
            StoryBeat beat = beats[i];
            if (beat == null) continue;
            if (!string.Equals(beat.eventId, eventId, System.StringComparison.Ordinal)) continue;
            if (!CanPlay(beat)) continue;

            if (best == null || beat.priority > best.priority)
            {
                best = beat;
            }
        }

        return best;
    }

    // 检查这段剧情现在能不能播放，比如flag、物品和是否已播放过。
    // Returns whether this script can can play.
    private bool CanPlay(StoryBeat beat)
    {
        if (IsOneShotEnabled(beat) && IsBeatAlreadyPlayed(beat)) return false;
        if (!HasAllFlags(beat.requiredFlags)) return false;
        if (HasAnyFlag(beat.blockedFlags)) return false;
        if (!HasAllItems(beat.requiredItemUniqueIDs)) return false;
        if (HasAnyItem(beat.blockedItemUniqueIDs)) return false;
        return true;
    }

    // 检查玩家是否拥有这段剧情要求的全部剧情flag。
    // Returns whether the required has all flags condition is met.
    private bool HasAllFlags(string[] flags)
    {
        if (flags == null) return true;

        for (int i = 0; i < flags.Length; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrWhiteSpace(flag)) continue;
            if (!StoryFlags.Has(flag)) return false;
        }

        return true;
    }

    // 检查玩家是否拥有任意一个会阻止这段剧情的flag。
    // Returns whether the required has any flag condition is met.
    private bool HasAnyFlag(string[] flags)
    {
        if (flags == null) return false;

        for (int i = 0; i < flags.Length; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrWhiteSpace(flag)) continue;
            if (StoryFlags.Has(flag)) return true;
        }

        return false;
    }

    // 检查玩家是否拥有这段剧情要求的全部道具。
    // Returns whether the required has all items condition is met.
    private bool HasAllItems(string[] itemIDs)
    {
        if (itemIDs == null) return true;

        for (int i = 0; i < itemIDs.Length; i++)
        {
            string itemID = itemIDs[i];
            if (string.IsNullOrWhiteSpace(itemID)) continue;
            if (!Inventory.HasCollected(itemID)) return false;
        }

        return true;
    }

    // 检查玩家是否拥有任意一个会阻止这段剧情的道具。
    // Returns whether the required has any item condition is met.
    private bool HasAnyItem(string[] itemIDs)
    {
        if (itemIDs == null) return false;

        for (int i = 0; i < itemIDs.Length; i++)
        {
            string itemID = itemIDs[i];
            if (string.IsNullOrWhiteSpace(itemID)) continue;
            if (Inventory.HasCollected(itemID)) return true;
        }

        return false;
    }

    // 播放剧情时设置新的剧情flag，记录剧情进度。
    // Applies the requested visual, audio, or gameplay state.
    private void ApplyFlags(string[] flags)
    {
        if (flags == null) return;

        for (int i = 0; i < flags.Length; i++)
        {
            string flag = flags[i];
            if (string.IsNullOrWhiteSpace(flag)) continue;
            StoryFlags.Set(flag);
        }
    }

    // 决定这次剧情要显示哪几句对话，支持多次尝试显示不同文本。
    // Chooses the dialogue lines that should be used for this story beat.
    private string[] ResolveDialogues(StoryBeat beat)
    {
        if (beat == null) return null;

        if (beat.useAttemptNarration && !string.IsNullOrWhiteSpace(beat.attemptCounterKey))
        {
            if (beat.attemptDialogues != null && beat.attemptDialogues.Length > 0)
            {
                int attemptIndex = StoryFlags.GetCounter(beat.attemptCounterKey);

                if (attemptIndex < 0) attemptIndex = 0;
                if (attemptIndex >= beat.attemptDialogues.Length)
                {
                    attemptIndex = beat.clampToLastAttemptLine ? beat.attemptDialogues.Length - 1 : 0;
                }

                string line = beat.attemptDialogues[attemptIndex];
                if (!string.IsNullOrWhiteSpace(line))
                {
                    return new string[] { line };
                }
            }
        }

        return beat.dialogues;
    }

    // 增加尝试次数计数，让下次互动可以显示不同提示。
    // Handles the increase attempt counter step for this script.
    private void IncreaseAttemptCounter(StoryBeat beat)
    {
        if (beat == null) return;
        if (!beat.useAttemptNarration) return;
        if (string.IsNullOrWhiteSpace(beat.attemptCounterKey)) return;

        StoryFlags.IncrementCounter(beat.attemptCounterKey);
    }

    // 把选好的对话交给DialogueManager播放，必要时也显示插图。
    // Plays the play dialogues sequence or audio feedback.
    private bool PlayDialogues(StoryBeat beat, string[] lines)
    {
        if (DialogueManager.Instance == null) return false;
        if (lines == null || lines.Length == 0) return false;

        if (DialogueManager.Instance.IsDialogueActive) return false;

        if (beat != null && beat.showDialogueImage && beat.dialogueImageSprite != null)
        {
            DialogueManager.Instance.ShowDialogueImage(beat.dialogueImageSprite);
        }

        DialogueManager.Instance.ShowDialogue(lines, null, null, null, beat != null ? beat.dialogueAudioSettings : null);
        return true;
    }

    // 检查这段一次性剧情之前是不是已经播放过。
    // Returns whether is beat already played is true for the current state.
    private bool IsBeatAlreadyPlayed(StoryBeat beat)
    {
        string key = GetBeatKey(beat);
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (StoryFlags.Has(key)) return true;
        return false;
    }

    // 把一次性剧情标记为已播放，防止之后重复触发。
    // Handles the mark beat played step for this script.
    private void MarkBeatPlayed(StoryBeat beat)
    {
        if (!IsOneShotEnabled(beat)) return;

        string key = GetBeatKey(beat);
        if (string.IsNullOrWhiteSpace(key)) return;

        StoryFlags.Set(key);
    }

    // 判断这段剧情是不是只允许播放一次。
    // Returns whether is one shot enabled is true for the current state.
    private bool IsOneShotEnabled(StoryBeat beat)
    {
        if (beat == null) return true;
        if (beat.useAttemptNarration) return false;
        return true;
    }

    // 生成这段剧情自己的唯一标记名，用来记录它是否播放过。
    // Returns the requested value or runtime object.
    private string GetBeatKey(StoryBeat beat)
    {
        if (beat == null) return string.Empty;
        string safeEventId = string.IsNullOrWhiteSpace(beat.eventId) ? "NoEvent" : beat.eventId.Trim();
        string safeName = string.IsNullOrWhiteSpace(beat.name) ? "UnnamedBeat" : beat.name.Trim();
        return $"StoryBeat:{safeEventId}:{safeName}";
    }
}
