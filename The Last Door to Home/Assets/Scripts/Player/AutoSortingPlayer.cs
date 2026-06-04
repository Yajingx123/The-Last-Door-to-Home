using System;
using UnityEngine;

/*
Purpose: Manages a ut os or ti ng pl ay er behavior for this part of the game.
Attached GameObject: Player GameObject or a player-specific child object.
Main responsibilities: Read player-facing state, coordinate related components, and apply movement or presentation updates.
Inputs: Inspector references, Unity input, and state from linked gameplay managers.
Outputs or effects: Moves the player or camera, updates animations, and changes immediate gameplay feel.
Authorship or assistance: Original game script with English documentation assistance added via OpenAI Codex.
Testing notes: Verify inspector references, expected play-mode behavior, and any related UI or audio feedback after changes.
*/

[RequireComponent(typeof(SpriteRenderer))]
public class AutoSortingPlayer : MonoBehaviour
{
    [Header("默认层级（附近没有参照物时）")]
    public int defaultOrder = 100;

    [Header("相对参照物偏移")]
    public int behindOffset = -2; // 玩家在物体后面
    public int frontOffset = 2;   // 玩家在物体前面

    [Header("检测范围")]
    public Vector2 checkBoxSize = new Vector2(2.2f, 2.2f);
    public LayerMask detectLayers = ~0;
    public bool includeTriggerColliders = true;
    public string[] decorationTags = { "decorations", "decoration" };

    [Header("调试")]
    public bool debugLogTarget;

    private SpriteRenderer playerSR;
    private string lastTargetName;

    // Initializes cached references and one-time component state before gameplay begins.
    void Awake()
    {
        playerSR = GetComponent<SpriteRenderer>();
    }

    // Applies follow-up updates after other frame logic has already run.
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
            if (sr.sortingLayerID != playerSR.sortingLayerID) continue; // 只比较同一Sorting Layer

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

    // Checks whether the collider belongs to a decoration tag that should affect player occlusion.
    private bool HasDecorationTag(Collider2D col)
    {
        if (col == null) return false;
        if (decorationTags == null || decorationTags.Length == 0) return false;

        string currentTag = col.tag;
        for (int i = 0; i < decorationTags.Length; i++)
        {
            string tagName = decorationTags[i];
            if (string.IsNullOrWhiteSpace(tagName)) continue;

            if (string.Equals(currentTag, tagName, StringComparison.Ordinal))
            {
                return true;
            }
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
