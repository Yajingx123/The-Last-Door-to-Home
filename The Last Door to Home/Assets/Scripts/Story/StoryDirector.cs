using UnityEngine;
using System.Collections;

/*
Purpose: Selects and plays story beats based on flags, inventory state, and event triggers.
Attached GameObject: Central story manager GameObject.
Main responsibilities: Evaluate progression conditions, trigger story responses, and update narrative state.
Inputs: Event IDs, story flags, inventory state, and serialized story data.
Outputs or effects: Advances narrative state, launches dialogue, or gates gameplay actions.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class StoryDirector : MonoBehaviour
{
    private const string BeatPlayedPrefPrefix = "StoryBeatPlayed:";

    public static StoryDirector Instance;

    [Header("剧情条目（按优先级从高到低匹配）")]
    public StoryBeat[] beats;

    [Header("剧情条目组（可选）")]
    public StoryBeatSet[] beatSets;

    [Header("一次性记忆范围")]
    [Tooltip("关闭=仅当前Play运行期有效（跨场景有效，停止Play后重置）；开启=写入PlayerPrefs，跨多次运行也记住。")]
    public bool persistBeatPlayedToPlayerPrefs = false;
    
    [Header("开场自动播放")]
    public bool autoPlayFirstBeatOnStart = false;
    public float autoPlayDelaySeconds = 1f;

    // Initializes cached references and one-time component state before gameplay begins.
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Cleans up cached state and running effects during teardown.
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // Prepares runtime state after the scene finishes its initial setup.
    void Start()
    {
        if (autoPlayFirstBeatOnStart)
        {
            StartCoroutine(AutoPlayFirstBeatWhenReady());
        }
    }

    // Finds and attempts to play the best story beat for the incoming event ID.
    public bool TryHandleEvent(string eventId)
    {
        StoryBeat best = FindBestBeat(eventId);
        return TryHandleBeat(best);
    }

    // Validates a specific story beat and plays it when allowed.
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

    // Waits for the dialogue system and then auto-plays the opening story beat.
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

    // Attempts to trigger the first configured beat when startup conditions allow it.
    private void TryAutoPlayFirstBeat()
    {
        if (beats == null || beats.Length == 0) return;
        if (beats[0] == null) return;
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive) return;

        StoryBeat first = beats[0];
        TryHandleBeat(first);
    }

    // Finds the highest-priority playable beat for the supplied event ID.
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

    // Checks whether the supplied beat passes all playback requirements.
    private bool CanPlay(StoryBeat beat)
    {
        if (IsOneShotEnabled(beat) && IsBeatAlreadyPlayed(beat)) return false;
        if (!HasAllFlags(beat.requiredFlags)) return false;
        if (HasAnyFlag(beat.blockedFlags)) return false;
        if (!HasAllItems(beat.requiredItemUniqueIDs)) return false;
        if (HasAnyItem(beat.blockedItemUniqueIDs)) return false;
        return true;
    }

    // Verifies that every required story flag is currently set.
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

    // Checks whether any blocking story flag is currently set.
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

    // Verifies that all required inventory items are currently collected.
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

    // Checks whether any blocked inventory item is currently collected.
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

    // Applies the story flags granted when this beat plays.
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

    // Advances the attempt counter used for repeat narration variants.
    private void IncreaseAttemptCounter(StoryBeat beat)
    {
        if (beat == null) return;
        if (!beat.useAttemptNarration) return;
        if (string.IsNullOrWhiteSpace(beat.attemptCounterKey)) return;

        StoryFlags.IncrementCounter(beat.attemptCounterKey);
    }

    // Asks the dialogue system to play the supplied story dialogue lines.
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

    // Checks runtime and saved data to see whether this beat already fired.
    private bool IsBeatAlreadyPlayed(StoryBeat beat)
    {
        string key = GetBeatKey(beat);
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (StoryFlags.Has(key)) return true;
        if (!persistBeatPlayedToPlayerPrefs) return false;
        return PlayerPrefs.GetInt(BeatPlayedPrefPrefix + key, 0) == 1;
    }

    // Records this beat as played so one-shot content does not repeat.
    private void MarkBeatPlayed(StoryBeat beat)
    {
        if (!IsOneShotEnabled(beat)) return;

        string key = GetBeatKey(beat);
        if (string.IsNullOrWhiteSpace(key)) return;

        StoryFlags.Set(key);
        if (persistBeatPlayedToPlayerPrefs)
        {
            PlayerPrefs.SetInt(BeatPlayedPrefPrefix + key, 1);
            PlayerPrefs.Save();
        }
    }

    // Determines whether the supplied beat should only play once.
    private bool IsOneShotEnabled(StoryBeat beat)
    {
        if (beat == null) return true;
        if (beat.useAttemptNarration) return false;
        return true;
    }

    // Builds a stable key used to track story beat completion.
    private string GetBeatKey(StoryBeat beat)
    {
        if (beat == null) return string.Empty;
        string safeEventId = string.IsNullOrWhiteSpace(beat.eventId) ? "NoEvent" : beat.eventId.Trim();
        string safeName = string.IsNullOrWhiteSpace(beat.name) ? "UnnamedBeat" : beat.name.Trim();
        return $"StoryBeat:{safeEventId}:{safeName}";
    }
}
