using UnityEngine;
using System;
using System.Collections.Generic;
/*
Purpose: Implements a world interaction used by the player interaction system.
Attached GameObject: Scene object with a Collider2D and interaction-specific serialized settings.
Main responsibilities: Checks interaction requirements, updates inventory/story/scene state, and provides player feedback.
Inputs: Player interaction calls, serialized IDs/text, inventory state, story flags, and optional audio or scene settings.
Outputs or effects: Starts dialogue, changes locked/collected state, updates Inventory/StoryFlags, plays audio, or triggers scene flow.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify successful interaction, missing-requirement feedback, repeated interaction behavior, and save/load persistence.
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
        LoadScene,
        LoadTemporaryCutscene
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

    [Header("前置对白（可空） / Intro Dialogue (Optional)")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("选项配置 / Option Settings")]
    public ChoiceOption[] choices;

    [Header("音效 / Audio")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    // Handles player interaction with this object.
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

    // Builds data or UI objects required by this system.
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

    // Returns whether this script can can execute.
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

    // Handles the event or callback associated with this method.
    private void HandleBlocked(ChoiceOption choice)
    {
        if (DialogueManager.Instance != null && !string.IsNullOrWhiteSpace(choice.noItemMessage))
        {
            DialogueManager.Instance.ShowDialogue(new string[] { choice.noItemMessage });
        }
    }

    // Handles the execute choice step for this script.
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

        if (choice.actionType == ChoiceActionType.LoadTemporaryCutscene && !string.IsNullOrWhiteSpace(choice.targetSceneName))
        {
            if (sceneSwitchSfx != null)
            {
                AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
            }

            CutsceneReturnContext.SaveCurrentSceneAndPlayerPosition();
            SceneTransition.LoadScene(choice.targetSceneName);
        }
    }
}
