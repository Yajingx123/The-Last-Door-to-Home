using UnityEngine;
using UnityEngine.SceneManagement;

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

    private string GetHierarchyPath()
    {
        string path = gameObject.name;
        Transform current = transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private string GetEffectiveFlag()
    {
        if (!string.IsNullOrWhiteSpace(playedFlag)) return playedFlag;
        if (!triggerOnce) return string.Empty;

        // Auto-generate a stable key per scene object for one-shot triggers.
        Scene scene = gameObject.scene;
        string sceneName = scene.IsValid() ? scene.name : "UnknownScene";
        return $"StoryTrigger:{sceneName}:{GetHierarchyPath()}";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;

        string effectiveFlag = GetEffectiveFlag();
        if (!string.IsNullOrWhiteSpace(effectiveFlag) && StoryFlags.Has(effectiveFlag))
        {
            triggered = true;
            return;
        }

        if (StoryDirector.Instance == null) return;

        StoryDirector.Instance.TryHandleEvent(eventId);
        triggered = true;

        if (!string.IsNullOrWhiteSpace(effectiveFlag))
        {
            StoryFlags.Set(effectiveFlag);
        }
    }
}
