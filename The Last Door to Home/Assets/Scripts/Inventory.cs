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

public static class Inventory
{
    public static List<string> collectedItemNames = new List<string>();
    private static HashSet<string> collectedIDs = new HashSet<string>();
    private static HashSet<ItemType> collectedTypes = new HashSet<ItemType>();
    private static HashSet<string> unlockedSafeIDs = new HashSet<string>();
    private static HashSet<string> unlockedDoorIDs = new HashSet<string>();

    public static void AddItem(string name, ItemType type, string uniqueID)
    {
        if (!collectedIDs.Contains(uniqueID))
        {
            collectedIDs.Add(uniqueID);
            collectedItemNames.Add(name);
            collectedTypes.Add(type);
        }
    }

    public static bool HasCollected(string uniqueID)
    {
        return collectedIDs.Contains(uniqueID);
    }

    public static bool HasItem(string itemName)
    {
        return collectedItemNames.Contains(itemName);
    }

    public static bool HasItemType(ItemType itemType)
    {
        return collectedTypes.Contains(itemType);
    }

    public static void MarkSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return;
        unlockedSafeIDs.Add(safeUniqueID);
    }

    public static bool IsSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return false;
        return unlockedSafeIDs.Contains(safeUniqueID);
    }

    public static void MarkDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return;
        unlockedDoorIDs.Add(doorUniqueID);
    }

    public static bool IsDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return false;
        return unlockedDoorIDs.Contains(doorUniqueID);
    }

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
