using UnityEngine;
/*
Purpose: Controls boss, enemy, damage, or boss-ending behavior.
Attached GameObject: Boss/enemy GameObject, damage hitbox, or boss-scene controller.
Main responsibilities: Updates combat movement/state, resolves contact damage, handles defeat, and triggers ending or door behavior.
Inputs: Player position, colliders, serialized combat settings, health/progression state, and scene triggers.
Outputs or effects: Moves enemies, applies damage, updates animations, changes story/ending state, or loads scenes.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify combat states, damage timing, defeat conditions, and ending transitions.
*/

public class BossEndingDoor : MonoBehaviour
{
    [Header("Boss Condition / Boss 条件")]
    [SerializeField] private EyeMonster eyeMonster;

    [Header("Ending Scenes / 结局场景")]
    [SerializeField] private string ending2SceneName;
    [SerializeField] private string ending3SceneName;

    [Header("Spawn Positions / 出生点")]
    [SerializeField] private bool setSpawnPositionOnLoad;
    [SerializeField] private Vector2 ending2SpawnPosition;
    [SerializeField] private Vector2 ending3SpawnPosition;

    [Header("Trigger Control / 触发控制")]
    [SerializeField] private float reTriggerCooldown = 0.25f;

    [Header("Audio / 音频")]
    [SerializeField] private AudioClip sceneSwitchSfx;
    [SerializeField, Range(0f, 1f)] private float sceneSwitchSfxVolume = 1f;

    private bool handledThisStay;
    private float nextAllowedTriggerTime;

    // Handles 2D trigger entry events for this object.
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

    // Handles 2D trigger exit events for this object.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        handledThisStay = false;
        nextAllowedTriggerTime = Time.time + Mathf.Max(0f, reTriggerCooldown);
    }

    // Loads the requested data, scene, or runtime content.
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
