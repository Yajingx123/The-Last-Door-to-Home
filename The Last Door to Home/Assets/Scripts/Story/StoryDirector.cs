using UnityEngine;
using System.Collections;

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

    void Start()
    {
        if (autoPlayFirstBeatOnStart)
        {
            StartCoroutine(AutoPlayFirstBeatWhenReady());
        }
    }

    public bool TryHandleEvent(string eventId)
    {
        StoryBeat best = FindBestBeat(eventId);
        return TryHandleBeat(best);
    }

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

    private void TryAutoPlayFirstBeat()
    {
        if (beats == null || beats.Length == 0) return;
        if (beats[0] == null) return;
        if (DialogueManager.Instance == null) return;
        if (DialogueManager.Instance.IsDialogueActive) return;

        StoryBeat first = beats[0];
        TryHandleBeat(first);
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
        if (IsOneShotEnabled(beat) && IsBeatAlreadyPlayed(beat)) return false;
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

    private bool IsBeatAlreadyPlayed(StoryBeat beat)
    {
        string key = GetBeatKey(beat);
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (StoryFlags.Has(key)) return true;
        if (!persistBeatPlayedToPlayerPrefs) return false;
        return PlayerPrefs.GetInt(BeatPlayedPrefPrefix + key, 0) == 1;
    }

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

    private bool IsOneShotEnabled(StoryBeat beat)
    {
        if (beat == null) return true;
        if (beat.useAttemptNarration) return false;
        return true;
    }

    private string GetBeatKey(StoryBeat beat)
    {
        if (beat == null) return string.Empty;
        string safeEventId = string.IsNullOrWhiteSpace(beat.eventId) ? "NoEvent" : beat.eventId.Trim();
        string safeName = string.IsNullOrWhiteSpace(beat.name) ? "UnnamedBeat" : beat.name.Trim();
        return $"StoryBeat:{safeEventId}:{safeName}";
    }
}
