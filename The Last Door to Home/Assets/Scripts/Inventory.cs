using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Key,
    Tool,
    Note,
    Flower,
    stone
}

/*
Purpose: Provides shared i nv en to ry utilities for other gameplay systems.
Attached GameObject: Not attached; accessed as a static utility from other systems.
Main responsibilities: Maintain shared state or helper operations and provide a central access point.
Inputs: Method parameters, saved runtime state, and calls from other scripts.
Outputs or effects: Updated shared state and return values consumed by other systems.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public static class Inventory
{
    public static List<string> collectedItemNames = new List<string>();
    private static HashSet<string> collectedIDs = new HashSet<string>();
    private static HashSet<ItemType> collectedTypes = new HashSet<ItemType>();
    private static HashSet<string> unlockedSafeIDs = new HashSet<string>();
    private static HashSet<string> unlockedDoorIDs = new HashSet<string>();

    // Adds the supplied item to the shared inventory state.
    public static void AddItem(string name, ItemType type, string uniqueID)
    {
        if (!collectedIDs.Contains(uniqueID))
        {
            collectedIDs.Add(uniqueID);
            collectedItemNames.Add(name);
            collectedTypes.Add(type);
        }
    }

    // Checks whether the requested unique item has already been collected.
    public static bool HasCollected(string uniqueID)
    {
        return collectedIDs.Contains(uniqueID);
    }

    // Checks whether the named inventory item is present.
    public static bool HasItem(string itemName)
    {
        return collectedItemNames.Contains(itemName);
    }

    // Checks whether any collected item matches the requested type.
    public static bool HasItemType(ItemType itemType)
    {
        return collectedTypes.Contains(itemType);
    }

    // Marks the requested safe as unlocked in shared state.
    public static void MarkSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return;
        unlockedSafeIDs.Add(safeUniqueID);
    }

    // Checks whether the requested safe has already been unlocked.
    public static bool IsSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return false;
        return unlockedSafeIDs.Contains(safeUniqueID);
    }

    // Marks the requested door as unlocked in shared state.
    public static void MarkDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return;
        unlockedDoorIDs.Add(doorUniqueID);
    }

    // Checks whether the requested door has already been unlocked.
    public static bool IsDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return false;
        return unlockedDoorIDs.Contains(doorUniqueID);
    }

    // Clears the stored runtime state managed by this utility.
    public static void Clear()
    {
        collectedIDs.Clear();
        collectedItemNames.Clear();
        collectedTypes.Clear();
        unlockedSafeIDs.Clear();
        unlockedDoorIDs.Clear();
        StoryFlags.Clear();
    }
}
