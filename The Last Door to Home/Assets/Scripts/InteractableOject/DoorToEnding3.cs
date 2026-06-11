using UnityEngine;
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

public class DoorToEnding3 : MonoBehaviour, IInteractable
{
    [Header("目标结局动画场景 / Target Ending Cutscene Scene")]
    public string targetEndingSceneName = "Ending3";

    [Header("选项文案 / Option Text")]
    public string openOptionText = "Open It";
    public string cancelOptionText = "Cancel";

    [Header("白色渐变 / White Fade")]
    public Color fadeColor = Color.white;
    [Tooltip("从开始渐白到新场景渐显完成的总时长。会平均分给淡出和淡入。")]
    public float totalFadeDuration = 6f;

    [Header("音效 / Audio")]
    public AudioClip openSfx;
    [Range(0f, 1f)] public float openSfxVolume = 1f;

    // Handles player interaction with this object.
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

    // Opens the related UI or gameplay flow.
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

    // Returns whether this script can cancel.
    private void Cancel()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.LockPlayer(false);
        }
    }
}
