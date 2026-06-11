using UnityEngine;

/// <summary>
/// Shares dialogue progress across multiple objects.
/// - targets: Objects that belong to the same progress group.
/// - beatSet: Story beats triggered in sequence.
/// - Each object's first interaction advances progress once.
/// - When progress exceeds the beat count, the index stays on the final beat.
/// </summary>
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
*/

public class MultiObjectDialogue : MonoBehaviour, IInteractable
{
    [Header("参与这组进度的对象 / Objects In This Progress Group")]
    public GameObject[] targets;

    [Header("按顺序触发的剧情条目组 / Sequential Story Beat Group")]
    public StoryBeatSet beatSet;

    [Header("进度组ID（同一组对象要相同） / Progress Group ID (Same For Group Objects)")]
    public string groupId = "multi_object_dialogue";

    [Header("未命中剧情时的兜底对白（可选） / Fallback Dialogue When No Story Matches (Optional)")]
    [TextArea(2, 6)]
    public string[] fallbackDialogues;

    [Header("调试 / Debug")]
    public bool enableDebugLogs = true;

    // Handles player interaction with this object.
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

    // Resolves the best available value for the requested data.
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

    // Returns the requested value or runtime object.
    private Transform GetPlayerTransform()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.player != null)
        {
            return DialogueManager.Instance.player.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    // Returns the requested value or runtime object.
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

    // Returns the requested value or runtime object.
    private string GetStableId(Transform targetTransform)
    {
        if (targetTransform == null) return "UnknownTarget";
        return gameObject.scene.name + ":" + targetTransform.GetHierarchyPath();
    }

    // Returns the requested value or runtime object.
    private static string GetCounterKey(string group)
    {
        return $"MOD:Counter:{group}";
    }

    // Normalizes the normalize value for reliable comparisons.
    private static string Normalize(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return value.Trim();
    }

    // Shows the show fallback UI or dialogue flow.
    private void ShowFallback()
    {
        if (fallbackDialogues == null || fallbackDialogues.Length == 0) return;
        DialogueManager.Instance.ShowDialogue(fallbackDialogues);
    }

    // Returns the requested value or runtime object.
    private string GetBlockReason(StoryBeat beat)
    {
        if (StoryDirector.Instance == null) return "StoryDirector.Instance 为空。";
        if (beat == null) return "选中的 Beat 为 NULL。";
        if (DialogueManager.Instance == null) return "DialogueManager.Instance 为空。";
        if (DialogueManager.Instance.IsDialogueActive) return "当前已有对话在播放中。";

        string beatKey = $"StoryBeat:{(string.IsNullOrWhiteSpace(beat.eventId) ? "NoEvent" : beat.eventId.Trim())}:{(string.IsNullOrWhiteSpace(beat.name) ? "UnnamedBeat" : beat.name.Trim())}";
        if (StoryFlags.Has(beatKey))
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

    // Writes debug information for the log flow when logging is enabled.
    private void Log(string msg)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[MultiObjectDialogue] {msg}", this);
    }
}
public static class TransformPathExtensions
{
    // Returns the requested value or runtime object.
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
