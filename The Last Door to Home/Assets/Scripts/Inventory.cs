using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    Key,
    Tool,
    Note,
    Flower,
    Box
}

public static class Inventory
{
    public static List<string> collectedItemNames = new List<string>();
    private static HashSet<string> collectedIDs = new HashSet<string>();
    private static HashSet<ItemType> collectedTypes = new HashSet<ItemType>();

    // 每次进游戏都强制清空！
    static Inventory()
    {
        collectedIDs.Clear();
        collectedItemNames.Clear();
        collectedTypes.Clear();
    }

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

    public static void Clear()
    {
        collectedIDs.Clear();
        collectedItemNames.Clear();
        collectedTypes.Clear();
    }
}
