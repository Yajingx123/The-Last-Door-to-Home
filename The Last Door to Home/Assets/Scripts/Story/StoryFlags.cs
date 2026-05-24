using System.Collections.Generic;

public static class StoryFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();
    private static readonly Dictionary<string, int> counters = new Dictionary<string, int>();

    public static bool Has(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return false;
        return flags.Contains(flag);
    }

    public static void Set(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag)) return;
        flags.Add(flag);
    }

    public static int GetCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;
        return counters.TryGetValue(key, out int value) ? value : 0;
    }

    public static int IncrementCounter(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return 0;

        int value = GetCounter(key) + 1;
        counters[key] = value;
        return value;
    }

    public static void Clear()
    {
        flags.Clear();
        counters.Clear();
    }
}
