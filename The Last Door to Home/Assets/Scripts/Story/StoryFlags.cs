using System.Collections.Generic;

/*
Purpose: Provides shared s to ry fl ag s utilities for other gameplay systems.
Attached GameObject: Not attached; accessed as a static utility from other systems.
Main responsibilities: Maintain shared state or helper operations and provide a central access point.
Inputs: Method parameters, saved runtime state, and calls from other scripts.
Outputs or effects: Updated shared state and return values consumed by other systems.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public static class StoryFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();
    private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    // Checks whether the requested story flag is currently set.
    public static bool Has(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return false;
        return flags.Contains(flag);
    }

    // Sets the requested story flag in shared narrative state.
    public static void Set(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;
        flags.Add(flag);
    }

    // Returns the current value of the requested story counter.
    public static int GetCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        return counters.TryGetValue(key, out int value) ? value : 0;
    }

    // Increments the requested story counter by one.
    public static int IncrementCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;

        int value = GetCounter(key) + 1;
        counters[key] = value;
        return value;
    }

    // Exports the current story flags for save serialization.
    public static List<string> ExportFlags()
    {
        return new List<string>(flags);
    }

    // Exports the current story counters for save serialization.
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

    // Restores story flags and counters from save data.
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

    // Clears the stored runtime state managed by this utility.
    public static void Clear()
    {
        flags.Clear();
        counters.Clear();
    }
}
