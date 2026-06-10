using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int slotIndex;
    public string sceneName;
    public bool restorePlayerPosition;
    public float playerPositionX;
    public float playerPositionY;
    public float playTimeSeconds;
    public string savedAtUtc;
    public List<InventoryItemRecord> collectedItems = new List<InventoryItemRecord>();
    public List<string> unlockedSafeIds = new List<string>();
    public List<string> unlockedDoorIds = new List<string>();
    public List<string> storyFlags = new List<string>();
    public List<StoryCounterRecord> storyCounters = new List<StoryCounterRecord>();
}

[Serializable]
public class InventoryItemRecord
{
    public string itemName;
    public string uniqueID;
    public ItemType itemType;
    public string itemDescription;
    public string itemIconResourcePath;
    [NonSerialized] public Sprite runtimeIcon;
}

[Serializable]
public class StoryCounterRecord
{
    public string key;
    public int value;
}

[Serializable]
public class SaveSlotSummary
{
    public int slotIndex;
    public bool hasData;
    public string sceneName;
    public float playTimeSeconds;
    public string savedAtUtc;
}
