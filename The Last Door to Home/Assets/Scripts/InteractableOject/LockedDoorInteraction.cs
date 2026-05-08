using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private bool isUnlocked;

    void Awake()
    {
        isUnlocked = Inventory.IsDoorUnlocked(doorUniqueID);
    }

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
                if (DialogueManager.Instance != null)
                {
                    // 结束前置对白的“等待选项”状态，避免卡住无法关闭。
                    DialogueManager.Instance.CloseDialogueAfterOption();
                }

                if (!string.IsNullOrWhiteSpace(noKeyMessage) && DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.ShowDialogue(new string[] { noKeyMessage });
                }
                return;
            }

            OptionMenu.Instance.ShowOptions(entries, true, null);
        };

        if (preDialogues != null && preDialogues.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(preDialogues, null, showOptions);
            return;
        }

        if (hasOwnedKey && DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        showOptions.Invoke();
    }

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
            DialogueManager.Instance.ShowDialogue(new string[] { keyOption.wrongKeyMessage });
        }
    }

    private void UnlockDoor()
    {
        isUnlocked = true;
        Inventory.MarkDoorUnlocked(doorUniqueID);
    }

    private void SwitchSceneNow()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) return;
        PlayerSpawn.SPAWN_POSITION = spawnPosition;
        PlayerSpawn.NEED_SPAWN = true;
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUnlocked) return;
        if (!other.CompareTag("Player")) return;
        SwitchSceneNow();
    }
}
