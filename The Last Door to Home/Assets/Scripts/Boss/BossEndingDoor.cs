using UnityEngine;

/*
Purpose: Sends the player to one of two ending scenes depending on whether the linked eye monster cleared every target.
Attached GameObject: A door or exit object with a trigger collider.
Main responsibilities: Detect player entry, evaluate the eye-monster clear state, and switch to the matching ending scene.
Inputs: The linked EyeMonster, target ending scene names, optional spawn positions, and optional switch sound.
Outputs or effects: Loads ending2 or ending3 through the shared scene-transition flow.
Authorship or assistance: Original gameplay script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify trigger size, player tag, both ending scene names, and the eye-monster clear condition in Play Mode.
*/

public class BossEndingDoor : MonoBehaviour
{
    [Header("Boss Condition")]
    [SerializeField] private EyeMonster eyeMonster;

    [Header("Ending Scenes")]
    [SerializeField] private string ending2SceneName;
    [SerializeField] private string ending3SceneName;

    [Header("Spawn Positions")]
    [SerializeField] private bool setSpawnPositionOnLoad;
    [SerializeField] private Vector2 ending2SpawnPosition;
    [SerializeField] private Vector2 ending3SpawnPosition;

    [Header("Trigger Control")]
    [SerializeField] private float reTriggerCooldown = 0.25f;

    [Header("Audio")]
    [SerializeField] private AudioClip sceneSwitchSfx;
    [SerializeField, Range(0f, 1f)] private float sceneSwitchSfxVolume = 1f;

    private bool handledThisStay;
    private float nextAllowedTriggerTime;

    // Loads the appropriate ending scene when the player enters this boss-exit door.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (handledThisStay || Time.time < nextAllowedTriggerTime)
        {
            return;
        }

        handledThisStay = true;
        LoadConfiguredEnding();
    }

    // Rearms the trigger after the player leaves the door area.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        handledThisStay = false;
        nextAllowedTriggerTime = Time.time + Mathf.Max(0f, reTriggerCooldown);
    }

    // Chooses ending3 only when the eye monster finished clearing every target and then deactivated itself.
    private void LoadConfiguredEnding()
    {
        bool shouldLoadEnding3 = eyeMonster != null
            && eyeMonster.HasClearedAllTargets
            && !eyeMonster.gameObject.activeInHierarchy;

        string targetSceneName = shouldLoadEnding3 ? ending3SceneName : ending2SceneName;
        Vector2 spawnPosition = shouldLoadEnding3 ? ending3SpawnPosition : ending2SpawnPosition;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("BossEndingDoor: 目标结局场景名为空。", this);
            handledThisStay = false;
            return;
        }

        if (sceneSwitchSfx != null)
        {
            AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
        }

        SceneTransition.LoadScene(targetSceneName, () =>
        {
            if (!setSpawnPositionOnLoad)
            {
                return;
            }

            PlayerSpawn.SPAWN_POSITION = spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
        });
    }
}
