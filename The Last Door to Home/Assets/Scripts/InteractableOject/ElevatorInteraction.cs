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

public class ElevatorInteraction : MonoBehaviour, IInteractable
{
    [Serializable]
public class ElevatorOption
    {
        public string optionText = "Go";
        public string targetSceneName = "";
        public Vector2 spawnPosition;
    }

    [Header("前置对白（可空） / Intro Dialogue (Optional)")]
    [TextArea(3, 10)]
    public string[] preDialogues;

    [Header("电梯选项 / Elevator Options")]
    public ElevatorOption[] options;

    [Header("音效 / Audio")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    // Handles player interaction with this object.
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

    // Builds data or UI objects required by this system.
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

    // Loads the requested data, scene, or runtime content.
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
