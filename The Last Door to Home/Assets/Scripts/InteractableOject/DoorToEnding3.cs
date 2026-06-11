using UnityEngine;
using System.Collections.Generic;

/*
Purpose: Shows a simple open/cancel door menu and loads the Ending 3 cutscene scene with a white fade.
Attached GameObject: Interactable door object with collider and player interaction detection.
Main responsibilities: Present door options and start a white scene transition into the ending cutscene.
Inputs: Player interaction calls and inspector-configured target ending cutscene scene.
Outputs or effects: Locks option input briefly, plays optional SFX, fades white, and loads the ending cutscene scene.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify target scene name, cutscene return behavior, option labels, and fade color in Play Mode.
*/

public class DoorToEnding3 : MonoBehaviour, IInteractable
{
    [Header("目标结局动画场景")]
    public string targetEndingSceneName = "Ending3";

    [Header("选项文案")]
    public string openOptionText = "Open It";
    public string cancelOptionText = "Cancel";

    [Header("白色渐变")]
    public Color fadeColor = Color.white;
    [Tooltip("从开始渐白到新场景渐显完成的总时长。会平均分给淡出和淡入。")]
    public float totalFadeDuration = 6f;

    [Header("音效")]
    public AudioClip openSfx;
    [Range(0f, 1f)] public float openSfxVolume = 1f;

    // Executes this object interaction when the player activates it.
    public void OnInteract()
    {
        if (OptionMenu.Instance == null) return;

        var entries = new List<OptionMenu.OptionEntry>
        {
            new OptionMenu.OptionEntry
            {
                text = openOptionText,
                canExecute = () => !string.IsNullOrWhiteSpace(targetEndingSceneName),
                onExecute = OpenDoor
            },
            new OptionMenu.OptionEntry
            {
                text = cancelOptionText,
                canExecute = () => true,
                onExecute = Cancel
            }
        };

        if (DialogueManager.Instance != null) DialogueManager.Instance.LockPlayer(true);
        OptionMenu.Instance.ShowOptions(entries, false, null);
    }

    // Opens the door and starts the configured ending cutscene scene.
    private void OpenDoor()
    {
        if (string.IsNullOrWhiteSpace(targetEndingSceneName)) return;

        if (openSfx != null)
        {
            AudioManager.EnsureInstance().PlaySfx(openSfx, openSfxVolume);
        }

        float halfFadeDuration = Mathf.Max(0.01f, totalFadeDuration) * 0.5f;
        SceneTransition.LoadSceneWithFadeColor(targetEndingSceneName, fadeColor, halfFadeDuration, halfFadeDuration);
    }

    // Closes the option flow without triggering dialogue.
    private void Cancel()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.LockPlayer(false);
        }
    }
}
