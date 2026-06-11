using UnityEngine;
/*
Purpose: Changes scenes when the player enters a configured trigger.
Attached GameObject: Scene exit or doorway GameObject with a 2D trigger collider.
Main responsibilities: Detects the player, optionally runs a story event, sets the next spawn point, plays SFX, and loads the target scene.
Inputs: Player trigger events, target scene name, spawn position, optional story event ID, and scene-switch audio settings.
Outputs or effects: Stops the player when blocked, updates PlayerSpawn, plays SFX, and starts a scene transition.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify trigger cooldown, blocked story events, spawn placement, and transition audio.
*/

public class SwitchScene : MonoBehaviour
{
    [Header("目标场景 / Target Scene")]
    public string targetSceneName;

    [Header("出生点 / Spawn Position")]
    public Vector2 spawnPosition;

    [Header("离场剧情事件（可选） / Exit Story Event (Optional)")]
    public string beforeExitEventId;

    [Header("拦截后再次触发冷却（秒） / Retrigger Cooldown After Block (Seconds)")]
    public float reTriggerCooldown = 0.25f;

    [Header("音效 / Audio")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    private bool handledThisStay;
    private float nextAllowedTriggerTime;

    // Handles 2D trigger entry events for this object.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (handledThisStay) return;
        if (Time.time < nextAllowedTriggerTime) return;

        handledThisStay = true;

        if (StoryDirector.Instance != null && !string.IsNullOrWhiteSpace(beforeExitEventId))
        {
            bool shouldBlock = StoryDirector.Instance.TryHandleEvent(beforeExitEventId);
            if (shouldBlock)
            {
                ForceStopPlayer(other.gameObject);
                nextAllowedTriggerTime = Time.time + Mathf.Max(0f, reTriggerCooldown);
                return;
            }
        }

        SwitchNow();
    }

    // Handles the force stop player step for this script.
    private void ForceStopPlayer(GameObject playerObj)
    {
        if (playerObj == null) return;

        PlayerMove move = playerObj.GetComponent<PlayerMove>();
        if (move != null)
        {
            move.ForceStopImmediate();
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    // Switches to the target scene or state for switch now.
    private void SwitchNow()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) return;
        if (sceneSwitchSfx != null)
        {
            AudioManager.EnsureInstance().PlaySfx(sceneSwitchSfx, sceneSwitchSfxVolume);
        }

        SceneTransition.LoadScene(targetSceneName, () =>
        {
            PlayerSpawn.SPAWN_POSITION = spawnPosition;
            PlayerSpawn.NEED_SPAWN = true;
        });
    }

    // Handles 2D trigger exit events for this object.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        handledThisStay = false;
    }
}
