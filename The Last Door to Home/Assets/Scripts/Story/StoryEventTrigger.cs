using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StoryEventTrigger : MonoBehaviour
{
    [Header("事件")]
    public string eventId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (StoryDirector.Instance == null) return;
        StoryDirector.Instance.TryHandleEvent(eventId);
    }
}
