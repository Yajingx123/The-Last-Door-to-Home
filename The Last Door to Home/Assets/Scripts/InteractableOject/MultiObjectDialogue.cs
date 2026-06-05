using UnityEngine;

/// <summary>
/// 多对象共享对话进度：
/// - targets: 指定属于同一组进度的对象
/// - beatSet: 按顺序触发的剧情条目
/// - 每个对象首次触发会推进一次进度
/// - 当进度超过 beat 数量时，索引停在最后一个 beat（但仍受 StoryBeat 全局只播一次限制）
/// </summary>
/*
Purpose: Manages m ul ti ob je ct di al og ue behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class MultiObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("参与这组进度的对象")]
    public GameObject[] targets;

    [Header("按顺序触发的剧情条目组")]
    public StoryBeatSet beatSet;

    [Header("进度组ID（同一组对象要相同）")]
    public string groupId = "multi_object_dialogue";

    [Header("未命中剧情时的兜底对白（可选）")]
    [TextArea(2, 6)]
    public string[] fallbackDialogues;

    [Header("调试")]
    public bool enableDebugLogs = true;

    // Executes this object interaction when the player activates it.
    public void OnInteract()
    {
        if (DialogueManager.Instance == null) return;
        if (beatSet == null || beatSet.beats == null || beatSet.beats.Length == 0)
        {
            Log("BeatSet 为空，走 fallback。");
            ShowFallback();
            return;
        }

        string group = Normalize(groupId, "multi_object_dialogue");
        GameObject triggerTarget = ResolveTriggerTarget();
        string targetId = GetStableId(triggerTarget != null ? triggerTarget.transform : transform);
        string visitedFlag = $"MOD:Visited:{group}:{targetId}";

        int progress = StoryFlags.GetCounter(GetCounterKey(group));
        if (!StoryFlags.Has(visitedFlag))
        {
            StoryFlags.Set(visitedFlag);
            progress = StoryFlags.IncrementCounter(GetCounterKey(group));
            Log($"首次触发对象: {targetId}，进度+1 => {progress}");
        }
        else
        {
            Log($"重复触发对象: {targetId}，进度保持 => {progress}");
        }

        int beatIndex = Mathf.Clamp(progress - 1, 0, beatSet.beats.Length - 1);
        StoryBeat beatToPlay = beatSet.beats[beatIndex];
        Log($"选择 Beat 索引={beatIndex}，Beat={(beatToPlay != null ? beatToPlay.name : "NULL")}");
        bool handled = StoryDirector.Instance != null && StoryDirector.Instance.TryHandleBeat(beatToPlay);

        if (!handled)
        {
            Log(GetBlockReason(beatToPlay));
            ShowFallback();
        }
        else
        {
            Log("Beat 播放成功。");
        }
    }

    // Resolves the trigger target that this interaction should use.
    private GameObject ResolveTriggerTarget()
    {
        if (targets == null || targets.Length == 0) return gameObject;

        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null)
        {
            return targets[0];
        }

        GameObject best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject candidate = targets[i];
            if (candidate == null) continue;

            Vector2 point = GetClosestPoint(candidate, playerTransform.position);
            float d = Vector2.Distance(playerTransform.position, point);

            if (d < bestDistance)
            {
                bestDistance = d;
                best = candidate;
            }
        }

        return best != null ? best : gameObject;
    }

    // Finds or returns the player transform used by this interaction.
    private Transform GetPlayerTransform()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.player != null)
        {
            return DialogueManager.Instance.player.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    // Returns the closest valid interaction point for the player.
    private Vector2 GetClosestPoint(GameObject target, Vector3 playerPos)
    {
        if (target == null) return playerPos;

        Collider2D[] cols = target.GetComponentsInChildren<Collider2D>(true);
        if (cols == null || cols.Length == 0)
        {
            return target.transform.position;
        }

        Vector2 bestPoint = target.transform.position;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D col = cols[i];
            if (col == null) continue;
            Vector2 p = col.ClosestPoint(playerPos);
            float d = Vector2.Distance(playerPos, p);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestPoint = p;
            }
        }

        return bestPoint;
    }

    // Builds a stable identifier for this interactable target.
    private string GetStableId(Transform targetTransform)
    {
        if (targetTransform == null) return "UnknownTarget";
        return gameObject.scene.name + ":" + targetTransform.GetHierarchyPath();
    }

    // Builds the counter key used for repeat interaction tracking.
    private static string GetCounterKey(string group)
    {
        return $"MOD:Counter:{group}";
    }

    // Normalizes the supplied identifier into a consistent comparison format.
    private static string Normalize(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Trim();
    }

    // Shows fallback feedback when no specific interaction result is available.
    private void ShowFallback()
    {
        if (fallbackDialogues == null || fallbackDialogues.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(fallbackDialogues);
    }

    // Returns the blocking reason that prevents this interaction from proceeding.
    private string GetBlockReason(StoryBeat beat)
    {
        if (StoryDirector.Instance == null) return "StoryDirector.Instance 为空。";
        if (beat == null) return "选中的 Beat 为 NULL。";
        if (DialogueManager.Instance == null) return "DialogueManager.Instance 为空。";
        if (DialogueManager.Instance.IsDialogueActive) return "当前已有对话在播放中。";

        string beatKey = $"StoryBeat:{(string.IsNullOrWhiteSpace(beat.eventId) ? "NoEvent" : beat.eventId.Trim())}:{(string.IsNullOrWhiteSpace(beat.name) ? "UnnamedBeat" : beat.name.Trim())}";
        if (StoryFlags.Has(beatKey) || PlayerPrefs.GetInt("StoryBeatPlayed:" + beatKey, 0) == 1)
        {
            return $"Beat 已播放（全局一次性拦截）: {beatKey}";
        }

        if (!HasAllFlags(beat.requiredFlags)) return "requiredFlags 条件不满足。";
        if (HasAnyFlag(beat.blockedFlags)) return "blockedFlags 命中。";
        if (!HasAllItems(beat.requiredItemUniqueIDs)) return "requiredItemUniqueIDs 条件不满足。";
        if (HasAnyItem(beat.blockedItemUniqueIDs)) return "blockedItemUniqueIDs 命中。";

        string[] lines = ResolveDialoguesForDebug(beat);
        if (lines == null || lines.Length == 0) return "对白为空（dialogues/attemptDialogues 无可用文本）。";

        return "未知原因（建议检查 Console 其他报错）。";
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

    // Builds a readable dialogue summary for debugging output.
    private string[] ResolveDialoguesForDebug(StoryBeat beat)
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

    // Writes a contextual debug message for this interaction helper.
    private void Log(string msg)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[MultiObjectDialogue] {msg}", this);
    }
}

public static class TransformPathExtensions
{
    // Builds a readable hierarchy path for debugging and lookup logs.
    public static string GetHierarchyPath(this Transform transform)
    {
        if (transform == null) return "Unknown";

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
