using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactRange = 1.5f;
    public float angleTolerance = 60f;
    [Tooltip("即使碰撞体最近点很近，也要求与物体锚点(Transform)距离不能超过该值，避免大碰撞体导致远距离误触发。")]
    public float maxAnchorDistance = 1.9f;

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

        for (int i = 0; i < interactables.Count; i++)
        {
            IInteractable interactable = interactables[i];
            MonoBehaviour item = interactable as MonoBehaviour;
            if (item == null || !item.enabled) continue;

            Vector2 targetPoint = GetInteractionPoint(item);
            float distance = Vector2.Distance(transform.position, targetPoint);
            if (distance > interactRange) continue;

            float anchorDistance = Vector2.Distance(transform.position, item.transform.position);
            float effectiveAnchorDistance = Mathf.Min(anchorDistance, distance);
            if (effectiveAnchorDistance > maxAnchorDistance) continue;

            Vector2 dirToItem = (targetPoint - (Vector2)transform.position).normalized;
            float angle = Vector2.Angle(faceDir, dirToItem);

            if (angle <= angleTolerance && distance < bestDistance)
            {
                bestDistance = distance;
                bestInteractable = interactable;
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

        Collider2D[] childCols = item.GetComponentsInChildren<Collider2D>(true);
        if (childCols != null && childCols.Length > 0)
        {
            Vector2 bestPoint = item.transform.position;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < childCols.Length; i++)
            {
                Collider2D childCol = childCols[i];
                if (childCol == null) continue;
                Vector2 point = childCol.ClosestPoint(transform.position);
                float d = Vector2.Distance(transform.position, point);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    bestPoint = point;
                }
            }

            return bestPoint;
        }

        return item.transform.position;
    }
}
