using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 1.5f;
    public float angleTolerance = 60f;
    private Vector2 faceDir = Vector2.right;

    void Update()
    {
        UpdateFaceDirection();

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (DialogueManager.Instance != null && !DialogueManager.Instance.CanStartInteraction) return;
            TryInteract();
        }
    }

    void UpdateFaceDirection()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
        {
            faceDir = new Vector2(h, v).normalized;
        }
    }

    void TryInteract()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError("❌ DialogueManager 实例不存在！");
            return;
        }

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        List<IInteractable> interactables = new List<IInteractable>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour is IInteractable interactable)
            {
                interactables.Add(interactable);
            }
        }

        foreach (var interactable in interactables)
        {
            MonoBehaviour item = interactable as MonoBehaviour;
            if (item == null || !item.enabled) continue;

            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance > interactRange) continue;

            Vector2 dirToItem = (item.transform.position - transform.position).normalized;
            float angle = Vector2.Angle(faceDir, dirToItem);

            if (angle <= angleTolerance)
            {
                interactable.OnInteract();
                break;
            }
        }
    }
}
