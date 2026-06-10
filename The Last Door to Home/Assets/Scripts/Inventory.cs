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
    private static List<string> collectedItemOrder = new List<string>();
    private static HashSet<string> collectedIDs = new HashSet<string>();
    private static HashSet<ItemType> collectedTypes = new HashSet<ItemType>();
    private static Dictionary<string, InventoryItemRecord> collectedItemRecords = new Dictionary<string, InventoryItemRecord>();
    private static HashSet<string> unlockedSafeIDs = new HashSet<string>();
    private static HashSet<string> unlockedDoorIDs = new HashSet<string>();

    // Adds the supplied item to the shared inventory state.
    public static void AddItem(string name, ItemType type, string uniqueID, string description = "", string iconResourcePath = "", Sprite runtimeIcon = null)
    {
        if (!collectedIDs.Contains(uniqueID))
        {
            collectedIDs.Add(uniqueID);
            collectedItemOrder.Add(uniqueID);
            collectedItemNames.Add(name);
            collectedTypes.Add(type);
            collectedItemRecords[uniqueID] = new InventoryItemRecord
            {
                itemName = name,
                itemType = type,
                uniqueID = uniqueID,
                itemDescription = description ?? string.Empty,
                itemIconResourcePath = iconResourcePath ?? string.Empty,
                runtimeIcon = runtimeIcon
            };
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

    // Exports the collected inventory items for save serialization.
    public static List<InventoryItemRecord> ExportCollectedItems()
    {
        var records = new List<InventoryItemRecord>();
        for (int i = 0; i < collectedItemOrder.Count; i++)
        {
            string uniqueID = collectedItemOrder[i];
            if (string.IsNullOrWhiteSpace(uniqueID)) continue;
            if (!collectedItemRecords.TryGetValue(uniqueID, out InventoryItemRecord matchedRecord) || matchedRecord == null) continue;

            records.Add(new InventoryItemRecord
            {
                uniqueID = matchedRecord.uniqueID,
                itemName = matchedRecord.itemName,
                itemType = matchedRecord.itemType,
                itemDescription = matchedRecord.itemDescription,
                itemIconResourcePath = matchedRecord.itemIconResourcePath,
                runtimeIcon = matchedRecord.runtimeIcon
            });
        }

        return records;
    }

    // Exports unlocked safe identifiers for save serialization.
    public static List<string> ExportUnlockedSafeIds()
    {
        return new List<string>(unlockedSafeIDs);
    }

    // Exports unlocked door identifiers for save serialization.
    public static List<string> ExportUnlockedDoorIds()
    {
        return new List<string>(unlockedDoorIDs);
    }

    // Restores the shared inventory state from save data.
    public static void ImportState(List<InventoryItemRecord> items, List<string> safeIds, List<string> doorIds)
    {
        collectedIDs.Clear();
        collectedItemOrder.Clear();
        collectedItemNames.Clear();
        collectedTypes.Clear();
        collectedItemRecords.Clear();
        unlockedSafeIDs.Clear();
        unlockedDoorIDs.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                InventoryItemRecord item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.uniqueID)) continue;

                collectedIDs.Add(item.uniqueID);
                collectedItemOrder.Add(item.uniqueID);
                collectedItemNames.Add(item.itemName ?? string.Empty);
                collectedTypes.Add(item.itemType);
                collectedItemRecords[item.uniqueID] = new InventoryItemRecord
                {
                    uniqueID = item.uniqueID,
                    itemName = item.itemName ?? string.Empty,
                    itemType = item.itemType,
                    itemDescription = item.itemDescription ?? string.Empty,
                    itemIconResourcePath = item.itemIconResourcePath ?? string.Empty,
                    runtimeIcon = LoadIconFromResources(item.itemIconResourcePath)
                };
            }
        }

        if (safeIds != null)
        {
            for (int i = 0; i < safeIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(safeIds[i])) continue;
                unlockedSafeIDs.Add(safeIds[i]);
            }
        }

        if (doorIds != null)
        {
            for (int i = 0; i < doorIds.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(doorIds[i])) continue;
                unlockedDoorIDs.Add(doorIds[i]);
            }
        }
    }

    // Clears the stored runtime state managed by this utility.
    public static void Clear()
    {
        collectedIDs.Clear();
        collectedItemOrder.Clear();
        collectedItemNames.Clear();
        collectedTypes.Clear();
        collectedItemRecords.Clear();
        unlockedSafeIDs.Clear();
        unlockedDoorIDs.Clear();
        StoryFlags.Clear();
    }

    private static Sprite LoadIconFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;
        return Resources.Load<Sprite>(resourcePath);
    }
}
