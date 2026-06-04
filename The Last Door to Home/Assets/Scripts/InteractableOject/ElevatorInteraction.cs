using System;
using System.Collections.Generic;
using UnityEngine;

/*
Purpose: Manages e le va to ri nt er ac ti on behavior for this part of the game.
Attached GameObject: Interactable scene object with collider and interaction logic.
Main responsibilities: Respond to player interaction requests and trigger the correct object-specific outcome.
Inputs: Player interaction calls, inspector configuration, and current story or inventory state.
Outputs or effects: Triggers dialogue, state changes, item flow, or scene reactions after interaction.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class ElevatorInteraction : MonoBehaviour, IInteractable
{
    [Serializable]
    public class ElevatorOption
    {
        public string optionText = "Go";
        public string targetSceneName = "";
        public Vector2 spawnPosition;
    }

    [Header("前置对白（可空）")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("电梯选项")]
    public ElevatorOption[] options;

    [Header("音效")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    // Executes this object interaction when the player activates it.
    public void OnInteract()
    {
        if (OptionMenu.Instance == null) return;

        List<OptionMenu.OptionEntry> entries = BuildEntries();
        if (entries.Count == 0) return;

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
        if (options == null) return entries;

        for (int i = 0; i < options.Length; i++)
        {
            ElevatorOption option = options[i];
            entries.Add(new OptionMenu.OptionEntry
            {
                text = option.optionText,
                canExecute = () => !string.IsNullOrWhiteSpace(option.targetSceneName),
                onExecute = () => LoadScene(option)
            });
        }

        return entries;
    }

    // Starts loading the requested scene through the transition flow.
    private void LoadScene(ElevatorOption option)
    {
        if (string.IsNullOrWhiteSpace(option.targetSceneName)) return;
        if (sceneSwitchSfx != null)
        {
            AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
        }

        SceneTransition.LoadScene(option.targetSceneName, () =>
        {
            PlayerSpawn.SPAWN_POSITION = option.spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
        });
    }
}
