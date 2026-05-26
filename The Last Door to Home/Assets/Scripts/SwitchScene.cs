using UnityEngine;
using UnityEngine.SceneManagement;

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

    private bool handledThisStay;
    private float nextAllowedTriggerTime;

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

    private void SwitchNow()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName)) return;

        PlayerSpawn.SPAWN_POSITION = spawnPosition;
        PlayerSpawn.NEED_SPAWN = true;
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        handledThisStay = false;
    }
}
