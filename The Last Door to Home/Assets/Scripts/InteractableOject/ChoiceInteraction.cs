using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class ChoiceInteraction : MonoBehaviour, IInteractable
{
    public enum RequireMode
    {
        None,
        ByItemType,
        ByUniqueID
    }

    public enum ChoiceActionType
    {
        None,
        LoadScene
    }

    [Serializable]
    public class ChoiceOption
    {
        public string optionText = "Option";
        public RequireMode requireMode = RequireMode.None;
        public ItemType requiredItemType = ItemType.Key;
        public string requiredItemUniqueID = "";
        [TextArea(2, 5)]
        public string noItemMessage = "You do not have the required item.";
        public ChoiceActionType actionType = ChoiceActionType.None;
        public string targetSceneName = "";
        public Vector2 spawnPosition;
    }

    [Header("前置对白（可空）")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("选项配置")]
    public ChoiceOption[] choices;

    public void OnInteract()
    {
        if (OptionMenu.Instance == null) return;

        List<OptionMenu.OptionEntry> entries = BuildEntries();

        if (preDialogues != null && preDialogues.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ShowDialogue(preDialogues, null, () =>
            {
                OptionMenu.Instance.ShowOptions(entries, true, null);
            });
            return;
        }

        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    private List<OptionMenu.OptionEntry> BuildEntries()
    {
        var entries = new List<OptionMenu.OptionEntry>();
        if (choices == null) return entries;

        for (int i = 0; i < choices.Length; i++)
        {
            ChoiceOption choice = choices[i];
            entries.Add(new OptionMenu.OptionEntry
            {
                text = choice.optionText,
                canExecute = () => CanExecute(choice),
                onBlocked = () => HandleBlocked(choice),
                onExecute = () => ExecuteChoice(choice)
            });
        }

        return entries;
    }

    private bool CanExecute(ChoiceOption choice)
    {
        if (choice.requireMode == RequireMode.None) return true;

        if (choice.requireMode == RequireMode.ByItemType)
        {
            return Inventory.HasItemType(choice.requiredItemType);
        }

        if (string.IsNullOrWhiteSpace(choice.requiredItemUniqueID)) return false;
        return Inventory.HasCollected(choice.requiredItemUniqueID);
    }

    private void HandleBlocked(ChoiceOption choice)
    {
        if (DialogueManager.Instance != null && !string.IsNullOrWhiteSpace(choice.noItemMessage))
        {
            DialogueManager.Instance.ShowDialogue(new string[] { choice.noItemMessage });
        }
    }

    private void ExecuteChoice(ChoiceOption choice)
    {
        if (choice.actionType == ChoiceActionType.LoadScene && !string.IsNullOrWhiteSpace(choice.targetSceneName))
        {
            PlayerSpawn.SPAWN_POSITION = choice.spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
            SceneManager.LoadScene(choice.targetSceneName);
        }
    }
}
