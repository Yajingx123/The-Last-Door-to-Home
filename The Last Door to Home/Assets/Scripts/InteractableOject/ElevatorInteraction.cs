using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void LoadScene(ElevatorOption option)
    {
        if (string.IsNullOrWhiteSpace(option.targetSceneName)) return;
        PlayerSpawn.SPAWN_POSITION = option.spawnPosition;
        PlayerSpawn.NEED_SPAWN = true;
        SceneManager.LoadScene(option.targetSceneName);
    }
}
