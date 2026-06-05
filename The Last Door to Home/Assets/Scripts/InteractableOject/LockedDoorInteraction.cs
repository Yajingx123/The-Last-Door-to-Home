using System;
using System.Collections.Generic;
using UnityEngine;

/*
Purpose: Manages l oc ke dd oo ri nt er ac ti on behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class LockedDoorInteraction : MonoBehaviour, IInteractable
{
    [Serializable]
    public class DoorKeyOption
    {
        [Header("这个Key的唯一ID（匹配 PickableItem.itemUniqueID）")]
        public string keyUniqueID = "";

        [Header("显示在选项里的文本")]
        public string optionText = "Use Key";

        [Header("使用错误Key时提示")]
        [TextArea(2, 5)]
        public string wrongKeyMessage = "This key does not fit.";

        [Header("该Key是否正确")]
        public bool isCorrectKey;
    }

    [Header("门唯一ID（用于跨场景记忆已解锁）")]
    public string doorUniqueID = "door_01";

    [Header("目标场景")]
    public string targetSceneName = "";

    [Header("出生点")]
    public Vector2 spawnPosition;

    [Header("前置对白（可空）")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("无任何Key时提示（可空，不填则只显示前置对白）")]
    [TextArea(2, 5)]
    public string noKeyMessage = "";

    [Header("可尝试的Key选项")]
    public DoorKeyOption[] keyOptions;

    [Header("音效")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    private bool isUnlocked;

    // Initializes cached references and one-time component state before gameplay begins.
    void Awake()
    {
        isUnlocked = Inventory.IsDoorUnlocked(doorUniqueID);
    }

    // Executes this object interaction when the player activates it.
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

    // Builds option entries for the keys the player currently owns.
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

    // Attempts to use the selected key on this locked door.
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

    // Unlocks the door and applies its post-unlock state changes.
    private void UnlockDoor()
    {
        isUnlocked = true;
        Inventory.MarkDoorUnlocked(doorUniqueID);
    }

    // Performs the requested scene change immediately.
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

    // Handles trigger entry events for this gameplay object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked) return;
        if (!other.CompareTag("Player")) return;
        SwitchSceneNow();
    }
}
