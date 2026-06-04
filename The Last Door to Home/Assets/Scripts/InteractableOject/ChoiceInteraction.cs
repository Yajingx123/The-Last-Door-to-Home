using UnityEngine;
using System;
using System.Collections.Generic;

/*
Purpose: Manages c ho ic ei nt er ac ti on behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

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

    [Header("音效")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    // Executes this object interaction when the player activates it.
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

    // Builds the option entries that should be shown to the player.
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

    // Checks whether the selected choice can currently be executed.
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

    // Handles the blocked interaction path and shows the appropriate feedback.
    private void HandleBlocked(ChoiceOption choice)
    {
        if (DialogueManager.Instance != null && !string.IsNullOrWhiteSpace(choice.noItemMessage))
        {
            DialogueManager.Instance.ShowDialogue(new string[] { choice.noItemMessage });
        }
    }

    // Executes the chosen interaction result and applies its side effects.
    private void ExecuteChoice(ChoiceOption choice)
    {
        if (choice.actionType == ChoiceActionType.LoadScene && !string.IsNullOrWhiteSpace(choice.targetSceneName))
        {
            if (sceneSwitchSfx != null)
            {
                AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
            }

            SceneTransition.LoadScene(choice.targetSceneName, () =>
            {
                PlayerSpawn.SPAWN_POSITION = choice.spawnPosition;
                PlayerSpawn.NEED_SPAWN = true;
            });
        }
    }
}
