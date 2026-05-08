using UnityEngine;

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
    public string decorationTag = "decoration";

    [Header("调试")]
    public bool debugLogTarget;

    private SpriteRenderer playerSR;
    private string lastTargetName;

    void Awake()
    {
        playerSR = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (playerSR == null) return;

        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, checkBoxSize, 0f, detectLayers);

        SpriteRenderer targetSR = null;
        float bestSqrDist = float.MaxValue;
        float playerY = transform.position.y;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null) continue;
            if (col.transform == transform) continue;
            if (!includeTriggerColliders && col.isTrigger) continue;
            if (!col.CompareTag(decorationTag)) continue;

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
            }
        }

        if (targetSR == null)
        {
            playerSR.sortingOrder = defaultOrder;
            lastTargetName = "(none)";
            return;
        }

        float targetY = targetSR.bounds.center.y;
        bool isBehindTarget = playerY > targetY;
        playerSR.sortingOrder = targetSR.sortingOrder + (isBehindTarget ? behindOffset : frontOffset);
        lastTargetName = targetSR.name;

        if (debugLogTarget)
        {
            Debug.Log($"AutoSorting target={targetSR.name}, targetOrder={targetSR.sortingOrder}, playerOrder={playerSR.sortingOrder}, behind={isBehindTarget}", this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, checkBoxSize);
    }
}
