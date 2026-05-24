using UnityEngine;

public class StoryDirector : MonoBehaviour
{
    public static StoryDirector Instance;

    [Header("剧情条目（按优先级从高到低匹配）")]
    public StoryBeat[] beats;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryHandleEvent(string eventId)
    {
        StoryBeat best = FindBestBeat(eventId);
        if (best == null) return false;

        ApplyFlags(best.setFlagsOnPlay);
        string[] linesToPlay = ResolveDialogues(best);
        PlayDialogues(linesToPlay);
        IncreaseAttemptCounter(best);
        return best.blockDefaultAction;
    }

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

    private bool CanPlay(StoryBeat beat)
    {
        if (!HasAllFlags(beat.requiredFlags)) return false;
        if (HasAnyFlag(beat.blockedFlags)) return false;
        if (!HasAllItems(beat.requiredItemUniqueIDs)) return false;
        if (HasAnyItem(beat.blockedItemUniqueIDs)) return false;
        return true;
    }

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

    private void IncreaseAttemptCounter(StoryBeat beat)
    {
        if (beat == null) return;
        if (!beat.useAttemptNarration) return;
        if (string.IsNullOrWhiteSpace(beat.attemptCounterKey)) return;

        StoryFlags.IncrementCounter(beat.attemptCounterKey);
    }

    private void PlayDialogues(string[] lines)
    {
        if (DialogueManager.Instance == null) return;
        if (lines == null || lines.Length == 0) return;

        if (DialogueManager.Instance.IsDialogueActive) return;
        DialogueManager.Instance.ShowDialogue(lines);
    }
}
