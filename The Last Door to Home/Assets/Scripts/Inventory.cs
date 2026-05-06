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

    // 每次进游戏都强制清空！
    static Inventory()
    {
        collectedIDs.Clear();
        collectedItemNames.Clear();
    }

    public static void AddItem(string name, ItemType type, string uniqueID)
    {
        if (!collectedIDs.Contains(uniqueID))
        {
            collectedIDs.Add(uniqueID);
            collectedItemNames.Add(name);
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

    public static void Clear()
    {
        collectedIDs.Clear();
        collectedItemNames.Clear();
    }
}