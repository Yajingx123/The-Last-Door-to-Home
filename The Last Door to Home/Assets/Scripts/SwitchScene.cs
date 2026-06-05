using UnityEngine;
/*
Purpose: Manages s wi tc hs ce ne behavior for this part of the game.
Attached GameObject: Relevant scene controller GameObject.
Main responsibilities: Coordinate inspector data, runtime checks, and the main behaviour handled by this script.
Inputs: Inspector configuration, scene references, and runtime method calls.
Outputs or effects: Applies runtime side effects through component state, UI updates, or return values.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

public class SwitchScene : MonoBehaviour
{
    [Header("目标场景")]
    public string targetSceneName;

    [Header("出生点")]
    public Vector2 spawnPosition;

    [Header("离场剧情事件（可选）")]
    public string beforeExitEventId;

    [Header("拦截后再次触发冷却（秒）")]
    public float reTriggerCooldown = 0.25f;

    [Header("音效")]
    public AudioClip sceneSwitchSfx;
    [Range(0f, 1f)] public float sceneSwitchSfxVolume = 1f;

    private bool handledThisStay;
    private float nextAllowedTriggerTime;

    // Handles trigger entry events for this gameplay object.
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

    // Stops the player immediately before a forced scene transition.
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

    // Performs the actual scene switch once trigger conditions are satisfied.
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

    // Handles trigger exit events for this gameplay object.
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        handledThisStay = false;
    }
}
