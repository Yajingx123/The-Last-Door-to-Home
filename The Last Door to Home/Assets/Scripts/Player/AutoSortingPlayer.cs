using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
/*
Purpose: Automatically adjusts the player sprite sorting order around decoration objects.
Attached GameObject: Player GameObject with a SpriteRenderer.
Main responsibilities: Scans nearby decoration colliders, finds the closest valid SpriteRenderer, and offsets the player sorting order.
Inputs: Player position, configured detection box, decoration tags, layer mask, and nearby Collider2D/SpriteRenderer data.
Outputs or effects: Updates the player SpriteRenderer sortingOrder and optional debug log output.
Authorship or assistance: Original project script; comments and documentation wording assisted by OpenAI Codex.
Testing notes: Verify the player renders in front of and behind decorations from multiple approach directions.
*/

public class AutoSortingPlayer : MonoBehaviour
{
    [Header("默认层级（附近没有参照物时） / Default Sorting Order (No Nearby Reference)")]
    public int defaultOrder = 100;

    [Header("相对参照物偏移 / Reference Sorting Offset")]
    public int behindOffset = -2; // Sorting offset when the player is behind the object.
    public int frontOffset = 2;   // Sorting offset when the player is in front of the object.

    [Header("检测范围 / Detection Range")]
    public Vector2 checkBoxSize = new Vector2(2.2f, 2.2f);
    public LayerMask detectLayers = ~0;
    public bool includeTriggerColliders = true;
    public string[] decorationTags = { "decorations", "decoration" };

    [Header("调试 / Debug")]
    public bool debugLogTarget;

    private SpriteRenderer playerSR;
    private string lastTargetName;

    // Initializes component references and singleton ownership before Start runs.
    void Awake()
    {
        playerSR = GetComponent<SpriteRenderer>();
    }

    // Applies follow-up updates after other frame logic has completed.
    void LateUpdate()
    {
        if (playerSR == null) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, checkBoxSize, 0f, detectLayers);

        SpriteRenderer targetSR = null;
        Collider2D targetCollider = null;
        float bestSqrDist = float.MaxValue;
        float playerFootY = playerSR.bounds.min.y;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;
            if (col.transform == transform) continue;
            if (!includeTriggerColliders && col.isTrigger) continue;
            if (!HasDecorationTag(col)) continue;

            SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
            if (sr == null) sr = col.GetComponentInParent<SpriteRenderer>();
            if (sr == null) sr = col.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) continue;
            if (sr.sortingLayerID != playerSR.sortingLayerID) continue; // Compare only within the same sorting layer.

            float sqrDist = (col.bounds.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                targetSR = sr;
                targetCollider = col;
            }
        }

        if (targetSR == null || targetCollider == null)
        {
            playerSR.sortingOrder = defaultOrder;
            lastTargetName = "(none)";
            return;
        }

        float targetFootY = targetCollider.bounds.min.y;
        bool isBehindTarget = playerFootY > targetFootY;
        playerSR.sortingOrder = targetSR.sortingOrder + (isBehindTarget ? behindOffset : frontOffset);
        lastTargetName = targetSR.name;

        if (debugLogTarget)
        {
            Debug.Log($"AutoSorting target={targetSR.name}, targetOrder={targetSR.sortingOrder}, playerOrder={playerSR.sortingOrder}, playerFootY={playerFootY:F3}, targetFootY={targetFootY:F3}, behind={isBehindTarget}", this);
        }
    }

    // Returns whether the required has decoration tag condition is met.
    private bool HasDecorationTag(Collider2D col)
    {
        if (col == null) return false;
        if (decorationTags == null || decorationTags.Length == 0) return false;

        if (HasAnyConfiguredTag(col.transform))
        {
            return true;
        }

        if (col.attachedRigidbody != null && HasAnyConfiguredTag(col.attachedRigidbody.transform))
        {
            return true;
        }

        SpriteRenderer sr = col.GetComponent<SpriteRenderer>();
        if (sr == null) sr = col.GetComponentInParent<SpriteRenderer>();
        if (sr != null && HasAnyConfiguredTag(sr.transform))
        {
            return true;
        }

        return false;
    }

    // Returns whether the required has any configured tag condition is met.
    private bool HasAnyConfiguredTag(Transform targetTransform)
    {
        Transform current = targetTransform;
        while (current != null)
        {
            string currentTag = current.tag;
            for (int i = 0; i < decorationTags.Length; i++)
            {
                string tagName = decorationTags[i];
                if (string.IsNullOrWhiteSpace(tagName)) continue;

                if (string.Equals(currentTag, tagName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    // Draws editor-only debug helpers while this object is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, checkBoxSize);
    }
}
