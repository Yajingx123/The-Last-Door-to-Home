using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
/*
Purpose: Saves, loads, and summarizes the game state for the slot-based save system.
Attached GameObject: Static helper; no GameObject attachment required.
Main responsibilities: Serializes save data, restores inventory/story/session state, resolves player position, and loads saved scenes.
Inputs: Slot index, active scene, player transform, inventory state, story flags, and persistent save files.
Outputs or effects: Writes JSON files, restores runtime state, returns status messages, and starts scene transitions.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify save overwrite, empty-slot handling, corrupted-file handling, load restore, and boss-scene spawn fallback.
*/

public static class SaveSystem
{
    public const int SlotCount = 10;
    private const string SaveFolderName = "Saves";

    // 把当前游戏状态保存到指定存档槽里。
    // Saves the current data or runtime state.
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

    // 从指定存档槽读取数据，并恢复场景、玩家位置和游戏状态。
    // Loads the requested data, scene, or runtime content.
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

    // 获取所有存档槽的简要信息，用来显示读档/存档列表。
    // Returns the requested value or runtime object.
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

    // 检查当前是否至少有一个可用存档。
    // Returns whether the required has any save condition is met.
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

    // 尝试读取最近保存的存档槽。
    // Attempts the requested operation and reports whether it succeeded.
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

    // 把游玩秒数转换成小时:分钟:秒的显示格式。
    // Handles the format play time step for this script.
    public static string FormatPlayTime(float seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
        int totalHours = Mathf.FloorToInt((float)span.TotalHours);
        return $"{totalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    // 收集当前场景、玩家、背包和剧情状态，打包成存档数据。
    // Builds data or UI objects required by this system.
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

    // 获取玩家当前位置，用来存档后恢复出生位置。
    // Resolves the best available value for the requested data.
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

    // 判断当前是不是Boss相关场景，Boss场景读档时不直接恢复原坐标。
    // Returns whether is boss scene is true for the current state.
    private static bool IsBossScene()
    {
        return UnityEngine.Object.FindObjectOfType<MonsterController>() != null
            || UnityEngine.Object.FindObjectOfType<EyeMonster>() != null
            || UnityEngine.Object.FindObjectOfType<VineMonster>() != null
            || UnityEngine.Object.FindObjectOfType<StalkerMonster>() != null;
    }

    // 尝试读取一个存档槽的JSON文件，并转换成SaveData。
    // Attempts the requested operation and reports whether it succeeded.
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

    // 检查存档槽编号是不是在允许范围内。
    // Returns whether is valid slot index is true for the current state.
    private static bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < SlotCount;
    }

    // 把存档时间字符串转换成UTC时间，方便比较哪个存档最新。
    // Handles the parse saved at utc step for this script.
    private static DateTime ParseSavedAtUtc(string savedAtUtc)
    {
        if (DateTime.TryParse(savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime parsed))
        {
            return parsed.ToUniversalTime();
        }

        return DateTime.MinValue;
    }

    // 获取存档文件夹路径，也就是游戏实际写入存档的位置。
    // Returns the requested value or runtime object.
    private static string GetSaveFolderPath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFolderName);
    }

    // 获取某个存档槽对应的具体JSON文件路径。
    // Returns the requested value or runtime object.
    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(GetSaveFolderPath(), $"slot_{slotIndex + 1:00}.json");
    }
}
