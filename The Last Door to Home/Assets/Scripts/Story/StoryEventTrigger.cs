using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoryEventTrigger : MonoBehaviour
{
    [Header("事件")]
    public string eventId;

    [Header("只触发一次")]
    public bool triggerOnce = true;

    [Header("一次性Flag（可选）")]
    public string playedFlag;

    private bool triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;

        if (!string.IsNullOrWhiteSpace(playedFlag) && StoryFlags.Has(playedFlag))
        {
            triggered = true;
            return;
        }

        if (StoryDirector.Instance == null) return;

        StoryDirector.Instance.TryHandleEvent(eventId);
        triggered = true;

        if (!string.IsNullOrWhiteSpace(playedFlag))
        {
            StoryFlags.Set(playedFlag);
        }
    }
}
