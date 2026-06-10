using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
Purpose: Saves and loads the lightweight game state used by the pause-menu slot system.
Attached GameObject: Not attached; accessed through static helpers.
Main responsibilities: Serialize save slots, restore runtime state, and coordinate scene reload behavior.
Inputs: Inventory/story state, active scene info, player position, and selected save slot.
Outputs or effects: Writes JSON save files, restores runtime state, and triggers scene transitions on load.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify slot overwrite, empty-slot handling, boss-scene spawn fallback, and restored story progression after load.
*/

public static class SaveSystem
{
    public const int SlotCount = 10;
    private const string SaveFolderName = "Saves";

    public static bool SaveToSlot(int slotIndex, out string message)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            message = "Invalid save slot.";
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
        {
            message = "Current scene cannot be saved.";
            return false;
        }

        GameSessionTracker.EnsureSessionStarted();

        SaveData data = BuildSaveData(slotIndex, activeScene);
        string path = GetSlotPath(slotIndex);
        Directory.CreateDirectory(GetSaveFolderPath());
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        message = $"Saved to slot {slotIndex + 1}.";
        return true;
    }

    public static bool LoadFromSlot(int slotIndex, out string message)
    {
        if (!TryReadSlot(slotIndex, out SaveData data))
        {
            message = "This save slot is empty.";
            return false;
        }

        Inventory.Clear();
        Inventory.ImportState(data.collectedItems, data.unlockedSafeIds, data.unlockedDoorIds);
        StoryFlags.ImportState(data.storyFlags, data.storyCounters);
        CutsceneReturnContext.Clear();
        GameSessionTracker.RestoreSession(data.playTimeSeconds);

        if (data.restorePlayerPosition)
        {
            PlayerSpawn.SPAWN_POSITION = new Vector2(data.playerPositionX, data.playerPositionY);
            PlayerSpawn.NEED_SPAWN = true;
        }
        else
        {
            PlayerSpawn.NEED_SPAWN = false;
        }

        SceneTransition.LoadScene(data.sceneName);
        message = $"Loaded slot {slotIndex + 1}.";
        return true;
    }

    public static List<SaveSlotSummary> GetSlotSummaries()
    {
        var summaries = new List<SaveSlotSummary>(SlotCount);
        for (int i = 0; i < SlotCount; i++)
        {
            if (TryReadSlot(i, out SaveData data))
            {
                summaries.Add(new SaveSlotSummary
                {
                    slotIndex = i,
                    hasData = true,
                    sceneName = data.sceneName,
                    playTimeSeconds = data.playTimeSeconds,
                    savedAtUtc = data.savedAtUtc
                });
                continue;
            }

            summaries.Add(new SaveSlotSummary
            {
                slotIndex = i,
                hasData = false,
                sceneName = string.Empty,
                playTimeSeconds = 0f,
                savedAtUtc = string.Empty
            });
        }

        return summaries;
    }

    public static bool HasAnySave()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (TryReadSlot(i, out _))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryLoadMostRecentSlot(out string message)
    {
        int latestSlotIndex = -1;
        DateTime latestSaveTime = DateTime.MinValue;

        for (int i = 0; i < SlotCount; i++)
        {
            if (!TryReadSlot(i, out SaveData data))
            {
                continue;
            }

            DateTime parsedTime = ParseSavedAtUtc(data.savedAtUtc);
            if (latestSlotIndex < 0 || parsedTime > latestSaveTime)
            {
                latestSlotIndex = i;
                latestSaveTime = parsedTime;
            }
        }

        if (latestSlotIndex < 0)
        {
            message = "No save data found.";
            return false;
        }

        return LoadFromSlot(latestSlotIndex, out message);
    }

    public static string FormatPlayTime(float seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        int totalHours = Mathf.FloorToInt((float)span.TotalHours);
        return $"{totalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    private static SaveData BuildSaveData(int slotIndex, Scene activeScene)
    {
        Vector2 playerPosition = ResolvePlayerPosition();
        bool restorePosition = !IsBossScene();

        return new SaveData
        {
            slotIndex = slotIndex,
            sceneName = activeScene.name,
            restorePlayerPosition = restorePosition,
            playerPositionX = playerPosition.x,
            playerPositionY = playerPosition.y,
            playTimeSeconds = GameSessionTracker.PlayTimeSeconds,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            collectedItems = Inventory.ExportCollectedItems(),
            unlockedSafeIds = Inventory.ExportUnlockedSafeIds(),
            unlockedDoorIds = Inventory.ExportUnlockedDoorIds(),
            storyFlags = StoryFlags.ExportFlags(),
            storyCounters = StoryFlags.ExportCounters()
        };
    }

    private static Vector2 ResolvePlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return player.transform.position;
        }

        if (PlayerSpawn.NEED_SPAWN)
        {
            return PlayerSpawn.SPAWN_POSITION;
        }

        return Vector2.zero;
    }

    private static bool IsBossScene()
    {
        return UnityEngine.Object.FindObjectOfType<MonsterController>() != null
            || UnityEngine.Object.FindObjectOfType<EyeMonster>() != null
            || UnityEngine.Object.FindObjectOfType<VineMonster>() != null
            || UnityEngine.Object.FindObjectOfType<StalkerMonster>() != null;
    }

    private static bool TryReadSlot(int slotIndex, out SaveData data)
    {
        data = null;
        if (!IsValidSlotIndex(slotIndex)) return false;

        string path = GetSlotPath(slotIndex);
        if (!File.Exists(path)) return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null && !string.IsNullOrWhiteSpace(data.sceneName);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"SaveSystem: Failed to read slot {slotIndex + 1}. {ex.Message}");
            data = null;
            return false;
        }
    }

    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }

    private static DateTime ParseSavedAtUtc(string savedAtUtc)
    {
        if (DateTime.TryParse(savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return DateTime.MinValue;
    }

    private static string GetSaveFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFolderName);
    }

    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(GetSaveFolderPath(), $"slot_{slotIndex + 1:00}.json");
    }
}
