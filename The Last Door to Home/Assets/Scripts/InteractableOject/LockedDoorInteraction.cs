using System;
using System.Collections.Generic;
using UnityEngine;
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
*/

public class LockedDoorInteraction : MonoBehaviour, IInteractable
{
    [Serializable]
public class DoorKeyOption
    {
        [Header("这个Key的唯一ID（匹配 PickableItem.itemUniqueID） / Key Unique ID (Matches PickableItem.itemUniqueID)")]
        public string keyUniqueID = "";

        [Header("显示在选项里的文本 / Text Shown In Option")]
        public string optionText = "Use Key";

        [Header("使用错误Key时提示 / Message When Using Wrong Key")]
        [TextArea(2, 5)]
        public string wrongKeyMessage = "This key does not fit.";

        [Header("该Key是否正确 / Whether This Key Is Correct")]
        public bool isCorrectKey;
    }

    [Header("门唯一ID（用于跨场景记忆已解锁） / Door Unique ID (Remembers Unlock Across Scenes)")]
    public string doorUniqueID = "door_01";

    [Header("目标场景 / Target Scene")]
    public string targetSceneName = "";

    [Header("出生点 / Spawn Position")]
    public Vector2 spawnPosition;

    [Header("前置对白（可空） / Intro Dialogue (Optional)")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("无任何Key时提示（可空，不填则只显示前置对白） / No-Key Message (Optional)")]
    [TextArea(2, 5)]
    public string noKeyMessage = "";

    [Header("可尝试的Key选项 / Available Key Options")]
    public DoorKeyOption[] keyOptions;

    [Header("音效 / Audio")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    private bool isUnlocked;

    // Initializes component references and singleton ownership before Start runs.
    void Awake()
    {
        isUnlocked = Inventory.IsDoorUnlocked(doorUniqueID);
    }

    // Handles player interaction with this object.
    public void OnInteract()
    {
        if (isUnlocked)
        {
            SwitchSceneNow();
            return;
        }

        if (OptionMenu.Instance == null) return;

        List<OptionMenu.OptionEntry> entries = BuildOwnedKeyEntries();
        bool hasOwnedKey = entries.Count > 0;

        Action showOptions = () =>
        {
            if (!hasOwnedKey)
            {
                if (!string.IsNullOrWhiteSpace(noKeyMessage) && DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ContinueDialogueAfterOption(new string[] { noKeyMessage });
                }
                else if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.CloseDialogueAfterOption();
                }
                return;
            }

            OptionMenu.Instance.ShowOptions(entries, false, null);
        };

        if (preDialogues != null && preDialogues.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(preDialogues, null, showOptions);
            return;
        }

        if (hasOwnedKey && DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        showOptions.Invoke();
    }

    // Builds data or UI objects required by this system.
    private List<OptionMenu.OptionEntry> BuildOwnedKeyEntries()
    {
        var entries = new List<OptionMenu.OptionEntry>();
        if (keyOptions == null) return entries;

        for (int i = 0; i < keyOptions.Length; i++)
        {
            DoorKeyOption keyOption = keyOptions[i];
            if (string.IsNullOrWhiteSpace(keyOption.keyUniqueID)) continue;
            if (!Inventory.HasCollected(keyOption.keyUniqueID)) continue;

            entries.Add(new OptionMenu.OptionEntry
            {
                text = keyOption.optionText,
                canExecute = () => true,
                onExecute = () => TryUseKey(keyOption)
            });
        }

        return entries;
    }

    // Attempts the requested operation and reports whether it succeeded.
    private void TryUseKey(DoorKeyOption keyOption)
    {
        if (keyOption.isCorrectKey)
        {
            UnlockDoor();
            SwitchSceneNow();
            return;
        }

        if (DialogueManager.Instance != null && !string.IsNullOrWhiteSpace(keyOption.wrongKeyMessage))
        {
            DialogueManager.Instance.ContinueDialogueAfterOption(new string[] { keyOption.wrongKeyMessage });
        }
        else if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogueAfterOption();
        }
    }

    // Unlocks the related object and records the unlock door state.
    private void UnlockDoor()
    {
        isUnlocked = true;
        Inventory.MarkDoorUnlocked(doorUniqueID);
    }

    // Switches to the target scene or state for switch scene now.
    private void SwitchSceneNow()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) return;
        if (sceneSwitchSfx != null)
        {
            AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
        }

        SceneTransition.LoadScene(targetSceneName, () =>
        {
            PlayerSpawn.SPAWN_POSITION = spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
        });
    }

    // Handles 2D trigger entry events for this object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked) return;
        if (!other.CompareTag("Player")) return;
        SwitchSceneNow();
    }
}
