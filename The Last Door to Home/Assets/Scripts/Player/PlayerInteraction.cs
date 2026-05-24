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

        IInteractable bestInteractable = null;
        float bestDistance = float.MaxValue;

        foreach (var interactable in interactables)
        {
            MonoBehaviour item = interactable as MonoBehaviour;
            if (item == null || !item.enabled) continue;

            Vector2 targetPoint = GetInteractionPoint(item);
            float distance = Vector2.Distance(transform.position, targetPoint);
            if (distance > interactRange) continue;

            Vector2 dirToItem = (targetPoint - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(faceDir, dirToItem);

            if (angle <= angleTolerance)
            {
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestInteractable = interactable;
                }
            }
        }

        if (bestInteractable != null)
        {
            bestInteractable.OnInteract();
        }
    }

    Vector2 GetInteractionPoint(MonoBehaviour item)
    {
        Collider2D col = item.GetComponent<Collider2D>();
        if (col != null)
        {
            return col.ClosestPoint(transform.position);
        }

        return item.transform.position;
    }
}
