using System.Collections.Generic;
using System;
using UnityEngine;
/*
Purpose: Stores collected items and unlocked object state for the current game run.
Attached GameObject: Static helper; no GameObject attachment required.
Main responsibilities: Adds collected items, checks item/type ownership, tracks unlocked safes/doors, and imports/exports save data.
Inputs: Pickup calls, unique item IDs, item metadata, save data, and unlock requests.
Outputs or effects: Updates shared inventory state, raises ItemCollected, and returns serialized records for saving.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify duplicate collection prevention, item lookup, unlock tracking, and import/export round trips.
*/

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
    public static event Action<string> ItemCollected;

    public static List<string> collectedItemNames = new List<string>();
    private static List<string> collectedItemOrder = new List<string>();
    private static HashSet<string> collectedIDs = new HashSet<string>();
    private static HashSet<ItemType> collectedTypes = new HashSet<ItemType>();
    private static Dictionary<string, InventoryItemRecord> collectedItemRecords = new Dictionary<string, InventoryItemRecord>();
    private static HashSet<string> unlockedSafeIDs = new HashSet<string>();
    private static HashSet<string> unlockedDoorIDs = new HashSet<string>();

    // Handles the add item step for this script.
    public static void AddItem(string name, ItemType type, string uniqueID, string description = "", string iconResourcePath = "", Sprite runtimeIcon = null)
    {
        if (!collectedIDs.Contains(uniqueID))
        {
            Sprite resolvedIcon = runtimeIcon;
            if (resolvedIcon == null && !string.IsNullOrWhiteSpace(iconResourcePath))
            {
                resolvedIcon = LoadIconFromResources(iconResourcePath);
            }

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
                runtimeIcon = resolvedIcon
            };
            ItemCollected?.Invoke(uniqueID);
        }
    }

    // Returns whether the required has collected condition is met.
    public static bool HasCollected(string uniqueID)
    {
        return collectedIDs.Contains(uniqueID);
    }

    // Returns whether the required has item condition is met.
    public static bool HasItem(string itemName)
    {
        return collectedItemNames.Contains(itemName);
    }

    // Returns whether the required has item type condition is met.
    public static bool HasItemType(ItemType itemType)
    {
        return collectedTypes.Contains(itemType);
    }

    // Handles the mark safe unlocked step for this script.
    public static void MarkSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return;
        unlockedSafeIDs.Add(safeUniqueID);
    }

    // Returns whether is safe unlocked is true for the current state.
    public static bool IsSafeUnlocked(string safeUniqueID)
    {
        if (string.IsNullOrEmpty(safeUniqueID)) return false;
        return unlockedSafeIDs.Contains(safeUniqueID);
    }

    // Handles the mark door unlocked step for this script.
    public static void MarkDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return;
        unlockedDoorIDs.Add(doorUniqueID);
    }

    // Returns whether is door unlocked is true for the current state.
    public static bool IsDoorUnlocked(string doorUniqueID)
    {
        if (string.IsNullOrEmpty(doorUniqueID)) return false;
        return unlockedDoorIDs.Contains(doorUniqueID);
    }

    // Exports runtime state into serializable data.
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

    // Exports runtime state into serializable data.
    public static List<string> ExportUnlockedSafeIds()
    {
        return new List<string>(unlockedSafeIDs);
    }

    // Exports runtime state into serializable data.
    public static List<string> ExportUnlockedDoorIds()
    {
        return new List<string>(unlockedDoorIDs);
    }

    // Restores runtime state from serialized data.
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

    // Clears the stored runtime state managed by this system.
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

    // Loads a single Sprite path, or a sliced sprite using "SheetPath#SpriteName".
    private static Sprite LoadIconFromResources(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath)) return null;

        string trimmedPath = resourcePath.Trim();
        int spriteNameSeparator = trimmedPath.IndexOf('#');
        if (spriteNameSeparator >= 0)
        {
            string sheetPath = trimmedPath.Substring(0, spriteNameSeparator);
            string spriteName = trimmedPath.Substring(spriteNameSeparator + 1);
            if (string.IsNullOrWhiteSpace(sheetPath) || string.IsNullOrWhiteSpace(spriteName)) return null;

            Sprite[] sprites = Resources.LoadAll<Sprite>(sheetPath);
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && string.Equals(sprites[i].name, spriteName, StringComparison.Ordinal))
                {
                    return sprites[i];
                }
            }

            Debug.LogWarning($"Inventory: Could not find sprite '{spriteName}' in Resources path '{sheetPath}'.");
            return null;
        }

        return Resources.Load<Sprite>(trimmedPath);
    }
}
