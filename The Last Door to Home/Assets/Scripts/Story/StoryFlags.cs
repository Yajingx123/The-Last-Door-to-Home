using System.Collections.Generic;
/*
Purpose: Defines or executes story progression logic.
Attached GameObject: Story controller, trigger object, or ScriptableObject asset depending on the script type.
Main responsibilities: Tracks story flags/counters, evaluates story beats, and triggers dialogue or ending flows.
Inputs: Story event IDs, flags, counters, configured beats, interaction triggers, and scene state.
Outputs or effects: Updates story state, starts dialogue/events, blocks progression when needed, or triggers ending sequences.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify each story event fires once or repeats as designed and survives save/load when required.
*/

public static class StoryFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();
    private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    // Returns whether the required has condition is met.
    public static bool Has(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return false;
        return flags.Contains(flag);
    }

    // Updates the requested value or component state.
    public static void Set(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;
        flags.Add(flag);
    }

    // Returns the requested value or runtime object.
    public static int GetCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        return counters.TryGetValue(key, out int value) ? value : 0;
    }

    // Handles the increment counter step for this script.
    public static int IncrementCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;

        int value = GetCounter(key) + 1;
        counters[key] = value;
        return value;
    }

    // Exports runtime state into serializable data.
    public static List<string> ExportFlags()
    {
        return new List<string>(flags);
    }

    // Exports runtime state into serializable data.
    public static List<StoryCounterRecord> ExportCounters()
    {
        var records = new List<StoryCounterRecord>();
        foreach (KeyValuePair<string, int> pair in counters)
        {
            if (string.IsNullOrWhiteSpace(pair.Key)) continue;
            records.Add(new StoryCounterRecord { key = pair.Key, value = pair.Value });
        }

        return records;
    }

    // Restores runtime state from serialized data.
    public static void ImportState(List<string> savedFlags, List<StoryCounterRecord> savedCounters)
    {
        flags.Clear();
        counters.Clear();

        if (savedFlags != null)
        {
            for (int i = 0; i < savedFlags.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(savedFlags[i])) continue;
                flags.Add(savedFlags[i]);
            }
        }

        if (savedCounters != null)
        {
            for (int i = 0; i < savedCounters.Count; i++)
            {
                StoryCounterRecord record = savedCounters[i];
                if (record == null || string.IsNullOrWhiteSpace(record.key)) continue;
                counters[record.key] = record.value;
            }
        }
    }

    // Clears the stored runtime state managed by this system.
    public static void Clear()
    {
        flags.Clear();
        counters.Clear();
    }
}
