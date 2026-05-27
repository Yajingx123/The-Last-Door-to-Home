using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class StoryEventTrigger : MonoBehaviour
{
    private const string OneShotPrefPrefix = "OneShotTrigger:";

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
        if (IsAlreadyPlayed(effectiveFlag))
        {
            triggered = true;
            return;
        }

        if (StoryDirector.Instance == null) return;

        StoryDirector.Instance.TryHandleEvent(eventId);
        triggered = true;

        if (!string.IsNullOrWhiteSpace(effectiveFlag))
        {
            MarkPlayed(effectiveFlag);
        }
    }

    private bool IsAlreadyPlayed(string effectiveFlag)
    {
        if (string.IsNullOrWhiteSpace(effectiveFlag)) return false;
        if (StoryFlags.Has(effectiveFlag)) return true;
        return PlayerPrefs.GetInt(OneShotPrefPrefix + effectiveFlag, 0) == 1;
    }

    private void MarkPlayed(string effectiveFlag)
    {
        if (string.IsNullOrWhiteSpace(effectiveFlag)) return;

        StoryFlags.Set(effectiveFlag);
        PlayerPrefs.SetInt(OneShotPrefPrefix + effectiveFlag, 1);
        PlayerPrefs.Save();
    }
}
